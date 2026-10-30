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
    private double? _primaryRemainingPercent;
    private DateTimeOffset? _resetAt;
    private bool _showCountdown;
    private string _countdownText = "Renovação indisponível";
    public string CountdownText { get => _countdownText; private set => SetField(ref _countdownText, value); }
    public string ResetDisplay => _showCountdown ? CountdownText : ResetText;

    public void SetCountdown(bool enabled)
    {
        _showCountdown = enabled;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetDisplay)));
    }

    public double? PrimaryRemainingPercent
    {
        get => _primaryRemainingPercent;
        private set => SetField(ref _primaryRemainingPercent, value);
    }

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
        private set { SetField(ref _resetText, value); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetDisplay))); }
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
            _resetAt = primary?.ResetsAt;
            UpdateCountdown();

            PrimaryRemainingText = primary is null ? "--%" : $"{primary.RemainingPercent:0}%";
            PrimaryRemainingPercent = primary?.RemainingPercent;
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
                ? "Renovação indisponível"
                : FormatReset(primary.ResetsAt.Value);
        }
        catch
        {
            PrimaryRemainingText = "--%";
            _resetAt = null;
            UpdateCountdown();
            PrimaryRemainingPercent = null;
            PrimaryCaption = "CODEX INDISPONIVEL";
            PlanText = "Codex Meter";
            WeeklyText = "Semana indisponível";
            ResetText = "Tentando novamente";
            WeeklyUsedPercent = 0;
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void UpdateCountdown()
    {
        var remaining = _resetAt - DateTimeOffset.Now;
        CountdownText = remaining is null ? "Renovação indisponível" : remaining <= TimeSpan.Zero ? "Renovação em instantes" : $"Volta em {(int)remaining.Value.TotalHours}h {remaining.Value.Minutes:00}min";
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResetDisplay)));
    }

    private static string FormatReset(DateTimeOffset resetAt) =>
        $"5 h · volta às {resetAt.ToLocalTime():HH:mm}";

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
