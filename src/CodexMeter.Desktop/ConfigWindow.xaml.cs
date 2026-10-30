using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CodexMeter.Desktop.Services;

namespace CodexMeter.Desktop;

public partial class ConfigWindow : Window
{
    private readonly Action<AppearanceSettings> _preview;
    private AppearanceSettings _settings;
    private bool _ready;
    public AppearanceSettings? SavedSettings { get; private set; }

    public ConfigWindow(AppearanceSettings settings, Action<AppearanceSettings> preview)
    {
        InitializeComponent();
        _settings = settings;
        _preview = preview;
        ThemeCombo.SelectedItem = ThemeCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (string)item.Tag == settings.Theme) ?? ThemeCombo.Items[0];
        ShowWeeklyCheck.IsChecked = settings.ShowWeekly;
        ShowResetCheck.IsChecked = settings.ShowReset;
        CountdownCheck.IsChecked = settings.ShowCountdown;
        TopLeftCheck.IsChecked = settings.TopLeft;
        TopRightCheck.IsChecked = settings.TopRight;
        BottomLeftCheck.IsChecked = settings.BottomLeft;
        BottomRightCheck.IsChecked = settings.BottomRight;
        BorderSizeSlider.Value = settings.SafeBorderThickness;
        CardWidthSlider.Value = settings.SafeWidth;
        CardHeightSlider.Value = settings.SafeHeight;
        var matchingColor = BorderColorCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals(item.Tag as string, settings.BorderColor, StringComparison.OrdinalIgnoreCase));
        BorderColorCombo.SelectedItem = matchingColor ?? BorderColorCombo.Items.OfType<ComboBoxItem>().Last();
        BorderHexInput.Text = settings.BorderColor is { Length: 7 } ? settings.BorderColor : "#FFFFFF";
        BorderHexInput.IsEnabled = matchingColor is null;
        _ready = true;
        UpdatePreview();
    }

    private void Details_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _settings = _settings with { ShowWeekly = ShowWeeklyCheck.IsChecked == true, ShowReset = ShowResetCheck.IsChecked == true, ShowCountdown = CountdownCheck.IsChecked == true };
        UpdatePreview();
    }

    private void Theme_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || ThemeCombo.SelectedItem is not ComboBoxItem item) return;
        _settings = _settings with { Theme = item.Tag as string ?? "classic" };
        UpdatePreview();
    }

    private void Corner_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _settings = _settings with
        {
            TopLeft = TopLeftCheck.IsChecked == true,
            TopRight = TopRightCheck.IsChecked == true,
            BottomLeft = BottomLeftCheck.IsChecked == true,
            BottomRight = BottomRightCheck.IsChecked == true
        };
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        BorderSizeLabel.Text = $"Espessura · {_settings.SafeBorderThickness:0} px";
        CardWidthLabel.Text = $"Largura · {_settings.SafeWidth:0} px";
        CardHeightLabel.Text = $"Altura · {_settings.SafeHeight:0} px";
        _preview(_settings);
    }

    public void SetPreviewSource(Visual source) => LivePreview.Fill = new VisualBrush(source) { Stretch = Stretch.Uniform };

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        for (var element = e.OriginalSource as DependencyObject; element is not null; element = VisualTreeHelper.GetParent(element))
            if (element is ButtonBase) return;
        DragMove();
    }

    private void CardSize_OnChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready) return;
        _settings = _settings with { CardWidth = CardWidthSlider.Value, CardHeight = CardHeightSlider.Value };
        UpdatePreview();
    }

    private void BorderSize_OnChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready) return;
        _settings = _settings with { BorderThickness = e.NewValue };
        UpdatePreview();
    }

    private void BorderColor_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || BorderColorCombo.SelectedItem is not ComboBoxItem item) return;
        var color = item.Tag as string ?? "theme";
        BorderHexInput.IsEnabled = color == "custom";
        if (color == "custom")
        {
            ApplyCustomColor();
            return;
        }
        if (color.StartsWith('#')) BorderHexInput.Text = color;
        SaveButton.IsEnabled = true;
        BorderHint.Text = "Código #RRGGBB";
        _settings = _settings with { BorderColor = color };
        UpdatePreview();
    }

    private void BorderHex_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready && BorderColorCombo.SelectedItem is ComboBoxItem { Tag: "custom" }) ApplyCustomColor();
    }

    private void ApplyCustomColor()
    {
        var color = BorderHexInput.Text.Trim();
        var valid = System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
        SaveButton.IsEnabled = valid;
        BorderHint.Text = valid ? "Código #RRGGBB" : "Digite uma cor válida, por exemplo #FF8844";
        if (!valid) return;
        _settings = _settings with { BorderColor = color.ToUpperInvariant() };
        UpdatePreview();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) { SavedSettings = _settings; Close(); }
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => Close();
    private void Window_OnKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
}
