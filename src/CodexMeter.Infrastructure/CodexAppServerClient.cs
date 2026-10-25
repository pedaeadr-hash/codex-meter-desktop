using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using CodexMeter.Core;

namespace CodexMeter.Infrastructure;

public sealed class CodexAppServerClient : IUsageProvider, IDisposable, IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private Process? _process;
    private long _requestId;
    private Task? _stdoutLoop;

    public async Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);

        var limitsTask = RequestAsync("account/rateLimits/read", cancellationToken);
        var usageTask = RequestAsync("account/usage/read", cancellationToken);
        await Task.WhenAll(limitsTask, usageTask);

        return UsageNormalizer.Normalize(await limitsTask, await usageTask);
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_process is { HasExited: false })
            {
                return;
            }

            var binary = CodexBinaryLocator.Resolve();
            var startInfo = new ProcessStartInfo(binary, "app-server")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory
            };

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                startInfo.Environment["HOME"] = userProfile;
                startInfo.Environment["CODEX_HOME"] = Path.Combine(userProfile, ".codex");
            }

            _process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Nao foi possivel iniciar o Codex App Server.");
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => FailPending("O Codex App Server foi encerrado.");
            _stdoutLoop = ReadOutputAsync(_process);
            _ = DrainErrorsAsync(_process);

            await RawRequestAsync(
                "initialize",
                new
                {
                    clientInfo = new
                    {
                        name = "codex_meter_desktop",
                        title = "Codex Meter Desktop",
                        version = "1.0.1"
                    }
                },
                cancellationToken);

            await SendAsync(new { method = "initialized", @params = new { } }, cancellationToken);
        }
        catch
        {
            StopProcess();
            throw;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private Task<JsonElement> RequestAsync(string method, CancellationToken cancellationToken) =>
        RawRequestAsync(method, new { }, cancellationToken);

    private async Task<JsonElement> RawRequestAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        if (_process is not { HasExited: false })
        {
            throw new InvalidOperationException("Codex App Server indisponivel.");
        }

        var id = Interlocked.Increment(ref _requestId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        using var registration = timeout.Token.Register(() =>
        {
            if (_pending.TryRemove(id, out var pending))
            {
                pending.TrySetException(new TimeoutException($"Tempo esgotado ao consultar {method}."));
            }
        });

        await SendAsync(new { method, id, @params = parameters }, cancellationToken);
        return await completion.Task;
    }

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var process = _process ?? throw new InvalidOperationException("Codex App Server indisponivel.");
        var json = JsonSerializer.Serialize(message);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadOutputAsync(Process process)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var idElement) || !idElement.TryGetInt64(out var id))
                {
                    continue;
                }

                if (!_pending.TryRemove(id, out var completion))
                {
                    continue;
                }

                if (root.TryGetProperty("error", out var error))
                {
                    var message = error.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString()
                        : "Falha no Codex App Server.";
                    completion.TrySetException(new InvalidOperationException(message));
                    continue;
                }

                var result = root.TryGetProperty("result", out var resultElement)
                    ? resultElement.Clone()
                    : default;
                completion.TrySetResult(result);
            }
        }
        catch (Exception exception)
        {
            FailPending(exception.Message);
        }
    }

    private static async Task DrainErrorsAsync(Process process)
    {
        while (await process.StandardError.ReadLineAsync() is not null)
        {
            // Stderr is intentionally drained so the child process cannot block.
        }
    }

    private void FailPending(string message)
    {
        foreach (var pair in _pending.ToArray())
        {
            if (_pending.TryRemove(pair.Key, out var completion))
            {
                completion.TrySetException(new InvalidOperationException(message));
            }
        }
    }

    private void StopProcess()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    public void Dispose()
    {
        StopProcess();
        _startLock.Dispose();
        _writeLock.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
