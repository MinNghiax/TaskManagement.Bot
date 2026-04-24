using System.Globalization;
using Microsoft.Extensions.Configuration;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services.Reminders;

public enum ReminderRepeatUnit
{
    Minutes,
    Hours,
    Days,
    Weeks
}

public enum ReminderSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record TaskReminderPolicy
{
    public bool IsEnabled { get; init; } = true;
    public DeadlineReminderPolicy Deadline { get; init; } = new();
    public IReadOnlyList<StateReminderRule> StateRules { get; init; } = [];

    public bool HasAnyRule() => Deadline.HasAnyRule() || StateRules.Count > 0;
}

public sealed record DeadlineReminderPolicy
{
    public IReadOnlyList<TimeSpan> BeforeDeadlineOffsets { get; init; } = [TimeSpan.FromMinutes(30)];
    public bool NotifyAtDeadline { get; init; } = true;
    public TimeSpan? AfterDeadlineOffset { get; init; } = TimeSpan.FromMinutes(10);

    public bool HasAnyRule() =>
        BeforeDeadlineOffsets.Count > 0
        || NotifyAtDeadline
        || (AfterDeadlineOffset.HasValue && AfterDeadlineOffset.Value > TimeSpan.Zero);
}

public sealed record StateReminderRule(
    ETaskStatus State,
    ReminderRepeatRule Repeat,
    ReminderSeverity Severity,
    string Description);

public sealed record ReminderRepeatRule(int Every, ReminderRepeatUnit Unit)
{
    public TimeSpan ToTimeSpan() =>
        Unit switch
        {
            ReminderRepeatUnit.Minutes => TimeSpan.FromMinutes(Every),
            ReminderRepeatUnit.Hours => TimeSpan.FromHours(Every),
            ReminderRepeatUnit.Days => TimeSpan.FromDays(Every),
            ReminderRepeatUnit.Weeks => TimeSpan.FromDays(Every * 7d),
            _ => throw new InvalidOperationException($"Unsupported repeat unit '{Unit}'.")
        };

    public string ToDisplayText() =>
        Unit switch
        {
            ReminderRepeatUnit.Minutes => $"moi {Every} phut",
            ReminderRepeatUnit.Hours => $"moi {Every} gio",
            ReminderRepeatUnit.Days => $"moi {Every} ngay",
            ReminderRepeatUnit.Weeks => $"moi {Every} tuan",
            _ => $"moi {Every} lan"
        };
}

public sealed record WorkingHoursConfiguration(
    TimeOnly StartLocalTime,
    TimeOnly EndLocalTime,
    IReadOnlySet<DayOfWeek> WorkingDays)
{
    public static WorkingHoursConfiguration Parse(
        string start,
        string end,
        IReadOnlyCollection<string>? workingDays)
    {
        if (!TimeOnly.TryParseExact(start, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startLocalTime))
            throw new ArgumentException("JobSettings:WorkingHoursStart must use HH:mm format.");

        if (!TimeOnly.TryParseExact(end, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endLocalTime))
            throw new ArgumentException("JobSettings:WorkingHoursEnd must use HH:mm format.");

        if (startLocalTime >= endLocalTime)
            throw new ArgumentException("JobSettings working hours require WorkingHoursStart < WorkingHoursEnd.");

        return new WorkingHoursConfiguration(startLocalTime, endLocalTime, ParseWorkingDays(workingDays));
    }

    public bool Contains(DateTimeOffset utcDateTime, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
        var localTime = TimeOnly.FromDateTime(localDateTime.DateTime);

        return IsWorkingDay(localDateTime.Date.DayOfWeek)
            && localTime >= StartLocalTime
            && localTime <= EndLocalTime;
    }

    public DateTimeOffset NormalizeReportTimeUtc(DateTimeOffset dueAtUtc, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var localDateTime = TimeZoneInfo.ConvertTime(dueAtUtc, timeZone);
        var localDate = localDateTime.Date;
        var localTime = TimeOnly.FromDateTime(localDateTime.DateTime);

        if (!IsWorkingDay(localDate.DayOfWeek))
        {
            var nextWorkingDate = GetNextWorkingDate(localDate);
            return CreateZonedDateTimeOffset(nextWorkingDate, StartLocalTime, timeZone).ToUniversalTime();
        }

        if (localTime < StartLocalTime)
            return CreateZonedDateTimeOffset(localDate, StartLocalTime, timeZone).ToUniversalTime();

        if (localTime > EndLocalTime)
        {
            var nextWorkingDate = GetNextWorkingDate(localDate.AddDays(1));
            return CreateZonedDateTimeOffset(nextWorkingDate, StartLocalTime, timeZone).ToUniversalTime();
        }

        return dueAtUtc;
    }

    public DateTimeOffset GetPreviousWindowEndUtc(DateTimeOffset utcDateTime, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
        var localTime = TimeOnly.FromDateTime(localDateTime.DateTime);
        var boundaryDate = IsWorkingDay(localDateTime.Date.DayOfWeek) && localTime > EndLocalTime
            ? localDateTime.Date
            : GetPreviousWorkingDate(localDateTime.Date.AddDays(-1));

        return CreateZonedDateTimeOffset(boundaryDate, EndLocalTime, timeZone).ToUniversalTime();
    }

    private static HashSet<DayOfWeek> ParseWorkingDays(IReadOnlyCollection<string>? workingDays)
    {
        if (workingDays is null || workingDays.Count == 0)
            throw new ArgumentException("JobSettings:WorkingDays must contain at least one day.");

        HashSet<DayOfWeek> parsedWorkingDays = [];

        foreach (var rawWorkingDay in workingDays)
        {
            if (string.IsNullOrWhiteSpace(rawWorkingDay)
                || !Enum.TryParse(rawWorkingDay.Trim(), ignoreCase: true, out DayOfWeek workingDay)
                || !Enum.IsDefined(workingDay))
            {
                throw new ArgumentException(
                    $"JobSettings:WorkingDays contains invalid day '{rawWorkingDay}'. Use DayOfWeek names such as Monday or Friday.");
            }

            parsedWorkingDays.Add(workingDay);
        }

        return parsedWorkingDays;
    }

    private bool IsWorkingDay(DayOfWeek dayOfWeek) =>
        WorkingDays.Contains(dayOfWeek);

    private DateTime GetNextWorkingDate(DateTime localDate)
    {
        var candidate = localDate.Date;

        for (var i = 0; i < 7; i++)
        {
            if (IsWorkingDay(candidate.DayOfWeek))
                return candidate;

            candidate = candidate.AddDays(1);
        }

        throw new InvalidOperationException("No working day is configured.");
    }

    private DateTime GetPreviousWorkingDate(DateTime localDate)
    {
        var candidate = localDate.Date;

        for (var i = 0; i < 7; i++)
        {
            if (IsWorkingDay(candidate.DayOfWeek))
                return candidate;

            candidate = candidate.AddDays(-1);
        }

        throw new InvalidOperationException("No working day is configured.");
    }

    private static DateTimeOffset CreateZonedDateTimeOffset(
        DateTime localDate,
        TimeOnly localTime,
        TimeZoneInfo timeZone)
    {
        var localDateTime = localDate.Date + localTime.ToTimeSpan();

        while (timeZone.IsInvalidTime(localDateTime))
        {
            localDateTime = localDateTime.AddMinutes(1);
        }

        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }
}

public sealed record ReminderSchedulerConfiguration(
    TimeZoneInfo TimeZone,
    WorkingHoursConfiguration WorkingHours)
{
    public static ReminderSchedulerConfiguration Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("JobSettings");
        var workingDays = section.GetSection("WorkingDays").Get<string[]>()
            ?? DefaultWorkingDays();

        return Create(
            section["TimeZone"] ?? "UTC",
            section["WorkingHoursStart"] ?? "08:30",
            section["WorkingHoursEnd"] ?? "17:30",
            workingDays);
    }

    public static TimeZoneInfo CreateTimeZone(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return CreateTimeZone(configuration["JobSettings:TimeZone"] ?? "UTC");
    }

    public static ReminderSchedulerConfiguration Create(
        string timeZone,
        string workingHoursStart,
        string workingHoursEnd,
        IReadOnlyCollection<string>? workingDays)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new ArgumentException("JobSettings:TimeZone is required.");

        var timeZoneInfo = CreateTimeZone(timeZone);
        var workingHours = WorkingHoursConfiguration.Parse(
            workingHoursStart,
            workingHoursEnd,
            workingDays);

        return new ReminderSchedulerConfiguration(timeZoneInfo, workingHours);
    }

    public static TimeZoneInfo CreateTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new ArgumentException("JobSettings:TimeZone is required.");

        return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    }

    private static string[] DefaultWorkingDays() =>
    [
        nameof(DayOfWeek.Monday),
        nameof(DayOfWeek.Tuesday),
        nameof(DayOfWeek.Wednesday),
        nameof(DayOfWeek.Thursday),
        nameof(DayOfWeek.Friday)
    ];
}
