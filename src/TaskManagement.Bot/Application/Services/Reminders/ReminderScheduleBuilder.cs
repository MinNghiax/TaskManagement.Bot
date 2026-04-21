using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services.Reminders;

public static class ReminderScheduleBuilder
{
    public static IReadOnlyList<ReminderRule> CreateRules(TaskReminderPolicy reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        List<ReminderRule> rules = [];
        var nextRuleId = 1;

        var beforeOffsets = reminder.Deadline.BeforeDeadlineOffsets.Count == 0
            ? [TimeSpan.FromMinutes(30)]
            : reminder.Deadline.BeforeDeadlineOffsets;

        foreach (var offset in beforeOffsets
                     .Where(offset => offset > TimeSpan.Zero)
                     .Distinct()
                     .OrderByDescending(offset => offset))
        {
            var (value, unit) = ToTimeUnit(offset);

            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.TimeBefore,
                ETaskStatus.Doing,
                value,
                unit));

            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.TimeBefore,
                ETaskStatus.Review,
                value,
                unit));
        }

        if (reminder.Deadline.NotifyAtDeadline)
        {
            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.OnDeadline,
                ETaskStatus.Doing));

            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.OnDeadline,
                ETaskStatus.Review));
        }

        var lateOffset = reminder.Deadline.AfterDeadlineOffset;
        if (lateOffset.HasValue && lateOffset.Value > TimeSpan.Zero)
        {
            var (value, unit) = ToTimeUnit(lateOffset.Value);

            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.TimeAfter,
                ETaskStatus.Late,
                value,
                unit));
        }

        foreach (var stateRule in reminder.StateRules
                     .Distinct()
                     .OrderBy(rule => rule.State)
                     .ThenBy(rule => rule.Repeat.ToTimeSpan()))
        {
            var interval = stateRule.Repeat.ToTimeSpan();
            var (value, unit) = ToTimeUnit(interval);

            rules.Add(CreateRule(
                nextRuleId++,
                EReminderTriggerType.Repeat,
                stateRule.State,
                value,
                unit,
                isRepeat: true));
        }

        return rules;
    }

    public static List<Reminder> BuildTaskReminderEntities(
        TaskItem task,
        IEnumerable<CreateReminderRuleDto> reminderRules)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(reminderRules);

        var reminders = new List<Reminder>();

        if (task.DueDate.HasValue && IsTaskActive(task))
            reminders.Add(CreateOnDeadlineReminder(task));

        reminders.AddRange(BuildCustomReminderEntities(task, reminderRules));

        return reminders;
    }

    public static List<Reminder> BuildCustomReminderEntities(
        TaskItem task,
        IEnumerable<CreateReminderRuleDto> reminderRules)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(reminderRules);

        var validRules = reminderRules
            .Where(rule => rule.TriggerType != EReminderTriggerType.OnDeadline)
            .Where(IsValidReminderRule)
            .ToList();

        if (validRules.Count == 0)
            return [];

        if (!task.DueDate.HasValue)
            throw new InvalidOperationException("Cannot create task reminders without a due date.");

        return validRules.Select(ruleDto =>
        {
            var triggerAt = CalculateTriggerAt(task.DueDate.Value, ruleDto);
            return new Reminder
            {
                TaskId = task.Id,
                TriggerAt = triggerAt,
                TargetUserId = task.AssignedTo,
                Status = EReminderStatus.Pending,
                NextTriggerAt = ruleDto.IsRepeat ? triggerAt : null,
                StateSnapshot = task.Status,
                ReminderRule = new ReminderRule
                {
                    TriggerType = ruleDto.TriggerType,
                    IntervalUnit = ruleDto.IntervalUnit,
                    Value = ruleDto.Value,
                    TaskStatus = task.Status,
                    IsRepeat = ruleDto.IsRepeat
                }
            };
        }).ToList();
    }

    public static List<Reminder> RefreshReminders(
        TaskItem task,
        IReadOnlyList<Reminder> existingReminders,
        IReadOnlyList<ReminderRule> reminderRules,
        bool resetRepeats = false)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(existingReminders);
        ArgumentNullException.ThrowIfNull(reminderRules);

        var reminders = existingReminders
            .Select(CloneReminder)
            .ToList();

        foreach (var reminder in reminders)
        {
            reminder.ReminderRule = GetRule(reminderRules, reminder.ReminderRuleId)
                ?? reminder.ReminderRule;
            reminder.Task = task;
        }

        if (resetRepeats)
        {
            foreach (var reminder in reminders.Where(reminder =>
                         reminder.Status == EReminderStatus.Pending
                         && IsRepeatRule(GetRule(reminderRules, reminder.ReminderRuleId) ?? reminder.ReminderRule)))
            {
                reminder.Status = EReminderStatus.Cancelled;
                reminder.NextTriggerAt = null;
            }
        }

        if (!IsTaskActive(task))
            return reminders;

        foreach (var rule in reminderRules.OrderBy(rule => rule.Id))
        {
            var hasPendingReminder = reminders.Any(reminder =>
                RuleMatchesReminder(rule, reminder)
                && reminder.Status == EReminderStatus.Pending);

            if (hasPendingReminder)
                continue;

            var hasOneShotHistory = !IsRepeatRule(rule)
                && reminders.Any(reminder => RuleMatchesReminder(rule, reminder));

            if (hasOneShotHistory)
                continue;

            var triggerAtUtc = BuildTriggerAtUtc(task, rule);
            if (!triggerAtUtc.HasValue)
                continue;

            reminders.Add(new Reminder
            {
                TaskId = task.Id,
                ReminderRuleId = rule.Id,
                TriggerAt = triggerAtUtc.Value.UtcDateTime,
                TargetUserId = task.AssignedTo,
                Status = EReminderStatus.Pending,
                NextTriggerAt = triggerAtUtc.Value.UtcDateTime,
                StateSnapshot = rule.TaskStatus,
                ReminderRule = rule,
                Task = task
            });
        }

        foreach (var reminder in reminders.Where(reminder =>
                     reminder.Status == EReminderStatus.Pending
                     && !reminder.NextTriggerAt.HasValue))
        {
            reminder.NextTriggerAt = reminder.TriggerAt;
        }

        return reminders;
    }

    public static ReminderRule? GetRule(TaskItem task, int reminderRuleId)
    {
        ArgumentNullException.ThrowIfNull(task);

        return task.Reminders
            .Select(reminder => reminder.ReminderRule)
            .Where(rule => rule is not null)
            .DistinctBy(rule => rule!.Id)
            .FirstOrDefault(rule => rule!.Id == reminderRuleId);
    }

    public static ReminderRule? GetRule(IEnumerable<ReminderRule> rules, int reminderRuleId)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.FirstOrDefault(rule => rule.Id == reminderRuleId);
    }

    public static Reminder CreateOnDeadlineReminder(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.DueDate.HasValue)
            throw new InvalidOperationException("Cannot create an on-deadline reminder without a due date.");

        return new Reminder
        {
            TaskId = task.Id,
            TriggerAt = task.DueDate.Value,
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            NextTriggerAt = null,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                IntervalUnit = null,
                Value = 0,
                TaskStatus = task.Status,
                IsRepeat = false
            }
        };
    }

    public static void SyncOnDeadlineReminder(
        TaskItem task,
        Action<Reminder> addReminder,
        bool resetSchedule,
        DateTime? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(addReminder);

        var now = nowUtc ?? DateTime.UtcNow;
        var reminder = task.Reminders.FirstOrDefault(r =>
            r.ReminderRule?.TriggerType == EReminderTriggerType.OnDeadline);

        if (!task.DueDate.HasValue || !IsTaskActive(task))
        {
            CancelPendingReminder(reminder, now);
            return;
        }

        if (reminder is null)
        {
            addReminder(CreateOnDeadlineReminder(task));
            return;
        }

        if (resetSchedule || reminder.Status == EReminderStatus.Cancelled)
        {
            reminder.TriggerAt = task.DueDate.Value;
            reminder.NextTriggerAt = null;
            reminder.Status = EReminderStatus.Pending;
        }
        else if (reminder.Status == EReminderStatus.Pending)
        {
            reminder.TriggerAt = task.DueDate.Value;
            reminder.NextTriggerAt = null;
        }

        if (reminder.Status == EReminderStatus.Pending)
            reminder.TargetUserId = task.AssignedTo;

        reminder.StateSnapshot = task.Status;
        reminder.UpdatedAt = now;

        if (reminder.ReminderRule is not null)
        {
            reminder.ReminderRule.TaskStatus = task.Status;
            reminder.ReminderRule.UpdatedAt = now;
        }
    }

    public static DateTimeOffset? GetCurrentDueAtUtc(Reminder reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        var dueAtUtc = reminder.NextTriggerAt ?? reminder.TriggerAt;
        return dueAtUtc == default
            ? null
            : new DateTimeOffset(NormalizeUtc(dueAtUtc), TimeSpan.Zero);
    }

    public static DateTimeOffset? GetNextReportAtUtc(
        TaskItem task,
        ReminderSchedulerConfiguration schedulerConfiguration,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(schedulerConfiguration);

        DateTimeOffset? nextReportAtUtc = null;

        foreach (var reminder in task.Reminders.Where(reminder => reminder.Status == EReminderStatus.Pending))
        {
            var rule = reminder.ReminderRule ?? GetRule(task, reminder.ReminderRuleId);
            if (rule is null)
                continue;

            var dueAtUtc = GetCurrentDueAtUtc(reminder);
            if (!dueAtUtc.HasValue)
                continue;

            var reportAtUtc = schedulerConfiguration.WorkingHours.NormalizeReportTimeUtc(
                dueAtUtc.Value,
                schedulerConfiguration.TimeZone);

            if (reportAtUtc < nowUtc)
                reportAtUtc = nowUtc;

            if (!nextReportAtUtc.HasValue || reportAtUtc < nextReportAtUtc.Value)
                nextReportAtUtc = reportAtUtc;
        }

        return nextReportAtUtc;
    }

    public static bool IsRepeatRule(ReminderRule? rule) =>
        rule is not null && (rule.IsRepeat || rule.TriggerType == EReminderTriggerType.Repeat);

    public static TimeSpan? GetRepeatInterval(ReminderRule? rule)
    {
        if (!IsRepeatRule(rule))
            return null;

        return rule!.IntervalUnit.HasValue && rule.Value > 0
            ? ToTimeSpan(rule.Value, rule.IntervalUnit.Value)
            : null;
    }

    public static void ApplyNextSchedule(Reminder reminder, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        var interval = GetRepeatInterval(reminder.ReminderRule);
        if (!interval.HasValue || interval.Value <= TimeSpan.Zero)
        {
            reminder.Status = EReminderStatus.Sent;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = nowUtc;
            return;
        }

        var nextTriggerAt = (reminder.NextTriggerAt ?? reminder.TriggerAt).Add(interval.Value);
        while (nextTriggerAt <= nowUtc)
        {
            nextTriggerAt = nextTriggerAt.Add(interval.Value);
        }

        reminder.NextTriggerAt = nextTriggerAt;
        reminder.UpdatedAt = nowUtc;
    }

    public static bool IsTaskActive(TaskItem task) =>
        task.Status is not (ETaskStatus.Completed or ETaskStatus.Cancelled);

    public static bool IsValidReminderRule(CreateReminderRuleDto rule) =>
        rule.Value > 0 &&
        Enum.IsDefined(typeof(ETimeUnit), rule.IntervalUnit) &&
        Enum.IsDefined(typeof(EReminderTriggerType), rule.TriggerType);

    public static DateTime CalculateTriggerAt(DateTime dueDate, CreateReminderRuleDto rule)
    {
        var interval = ToTimeSpan(rule.Value, rule.IntervalUnit);
        return rule.TriggerType switch
        {
            EReminderTriggerType.BeforeDeadline => dueDate.Subtract(interval),
            EReminderTriggerType.AfterDeadline => dueDate.Add(interval),
            EReminderTriggerType.Repeat => DateTime.UtcNow.Add(interval),
            _ => dueDate
        };
    }

    public static TimeSpan ToTimeSpan(double value, ETimeUnit unit) =>
        unit switch
        {
            ETimeUnit.Minutes => TimeSpan.FromMinutes(value),
            ETimeUnit.Hours => TimeSpan.FromHours(value),
            ETimeUnit.Days => TimeSpan.FromDays(value),
            ETimeUnit.Weeks => TimeSpan.FromDays(value * 7),
            _ => TimeSpan.Zero
        };

    public static TimeSpan ToTimeSpan(ETimeUnit unit, double value) =>
        ToTimeSpan(value, unit);

    public static string FormatDuration(TimeSpan value)
    {
        if (value.TotalDays >= 7 && value.TotalDays % 7 == 0)
            return $"{value.TotalDays / 7:0} tuan";

        if (value.TotalDays >= 1 && value.TotalDays % 1 == 0)
            return $"{value.TotalDays:0} ngay";

        if (value.TotalHours >= 1 && value.TotalHours % 1 == 0)
            return $"{value.TotalHours:0} gio";

        return $"{value.TotalMinutes:0} phut";
    }

    private static void CancelPendingReminder(Reminder? reminder, DateTime nowUtc)
    {
        if (reminder?.Status != EReminderStatus.Pending)
            return;

        reminder.Status = EReminderStatus.Cancelled;
        reminder.NextTriggerAt = null;
        reminder.UpdatedAt = nowUtc;
    }

    private static ReminderRule CreateRule(
        int id,
        EReminderTriggerType triggerType,
        ETaskStatus taskStatus,
        double value = 0,
        ETimeUnit? intervalUnit = null,
        bool isRepeat = false) =>
        new()
        {
            Id = id,
            TriggerType = triggerType,
            IntervalUnit = intervalUnit,
            Value = value,
            TaskStatus = taskStatus,
            IsRepeat = isRepeat
        };

    private static Reminder CloneReminder(Reminder reminder) =>
        new()
        {
            Id = reminder.Id,
            TaskId = reminder.TaskId,
            ReminderRuleId = reminder.ReminderRuleId,
            TriggerAt = reminder.TriggerAt,
            TargetUserId = reminder.TargetUserId,
            Status = reminder.Status,
            NextTriggerAt = reminder.NextTriggerAt,
            StateSnapshot = reminder.StateSnapshot,
            ReminderRule = reminder.ReminderRule,
            Task = reminder.Task,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt,
            IsDeleted = reminder.IsDeleted
        };

    private static DateTimeOffset? BuildTriggerAtUtc(TaskItem task, ReminderRule rule) =>
        rule.TriggerType switch
        {
            EReminderTriggerType.TimeBefore => ToUtcOffset(task.DueDate) - GetRuleOffset(rule),
            EReminderTriggerType.TimeAfter => ToUtcOffset(task.DueDate) + GetRuleOffset(rule),
            EReminderTriggerType.OnDeadline => ToUtcOffset(task.DueDate),
            EReminderTriggerType.Repeat => BuildRepeatTriggerAtUtc(task, rule),
            _ => null
        };

    private static DateTimeOffset? BuildRepeatTriggerAtUtc(TaskItem task, ReminderRule rule)
    {
        var anchorUtc = GetAnchorUtc(task, rule.TaskStatus);
        if (!anchorUtc.HasValue)
            return null;

        var repeatInterval = GetRepeatInterval(rule);
        if (!repeatInterval.HasValue || repeatInterval.Value <= TimeSpan.Zero)
            return null;

        return anchorUtc.Value + repeatInterval.Value;
    }

    private static DateTimeOffset? GetAnchorUtc(TaskItem task, ETaskStatus taskStatus) =>
        taskStatus switch
        {
            ETaskStatus.Doing => ToUtcOffset(task.UpdatedAt ?? task.CreatedAt),
            ETaskStatus.Review => ToUtcOffset(task.ReviewStartedAt),
            ETaskStatus.Late => ToUtcOffset(task.DueDate),
            _ => null
        };

    private static TimeSpan GetRuleOffset(ReminderRule rule)
    {
        if (!rule.IntervalUnit.HasValue || rule.Value <= 0)
            return TimeSpan.Zero;

        return ToTimeSpan(rule.Value, rule.IntervalUnit.Value);
    }

    private static bool RuleMatchesReminder(ReminderRule rule, Reminder reminder)
    {
        if (rule.Id > 0 && reminder.ReminderRuleId == rule.Id)
            return true;

        return reminder.ReminderRule is not null
            && reminder.ReminderRule.TriggerType == rule.TriggerType
            && reminder.ReminderRule.IntervalUnit == rule.IntervalUnit
            && Math.Abs(reminder.ReminderRule.Value - rule.Value) < 0.0001
            && reminder.ReminderRule.TaskStatus == rule.TaskStatus
            && reminder.ReminderRule.IsRepeat == rule.IsRepeat;
    }

    private static (double Value, ETimeUnit Unit) ToTimeUnit(TimeSpan offset)
    {
        if (offset.TotalDays >= 7 && offset.TotalDays % 7 == 0)
            return (offset.TotalDays / 7, ETimeUnit.Weeks);

        if (offset.TotalDays >= 1 && offset.TotalDays % 1 == 0)
            return (offset.TotalDays, ETimeUnit.Days);

        if (offset.TotalHours >= 1 && offset.TotalHours % 1 == 0)
            return (offset.TotalHours, ETimeUnit.Hours);

        return (Math.Max(offset.TotalMinutes, 1), ETimeUnit.Minutes);
    }

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return new DateTimeOffset(NormalizeUtc(value.Value), TimeSpan.Zero);
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
