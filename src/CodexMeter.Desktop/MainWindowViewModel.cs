using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CodexMeter.Core;

namespace CodexMeter.Desktop;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IUsageProvider _usageProvider;
    private readonly DispatcherTimer _timer;
    private bool _refreshing;
    private string _primaryRemainingText = "--%";
    private string _primaryCaption = "JANELA DE 5 HORAS";
    private string _planText = "Codex Meter";
    private string _weeklyText = "Semana · aguardando dados";
    private string _resetText = "Conectando ao Codex";
    private double _weeklyUsedPercent;

    public MainWindowViewModel(IUsageProvider usageProvider)
    {
        _usageProvider = usageProvider;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
    }

    public string PrimaryRemainingText
    {
        get => _primaryRemainingText;
        private set => SetField(ref _primaryRemainingText, value);
    }

    public string PrimaryCaption
    {
        get => _primaryCaption;
        private set => SetField(ref _primaryCaption, value);
    }

    public string PlanText
    {
        get => _planText;
        private set => SetField(ref _planText, value);
    }

    public string WeeklyText
    {
        get => _weeklyText;
        private set => SetField(ref _weeklyText, value);
    }

    public string ResetText
    {
        get => _resetText;
        private set => SetField(ref _resetText, value);
    }

    public double WeeklyUsedPercent
    {
        get => _weeklyUsedPercent;
        private set => SetField(ref _weeklyUsedPercent, value);
    }

    public async Task RefreshAsync()
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            var usage = await _usageProvider.GetUsageAsync();
            var primary = usage.Primary;
            var secondary = usage.Secondary;

            PrimaryRemainingText = primary is null ? "--%" : $"{primary.RemainingPercent:0}%";
            PrimaryCaption = primary is null
                ? "JANELA DE 5 HORAS"
                : $"LIVRE · {primary.Label.ToUpperInvariant()}";
            PlanText = string.IsNullOrWhiteSpace(usage.PlanType)
                ? "Codex Meter"
                : $"Plano {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(usage.PlanType)}";
            WeeklyText = secondary is null
                ? $"Hoje · {FormatTokens(usage.Today.Tokens)} tokens"
                : $"Semana · {secondary.RemainingPercent:0}% livre";
            WeeklyUsedPercent = secondary?.UsedPercent ?? 0;
            ResetText = primary?.ResetsAt is null
                ? $"Atualizado {usage.UpdatedAt.ToLocalTime():HH:mm}"
                : FormatReset(primary.ResetsAt.Value);
        }
        catch
        {
            PrimaryRemainingText = "--%";
            PrimaryCaption = "CODEX INDISPONIVEL";
            PlanText = "Codex Meter";
            WeeklyText = "Abra o Codex e tente novamente";
            ResetText = "Clique no controle para atualizar";
            WeeklyUsedPercent = 0;
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static string FormatReset(DateTimeOffset resetAt)
    {
        var remaining = resetAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Renovando agora";
        }

        return remaining.TotalHours >= 1
            ? $"Renova em {(int)remaining.TotalHours}h {remaining.Minutes}min"
            : $"Renova em {Math.Max(1, remaining.Minutes)}min";
    }

    private static string FormatTokens(long tokens) => tokens switch
    {
        >= 1_000_000 => $"{tokens / 1_000_000d:0.#}M",
        >= 1_000 => $"{tokens / 1_000d:0.#}k",
        _ => tokens.ToString("N0", CultureInfo.CurrentCulture)
    };

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => _timer.Stop();
}
