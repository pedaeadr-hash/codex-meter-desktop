using System.Text.Json;
using CodexMeter.Core;

var zone = TimeZoneInfo.CreateCustomTimeZone("UTC-3", TimeSpan.FromHours(-3), "UTC-3", "UTC-3");
var now = DateTimeOffset.Parse("2026-10-24T18:00:00Z");

using var limits = JsonDocument.Parse("""
{
  "rateLimitsByLimitId": {
    "codex": {
      "planType": "plus",
      "primary": { "usedPercent": 85, "windowDurationMins": 300, "resetsAt": 1792880816 },
      "secondary": { "usedPercent": 28, "windowDurationMins": 10080, "resetsAt": 1793395990 }
    }
  }
}
""");

using var usage = JsonDocument.Parse("""
{
  "summary": { "peakDailyTokens": 1000 },
  "dailyUsageBuckets": [
    { "startDate": "2026-10-23", "tokens": 300 },
    { "startDate": "2026-10-24", "tokens": 200 }
  ]
}
""");

var result = UsageNormalizer.Normalize(limits.RootElement, usage.RootElement, now, zone);

Assert(result.Primary?.Label == "5 horas", "rotulo da janela primaria");
Assert(result.Primary?.UsedPercent == 85, "percentual usado da janela primaria");
Assert(result.Primary?.RemainingPercent == 15, "percentual restante da janela primaria");
Assert(result.Secondary?.Label == "Semanal", "rotulo da janela secundaria");
Assert(result.Today.Tokens == 200, "tokens de hoje");
Assert(result.Today.ShareOfWeekPercent == 40, "participacao de hoje na semana");
Assert(result.Week.Tokens == 500, "tokens da semana");
Assert(result.Week.Daily.Count == 7, "sete dias de atividade");

using var clampedLimits = JsonDocument.Parse("""
{
  "rateLimits": {
    "primary": { "usedPercent": 160, "windowDurationMins": 300 },
    "secondary": { "usedPercent": -12, "windowDurationMins": 10080 }
  }
}
""");
using var emptyUsage = JsonDocument.Parse("{}");
var clamped = UsageNormalizer.Normalize(clampedLimits.RootElement, emptyUsage.RootElement, now, zone);

Assert(clamped.Primary?.UsedPercent == 100, "limite superior de percentual");
Assert(clamped.Secondary?.UsedPercent == 0, "limite inferior de percentual");

Console.WriteLine("Smoke tests: OK");
return;

static void Assert(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Falha: {name}");
    }
}
