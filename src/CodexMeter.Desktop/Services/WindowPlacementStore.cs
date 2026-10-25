using System.Text.Json;
using System.IO;

namespace CodexMeter.Desktop.Services;

public sealed record WindowPlacement(double Left, double Top);

public sealed class WindowPlacementStore
{
    private readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexMeter",
        "window.json");

    public WindowPlacement? Load()
    {
        try
        {
            return File.Exists(_filePath)
                ? JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(_filePath))
                : null;
        }
        catch
        {
            return null;
        }
    }

    public void Save(double left, double top)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(new WindowPlacement(left, top)));
        }
        catch
        {
        }
    }
}
