using System.Net.Http;
using System.Threading;
using System.Windows;
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
