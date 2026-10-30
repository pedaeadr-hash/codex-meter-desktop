using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace CodexMeter.Desktop.Services;

public sealed record AppearanceSettings(string Theme = "classic", bool TopLeft = true, bool TopRight = true, bool BottomRight = true, bool BottomLeft = true, double BorderThickness = 5, string BorderColor = "theme", double CardWidth = 236, double CardHeight = 148, bool ShowWeekly = true, bool ShowReset = true, bool ShowCountdown = false)
{
    public CornerRadius Corners(double radius) => new(TopLeft ? radius : 0, TopRight ? radius : 0, BottomRight ? radius : 0, BottomLeft ? radius : 0);
    public double SafeBorderThickness => double.IsFinite(BorderThickness) ? Math.Clamp(BorderThickness, 0, 8) : 5;
    public double SafeWidth => double.IsFinite(CardWidth) ? Math.Clamp(CardWidth, 220, 400) : 236;
    public double SafeHeight => double.IsFinite(CardHeight) ? Math.Clamp(CardHeight, 140, 240) : 148;
    public Brush BorderBrush(Brush themeBorder)
    {
        if (BorderColor is null || !System.Text.RegularExpressions.Regex.IsMatch(BorderColor, "^#[0-9a-fA-F]{6}$")) return themeBorder;
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(BorderColor));
    }
}

public sealed class AppearanceStore
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexMeter", "appearance.json");

    public AppearanceSettings Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<AppearanceSettings>(File.ReadAllText(_path)) ?? new() : new(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public void Save(AppearanceSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed record AppearanceTheme(string Id, string Name, string Description, Brush Header, Brush Body, Brush Border, Brush Accent);

public static class AppearanceThemes
{
    private static Brush Solid(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    private static readonly AppearanceTheme Default = new("classic", "Preto essencial", "", Solid("#111111"), Solid("#090909"), Solid("#090909"), Solid("#272727"));
    private static readonly AppearanceTheme Poster = new("poster", "Pontilhado", "", Solid("#111111"), Solid("#090909"), Solid("#222222"), Solid("#EEEEEE"));
    private static readonly AppearanceTheme Light = new("light", "Claro", "", Solid("#EEEDE6"), Solid("#EEEDE6"), Solid("#EEEDE6"), Solid("#222222"));
    public static AppearanceTheme Get(string? id) => id == "poster" ? Poster : id is "ember-light" or "contrast" ? Light : Default;
}
