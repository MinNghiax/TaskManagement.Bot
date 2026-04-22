using System.Globalization;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services.Reminders;

public static class ReviewAutoCompletePolicy
{
    public const string SupportedFormatExamples = "30m, 30p, 1h, 1d, 1w";

    public static TimeSpan? GetDelay(string? autoCompleteAfter)
    {
        if (!TryParseDelay(autoCompleteAfter, out var delay))
            return null;

        return delay;
    }

    public static DateTime? GetDueAtUtc(TaskItem task, string? autoCompleteAfter)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.Status != ETaskStatus.Review || !task.ReviewStartedAt.HasValue)
            return null;

        var delay = GetDelay(autoCompleteAfter);
        if (!delay.HasValue)
            return null;

        return NormalizeUtc(task.ReviewStartedAt.Value).Add(delay.Value);
    }

    public static bool IsDue(TaskItem task, string? autoCompleteAfter, DateTime nowUtc)
    {
        var dueAtUtc = GetDueAtUtc(task, autoCompleteAfter);
        return dueAtUtc.HasValue && dueAtUtc.Value <= NormalizeUtc(nowUtc);
    }

    private static bool TryParseDelay(string? rawValue, out TimeSpan delay)
    {
        delay = default;

        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        var normalized = rawValue.Trim().ToLowerInvariant();
        if (normalized.Length < 2)
            return false;

        var unitSuffix = normalized[^1];
        var numberPart = normalized[..^1];

        if (!int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            return false;

        delay = unitSuffix switch
        {
            'm' or 'p' => TimeSpan.FromMinutes(value),
            'h' or 'g' => TimeSpan.FromHours(value),
            'd' => TimeSpan.FromDays(value),
            'w' or 't' => TimeSpan.FromDays(value * 7d),
            _ => default
        };

        return delay > TimeSpan.Zero;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
