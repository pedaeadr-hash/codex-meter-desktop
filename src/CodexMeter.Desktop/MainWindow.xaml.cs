using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using CodexMeter.Core;
using CodexMeter.Desktop.Services;

namespace CodexMeter.Desktop;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly WindowPlacementStore _placementStore = new();
    private readonly AppearanceStore _appearanceStore = new();
    private AppearanceSettings _appearance;
    private AppearanceSettings? _visibleAppearance;
    private ConfigWindow? _configWindow;

    public MainWindow(IUsageProvider usageProvider)
    {
        InitializeComponent();
        _appearance = _appearanceStore.Load();
        ApplyAppearance(_appearance);
        _viewModel = new MainWindowViewModel(usageProvider);
        DataContext = _viewModel;
        _viewModel.SetCountdown(_appearance.ShowCountdown);
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePlacement();
        await _viewModel.RefreshAsync();

        var previewPath = Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_PATH");
        if (!string.IsNullOrWhiteSpace(previewPath))
        {
            var previewTheme = Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_THEME");
            var preview = _appearance;
            if (!string.IsNullOrWhiteSpace(previewTheme)) preview = preview with { Theme = previewTheme };
            if (double.TryParse(Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_WIDTH"), out var previewWidth)) preview = preview with { CardWidth = previewWidth };
            if (double.TryParse(Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_HEIGHT"), out var previewHeight)) preview = preview with { CardHeight = previewHeight };
            ApplyAppearance(preview);
            RenderPreview(previewPath);
            Application.Current.Shutdown();
        }
    }

    private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || HasButtonParent(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();

    private void Config_OnClick(object sender, RoutedEventArgs e) => ShowConfig();

    public void ShowConfig()
    {
        if (!IsVisible) Show();
        if (_configWindow is not null)
        {
            _configWindow.Activate();
            return;
        }

        var original = _appearance;
        _configWindow = new ConfigWindow(original, ApplyAppearance);
        _configWindow.Owner = this;
        _configWindow.SetPreviewSource(this);
        _configWindow.Closed += (_, _) =>
        {
            if (_configWindow?.SavedSettings is { } saved)
            {
                try
                {
                    _appearanceStore.Save(saved);
                    _appearance = saved;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    ApplyAppearance(original);
                    MessageBox.Show("Não foi possível salvar a aparência. " + exception.Message, "Codex Meter", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ApplyAppearance(original);
            }
            _configWindow = null;
        };
        _configWindow.Show();
        _configWindow.Activate();
    }

    private void ApplyAppearance(AppearanceSettings settings)
    {
        _visibleAppearance = settings;
        Width = settings.SafeWidth;
        Height = settings.SafeHeight;
        var theme = AppearanceThemes.Get(settings.Theme);
        CardBorder.Background = theme.Body;
        CardBorder.BorderBrush = settings.BorderBrush(theme.Border);
        CardBorder.BorderThickness = new Thickness(settings.SafeBorderThickness);
        CardHeader.Background = theme.Header;
        CardBorder.CornerRadius = settings.Corners(28);
        var poster = settings.Theme == "poster";
        var apple = settings.Theme is "ember-dark" or "ember-light" or "bar";
        CardHeader.Visibility = poster || apple ? Visibility.Collapsed : Visibility.Visible;
        ClassicDetails.Visibility = poster || apple ? Visibility.Collapsed : Visibility.Visible;
        PosterLayout.Visibility = poster ? Visibility.Visible : Visibility.Collapsed;
        AppleLayout.Visibility = apple ? Visibility.Visible : Visibility.Collapsed;
        AppleLayout.Apply(settings);
        ClassicWeekly.Visibility = PosterWeekly.Visibility = settings.ShowWeekly ? Visibility.Visible : Visibility.Collapsed;
        ClassicReset.Visibility = PosterReset.Visibility = settings.ShowReset ? Visibility.Visible : Visibility.Collapsed;
        _viewModel?.SetCountdown(settings.ShowCountdown);
        UpdatePosterClip();
        CardHeader.CornerRadius = new CornerRadius(settings.TopLeft ? 14 : 0, settings.TopRight ? 14 : 0, 14, 14);
        ThemeArt.Children.Clear();
    }

    private void PosterLayout_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdatePosterClip();

    private void UpdatePosterClip()
    {
        if (_visibleAppearance is not { } settings) return;
        var active = AppleLayout.Visibility == Visibility.Visible ? (FrameworkElement)AppleLayout : PosterLayout;
        var w = active.ActualWidth;
        var h = active.ActualHeight;
        if (w <= 0 || h <= 0) return;
        var r = Math.Max(0, 28 - settings.SafeBorderThickness);
        var tl = settings.TopLeft ? r : 0; var tr = settings.TopRight ? r : 0;
        var br = settings.BottomRight ? r : 0; var bl = settings.BottomLeft ? r : 0;
        var geometry = new StreamGeometry();
        using (var c = geometry.Open())
        {
            c.BeginFigure(new Point(tl, 0), true, true);
            c.LineTo(new Point(w - tr, 0), true, false); c.QuadraticBezierTo(new Point(w, 0), new Point(w, tr), true, false);
            c.LineTo(new Point(w, h - br), true, false); c.QuadraticBezierTo(new Point(w, h), new Point(w - br, h), true, false);
            c.LineTo(new Point(bl, h), true, false); c.QuadraticBezierTo(new Point(0, h), new Point(0, h - bl), true, false);
            c.LineTo(new Point(0, tl), true, false); c.QuadraticBezierTo(new Point(0, 0), new Point(tl, 0), true, false);
        }
        PosterLayout.Clip = geometry;
        AppleLayout.Clip = geometry;
    }

    private void Hide_OnClick(object sender, RoutedEventArgs e) => Hide();

    private void Exit_OnClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void WidgetMenu_OnOpened(object sender, RoutedEventArgs e) =>
        StartupMenuItem.IsChecked = StartupManager.IsEnabled();

    private void Startup_OnClick(object sender, RoutedEventArgs e)
    {
        StartupManager.SetEnabled(StartupMenuItem.IsChecked);
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_PATH")) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODEX_METER_CONFIG_PREVIEW_PATH"))) _placementStore.Save(Left, Top);
        _viewModel.Dispose();
    }

    private void RestorePlacement()
    {
        var workArea = SystemParameters.WorkArea;
        var saved = _placementStore.Load();
        if (saved is not null &&
            saved.Left >= workArea.Left - Width + 40 && saved.Left <= workArea.Right - 40 &&
            saved.Top >= workArea.Top - Height + 40 && saved.Top <= workArea.Bottom - 40)
        {
            Left = saved.Left;
            Top = saved.Top;
            return;
        }

        Left = workArea.Right - Width - 18;
        Top = workArea.Bottom - Height - 18;
    }

    private static bool HasButtonParent(DependencyObject? current)
    {
        while (current is not null)
        {
            if (current is ButtonBase)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void RenderPreview(string path)
    {
        UpdateLayout();
        var dpi = VisualTreeHelper.GetDpi(this);
        var bitmap = new RenderTargetBitmap(
            Math.Max(1, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX)),
            Math.Max(1, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY)),
            96 * dpi.DpiScaleX,
            96 * dpi.DpiScaleY,
            PixelFormats.Pbgra32);
        bitmap.Render(this);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
