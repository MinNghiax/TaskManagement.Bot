using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;

namespace TaskManagement.Bot.Tests;

public class ReminderProcessorTests
{
    [Fact]
    public async Task ProcessDueRemindersAsync_SendsDueOneTimeReminderAndMarksSent()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var reminder = CreateReminder(isRepeat: false);

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal("user-1", sender.Sent[0].TargetUserId);
        Assert.Equal(EReminderStatus.Sent, reminder.Status);
        Assert.Null(reminder.NextTriggerAt);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_SchedulesNextTriggerForRepeatingReminder()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var reminder = CreateReminder(isRepeat: true);
        reminder.NextTriggerAt = DateTime.UtcNow.AddMinutes(-1);

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal("user-1", sender.Sent[0].TargetUserId);
        Assert.Equal(EReminderStatus.Pending, reminder.Status);
        Assert.NotNull(reminder.NextTriggerAt);
        Assert.True(reminder.NextTriggerAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenOneReminderFails_ProcessesOtherDueReminders()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender
        {
            FailedTargetUserId = "user-fail"
        };
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var failedReminder = CreateReminder(isRepeat: false, targetUserId: "user-fail", triggerAt: DateTime.UtcNow.AddMinutes(-2));
        var successfulReminder = CreateReminder(isRepeat: false, targetUserId: "user-ok", triggerAt: DateTime.UtcNow.AddMinutes(-1));

        context.Reminders.AddRange(failedReminder, successfulReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal("user-ok", sender.Sent[0].TargetUserId);
        Assert.Equal(EReminderStatus.Pending, failedReminder.Status);
        Assert.Equal(EReminderStatus.Sent, successfulReminder.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_AutoCompletesReviewTaskWhenPolicyIsDue()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(
            context,
            sender,
            NullLogger<ReminderProcessor>.Instance,
            CreateConfiguration("30p"));
        var task = new TaskItem
        {
            Title = "Review task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddHours(1),
            Status = ETaskStatus.Review,
            ReviewStartedAt = DateTime.UtcNow.AddMinutes(-31)
        };
        var reminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddMinutes(-5),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = ETaskStatus.Review,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 5,
                TaskStatus = ETaskStatus.Review,
                IsRepeat = true
            }
        };

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Empty(sender.Sent);
        Assert.Equal(ETaskStatus.Completed, task.Status);
        Assert.Null(task.ReviewStartedAt);
        Assert.Equal(EReminderStatus.Cancelled, reminder.Status);
        Assert.Null(reminder.NextTriggerAt);
    }

    private static Reminder CreateReminder(
        bool isRepeat,
        string targetUserId = "user-1",
        DateTime? triggerAt = null)
    {
        var task = new TaskItem
        {
            Title = "Reminder task",
            AssignedTo = targetUserId,
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddHours(1),
            Status = ETaskStatus.ToDo
        };

        return new Reminder
        {
            Task = task,
            TriggerAt = triggerAt ?? DateTime.UtcNow.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = isRepeat ? EReminderTriggerType.Repeat : EReminderTriggerType.BeforeDeadline,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 5,
                TaskStatus = task.Status,
                IsRepeat = isRepeat
            }
        };
    }

    private static TaskManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskManagementDbContext(options);
    }

    private static IConfiguration CreateConfiguration(string autoCompleteAfter) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobSettings:Review:AutoCompleteAfter"] = autoCompleteAfter
            })
            .Build();

    private sealed class FakeReminderNotificationSender : IReminderNotificationSender
    {
        public List<(int ReminderId, string TargetUserId)> Sent { get; } = [];
        public string? FailedTargetUserId { get; init; }

        public Task SendAsync(Reminder reminder, CancellationToken cancellationToken)
        {
            if (reminder.TargetUserId == FailedTargetUserId)
            {
                throw new InvalidOperationException("Send failed.");
            }

            Sent.Add((reminder.Id, reminder.TargetUserId));
            return Task.CompletedTask;
        }
    }
}
