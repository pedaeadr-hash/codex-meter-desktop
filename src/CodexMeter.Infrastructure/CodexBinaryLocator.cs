namespace CodexMeter.Infrastructure;

public static class CodexBinaryLocator
{
    public static string Resolve()
    {
        var configuredBinary = Environment.GetEnvironmentVariable("CODEX_BINARY");
        if (!string.IsNullOrWhiteSpace(configuredBinary))
        {
            var configuredPath = Normalize(configuredBinary);
            if (File.Exists(configuredPath))
            {
                return configuredPath;
            }

            throw new FileNotFoundException(
                $"O executavel definido em CODEX_BINARY nao foi encontrado: {configuredPath}",
                configuredPath);
        }

        var resolved = FindFirstExisting(EnumerateCandidates());
        return resolved ?? throw new FileNotFoundException(
            "O Codex CLI nao foi encontrado. Abra ou reinstale o Codex Desktop e tente novamente.");
    }

    public static string? FindFirstExisting(IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            try
            {
                var path = Normalize(candidate);
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return Path.Combine(directory.Trim(), "codex.exe");
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            var desktopBin = Path.Combine(localAppData, "OpenAI", "Codex", "bin");
            foreach (var candidate in EnumerateVersionedDesktopBinaries(desktopBin))
            {
                yield return candidate;
            }

            yield return Path.Combine(localAppData, "OpenAI", "Codex", "codex.exe");
            yield return Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "codex.exe");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "OpenAI", "Codex", "codex.exe");
            yield return Path.Combine(programFiles, "Codex", "codex.exe");
        }
    }

    private static IEnumerable<string> EnumerateVersionedDesktopBinaries(string binDirectory)
    {
        if (!Directory.Exists(binDirectory))
        {
            yield break;
        }

        string[] candidates;
        try
        {
            candidates = Directory.GetFiles(binDirectory, "codex.exe", SearchOption.AllDirectories);
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (var candidate in candidates
                     .OrderByDescending(File.GetLastWriteTimeUtc))
        {
            yield return candidate;
        }
    }

    private static string Normalize(string value) =>
        Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
}
