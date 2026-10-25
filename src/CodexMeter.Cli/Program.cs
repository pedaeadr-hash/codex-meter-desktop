using System.Diagnostics;
using System.Net.Http;

const string apiUrl = "http://127.0.0.1:5087";
var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();

if (command is null or "help" or "--help" or "-h")
{
    PrintHelp();
    return 0;
}

if (command == "minimizer")
{
    command = "minimize";
}

if (command is not ("open" or "off" or "minimize" or "status"))
{
    Console.Error.WriteLine($"Comando desconhecido: {command}");
    PrintHelp();
    return 2;
}

using var client = new HttpClient
{
    BaseAddress = new Uri(apiUrl),
    Timeout = TimeSpan.FromSeconds(2)
};

if (command == "open" && !await IsRunningAsync(client))
{
    var desktopPath = FindDesktopExecutable();
    if (desktopPath is null)
    {
        Console.Error.WriteLine("CodexMeter.exe nao foi encontrado. Reinstale o aplicativo.");
        return 1;
    }

    Process.Start(new ProcessStartInfo(desktopPath) { UseShellExecute = true });
    for (var attempt = 0; attempt < 30 && !await IsRunningAsync(client); attempt++)
    {
        await Task.Delay(150);
    }
}

try
{
    if (command == "status")
    {
        var running = await IsRunningAsync(client);
        Console.WriteLine(running ? "Codex Meter esta em execucao." : "Codex Meter esta fechado.");
        return running ? 0 : 1;
    }

    using var response = await client.PostAsync($"/api/app/{command}", null);
    response.EnsureSuccessStatusCode();
    Console.WriteLine(command switch
    {
        "open" => "Codex Meter aberto.",
        "off" => "Codex Meter encerrado.",
        _ => "Codex Meter ocultado."
    });
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Nao foi possivel controlar o Codex Meter: {exception.Message}");
    return 1;
}

static async Task<bool> IsRunningAsync(HttpClient client)
{
    try
    {
        using var response = await client.GetAsync("/api/app/status");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

static string? FindDesktopExecutable()
{
    var configured = Environment.GetEnvironmentVariable("CODEX_METER_DESKTOP");
    if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
    {
        return configured;
    }

    var installed = Path.Combine(AppContext.BaseDirectory, "CodexMeter.exe");
    return File.Exists(installed) ? installed : null;
}

static void PrintHelp()
{
    Console.WriteLine("Codex Meter para Windows");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  codex-meter open       Abre ou mostra o widget");
    Console.WriteLine("  codex-meter minimize   Oculta o widget");
    Console.WriteLine("  codex-meter minimizer  Alias de minimize");
    Console.WriteLine("  codex-meter off        Encerra o aplicativo");
    Console.WriteLine("  codex-meter status     Mostra se esta em execucao");
}
