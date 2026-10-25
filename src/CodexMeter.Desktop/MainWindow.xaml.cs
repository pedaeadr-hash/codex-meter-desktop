using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
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

    public MainWindow(IUsageProvider usageProvider)
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(usageProvider);
        DataContext = _viewModel;
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePlacement();
        await _viewModel.RefreshAsync();

        var previewPath = Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_PATH");
        if (!string.IsNullOrWhiteSpace(previewPath))
        {
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
        _placementStore.Save(Left, Top);
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
