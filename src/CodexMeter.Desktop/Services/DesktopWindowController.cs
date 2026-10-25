using System.Windows;

namespace CodexMeter.Desktop.Services;

public sealed class DesktopWindowController(MainWindow window) : IWindowController
{
    public void Open() => window.Dispatcher.BeginInvoke(() =>
    {
        if (!window.IsVisible)
        {
            window.Show();
        }

        window.WindowState = WindowState.Normal;
        window.Topmost = true;
        window.Activate();
    });

    public void Minimize() => window.Dispatcher.BeginInvoke(window.Hide);

    public void Close() => _ = Task.Run(async () =>
    {
        // Give the controller time to return its HTTP response before stopping Kestrel.
        await Task.Delay(200);
        await window.Dispatcher.InvokeAsync(() => Application.Current.Shutdown());
    });
}
