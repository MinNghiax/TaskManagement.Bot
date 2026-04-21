using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;

namespace TaskManagement.Bot.Tests;

public class ReminderScheduleBuilderTests
{
    [Fact]
    public void CreateRules_BuildsDeadlineAndStateRulesFromPolicy()
    {
        var policy = new TaskReminderPolicy
        {
            Deadline = new DeadlineReminderPolicy
            {
                BeforeDeadlineOffsets = [TimeSpan.FromMinutes(30)],
                NotifyAtDeadline = true,
                AfterDeadlineOffset = TimeSpan.FromMinutes(10)
            },
            StateRules =
            [
                new StateReminderRule(
                    ETaskStatus.Review,
                    new ReminderRepeatRule(1, ReminderRepeatUnit.Hours),
                    ReminderSeverity.Warning,
                    "Review repeat")
            ]
        };

        var rules = ReminderScheduleBuilder.CreateRules(policy);

        Assert.Equal(6, rules.Count);
        Assert.Contains(rules, rule =>
            rule.TriggerType == EReminderTriggerType.BeforeDeadline &&
            rule.TaskStatus == ETaskStatus.Doing &&
            rule.IntervalUnit == ETimeUnit.Minutes &&
            rule.Value == 30);
        Assert.Contains(rules, rule =>
            rule.TriggerType == EReminderTriggerType.OnDeadline &&
            rule.TaskStatus == ETaskStatus.Review);
        Assert.Contains(rules, rule =>
            rule.TriggerType == EReminderTriggerType.AfterDeadline &&
            rule.TaskStatus == ETaskStatus.Late &&
            rule.IntervalUnit == ETimeUnit.Minutes &&
            rule.Value == 10);
        Assert.Contains(rules, rule =>
            rule.TriggerType == EReminderTriggerType.Repeat &&
            rule.TaskStatus == ETaskStatus.Review &&
            rule.IsRepeat);
    }

    [Fact]
    public void RefreshReminders_CreatesPendingRemindersForRules()
    {
        var task = new TaskItem
        {
            Id = 42,
            Title = "Reminder task",
            AssignedTo = "user-1",
            CreatedBy = "pm",
            DueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc),
            Status = ETaskStatus.Review,
            ReviewStartedAt = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc)
        };
        var rules = ReminderScheduleBuilder.CreateRules(new TaskReminderPolicy
        {
            Deadline = new DeadlineReminderPolicy
            {
                BeforeDeadlineOffsets = [TimeSpan.Zero],
                NotifyAtDeadline = true,
                AfterDeadlineOffset = null
            },
            StateRules =
            [
                new StateReminderRule(
                    ETaskStatus.Review,
                    new ReminderRepeatRule(30, ReminderRepeatUnit.Minutes),
                    ReminderSeverity.Warning,
                    "Review repeat")
            ]
        });

        var reminders = ReminderScheduleBuilder.RefreshReminders(task, [], rules);

        Assert.Equal(3, reminders.Count);
        Assert.All(reminders, reminder => Assert.Equal(EReminderStatus.Pending, reminder.Status));
        Assert.Contains(reminders, reminder =>
            reminder.ReminderRule!.TriggerType == EReminderTriggerType.OnDeadline &&
            reminder.ReminderRule.TaskStatus == ETaskStatus.Doing &&
            reminder.TriggerAt == task.DueDate);
        Assert.Contains(reminders, reminder =>
            reminder.ReminderRule!.TriggerType == EReminderTriggerType.Repeat &&
            reminder.TriggerAt == task.ReviewStartedAt.Value.AddMinutes(30));
    }

    [Fact]
    public void GetNextReportAtUtc_NormalizesPendingReminderIntoWorkingHours()
    {
        var task = new TaskItem
        {
            Id = 42,
            Title = "Reminder task",
            AssignedTo = "user-1",
            CreatedBy = "pm",
            DueDate = new DateTime(2026, 4, 18, 2, 0, 0, DateTimeKind.Utc),
            Status = ETaskStatus.Doing
        };
        var rule = new ReminderRule
        {
            Id = 1,
            TriggerType = EReminderTriggerType.OnDeadline,
            TaskStatus = ETaskStatus.Doing
        };
        task.Reminders.Add(new Reminder
        {
            TaskId = task.Id,
            ReminderRuleId = rule.Id,
            TriggerAt = task.DueDate.Value,
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = ETaskStatus.Doing,
            ReminderRule = rule,
            Task = task
        });
        var configuration = ReminderSchedulerConfiguration.Create(
            "SE Asia Standard Time",
            "08:30",
            "17:30",
            ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]);
        var nowUtc = new DateTimeOffset(2026, 4, 18, 1, 0, 0, TimeSpan.Zero);

        var nextReportAtUtc = ReminderScheduleBuilder.GetNextReportAtUtc(task, configuration, nowUtc);

        Assert.Equal(
            new DateTimeOffset(2026, 4, 20, 1, 30, 0, TimeSpan.Zero),
            nextReportAtUtc);
    }
}
