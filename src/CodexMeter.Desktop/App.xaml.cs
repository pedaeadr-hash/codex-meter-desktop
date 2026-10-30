using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using CodexMeter.Core;
using CodexMeter.Desktop.Services;
using CodexMeter.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CodexMeter.Desktop;

public partial class App : Application
{
    public const string LocalApiUrl = "http://127.0.0.1:5087";
    private Mutex? _instanceMutex;
    private WebApplication? _api;
    private CodexAppServerClient? _usageProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configPreviewPath = Environment.GetEnvironmentVariable("CODEX_METER_CONFIG_PREVIEW_PATH");
        if (!string.IsNullOrWhiteSpace(configPreviewPath))
        {
            _usageProvider = new CodexAppServerClient();
            var previewWindow = new MainWindow(_usageProvider);
            MainWindow = previewWindow;
            previewWindow.Show();
            var config = new ConfigWindow(new AppearanceStore().Load(), _ => { }) { Owner = previewWindow };
            config.SetPreviewSource(previewWindow);
            config.Show();
            await Dispatcher.InvokeAsync(() =>
            {
                config.UpdateLayout();
                var dpi = VisualTreeHelper.GetDpi(config);
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(config.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(config.ActualHeight * dpi.DpiScaleY), 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
                bitmap.Render(config);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(configPreviewPath))!);
                using var output = File.Create(configPreviewPath);
                encoder.Save(output);
                Shutdown();
            }, DispatcherPriority.ApplicationIdle);
            return;
        }

        // Preview runs are isolated from the active widget and its local controller.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODEX_METER_PREVIEW_PATH")))
        {
            _usageProvider = new CodexAppServerClient();
            MainWindow = new MainWindow(_usageProvider);
            MainWindow.Show();
            return;
        }

        _instanceMutex = new Mutex(initiallyOwned: true, "Local\\CodexMeter.Desktop", out var createdNew);
        if (!createdNew)
        {
            await RequestExistingWindowAsync();
            Shutdown();
            return;
        }

        _usageProvider = new CodexAppServerClient();
        var window = new MainWindow(_usageProvider);
        var windowController = new DesktopWindowController(window);

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = e.Args,
            ApplicationName = typeof(App).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.WebHost.UseUrls(LocalApiUrl);
        builder.Services.AddSingleton<IUsageProvider>(_usageProvider);
        builder.Services.AddSingleton<IWindowController>(windowController);
        builder.Services.AddControllers();

        _api = builder.Build();
        _api.MapControllers();

        try
        {
            await _api.StartAsync();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Nao foi possivel iniciar o Codex Meter.\n\n{exception.Message}",
                "Codex Meter",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_api is not null)
        {
            _ = _api.StopAsync();
            _ = _api.DisposeAsync();
        }

        _usageProvider?.Dispose();

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static async Task RequestExistingWindowAsync()
    {
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(LocalApiUrl) };
            using var response = await client.PostAsync("/api/app/open", null);
        }
        catch
        {
        }
    }
}
