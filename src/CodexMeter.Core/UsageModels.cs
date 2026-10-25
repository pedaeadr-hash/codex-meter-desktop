namespace CodexMeter.Core;

public sealed record UsageWindow(
    string Label,
    double UsedPercent,
    double RemainingPercent,
    int WindowDurationMinutes,
    DateTimeOffset? ResetsAt);

public sealed record DailyUsage(DateOnly Date, long Tokens);

public sealed record TodayUsage(
    long Tokens,
    double? VersusPeakPercent,
    double? ShareOfWeekPercent);

public sealed record WeekUsage(long Tokens, IReadOnlyList<DailyUsage> Daily);

public sealed record UsageSummary(
    long? LifetimeTokens,
    long? PeakDailyTokens,
    int? CurrentStreakDays);

public sealed record UsageSnapshot(
    DateTimeOffset UpdatedAt,
    string? PlanType,
    UsageWindow? Primary,
    UsageWindow? Secondary,
    TodayUsage Today,
    WeekUsage Week,
    UsageSummary Summary);

public interface IUsageProvider
{
    Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default);
}
