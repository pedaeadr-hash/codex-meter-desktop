using System.Globalization;
using System.Text.Json;

namespace CodexMeter.Core;

public static class UsageNormalizer
{
    public static UsageSnapshot Normalize(
        JsonElement rateLimitPayload,
        JsonElement usagePayload,
        DateTimeOffset? now = null,
        TimeZoneInfo? timeZone = null)
    {
        var current = now ?? DateTimeOffset.UtcNow;
        var zone = timeZone ?? ResolveSaoPauloTimeZone();
        var localNow = TimeZoneInfo.ConvertTime(current, zone);
        var limits = SelectLimits(rateLimitPayload);
        var summary = TryProperty(usagePayload, "summary");
        var buckets = ReadBuckets(usagePayload);

        var dates = Enumerable.Range(0, 7)
            .Select(offset => DateOnly.FromDateTime(localNow.Date.AddDays(offset - 6)))
            .ToArray();

        var daily = dates
            .Select(date => new DailyUsage(date, buckets.GetValueOrDefault(date)))
            .ToArray();

        var todayTokens = buckets.GetValueOrDefault(DateOnly.FromDateTime(localNow.Date));
        var weekTokens = daily.Sum(day => day.Tokens);
        var peakDailyTokens = ReadNullableLong(summary, "peakDailyTokens") ?? 0;

        return new UsageSnapshot(
            current,
            ReadString(limits, "planType"),
            DescribeWindow(TryProperty(limits, "primary")),
            DescribeWindow(TryProperty(limits, "secondary")),
            new TodayUsage(
                todayTokens,
                peakDailyTokens > 0 ? Clamp(todayTokens * 100d / peakDailyTokens) : null,
                weekTokens > 0 ? Clamp(todayTokens * 100d / weekTokens) : null),
            new WeekUsage(weekTokens, daily),
            new UsageSummary(
                ReadNullableLong(summary, "lifetimeTokens"),
                ReadNullableLong(summary, "peakDailyTokens"),
                ReadNullableInt(summary, "currentStreakDays")));
    }

    private static JsonElement SelectLimits(JsonElement payload)
    {
        var byId = TryProperty(payload, "rateLimitsByLimitId");
        var codex = TryProperty(byId, "codex");
        return codex.ValueKind == JsonValueKind.Object
            ? codex
            : TryProperty(payload, "rateLimits");
    }

    private static Dictionary<DateOnly, long> ReadBuckets(JsonElement payload)
    {
        var result = new Dictionary<DateOnly, long>();
        var buckets = TryProperty(payload, "dailyUsageBuckets");
        if (buckets.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var bucket in buckets.EnumerateArray())
        {
            var key = ReadString(bucket, "startDate");
            if (DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                result[date] = ReadNullableLong(bucket, "tokens") ?? 0;
            }
        }

        return result;
    }

    private static UsageWindow? DescribeWindow(JsonElement window)
    {
        if (window.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var duration = ReadNullableInt(window, "windowDurationMins") ?? 0;
        var used = Clamp(ReadNullableDouble(window, "usedPercent") ?? 0);
        var label = duration switch
        {
            300 => "5 horas",
            1440 => "Diario",
            10080 => "Semanal",
            _ => $"{Math.Round(duration / 60d):0} horas"
        };

        DateTimeOffset? resetsAt = null;
        var resetSeconds = ReadNullableLong(window, "resetsAt");
        if (resetSeconds is > 0)
        {
            resetsAt = DateTimeOffset.FromUnixTimeSeconds(resetSeconds.Value);
        }

        return new UsageWindow(label, used, Clamp(100 - used), duration, resetsAt);
    }

    private static TimeZoneInfo ResolveSaoPauloTimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private static JsonElement TryProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value
            : default;

    private static string? ReadString(JsonElement element, string name)
    {
        var value = TryProperty(element, name);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static long? ReadNullableLong(JsonElement element, string name)
    {
        var value = TryProperty(element, name);
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        return null;
    }

    private static int? ReadNullableInt(JsonElement element, string name)
    {
        var value = ReadNullableLong(element, name);
        return value is >= int.MinValue and <= int.MaxValue ? (int)value.Value : null;
    }

    private static double? ReadNullableDouble(JsonElement element, string name)
    {
        var value = TryProperty(element, name);
        return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            ? number
            : null;
    }

    private static double Clamp(double value) => Math.Min(100, Math.Max(0, value));
}
