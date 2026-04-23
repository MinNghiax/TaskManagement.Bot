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
    public async Task ProcessDueRemindersAsync_WhenRepeatAndBeforeDeadlineAreDue_SendsOnlyBeforeDeadline()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = "Before deadline priority",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = now.AddHours(1),
            Status = ETaskStatus.ToDo
        };

        var beforeDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.BeforeDeadline,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 15,
                TaskStatus = task.Status
            }
        };

        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 5,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.AddRange(beforeDeadlineReminder, repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(2, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal(beforeDeadlineReminder.Id, sender.Sent[0].ReminderId);
        Assert.Equal(EReminderStatus.Sent, beforeDeadlineReminder.Status);
        Assert.Equal(EReminderStatus.Pending, repeatReminder.Status);
        Assert.NotNull(repeatReminder.NextTriggerAt);
        Assert.True(repeatReminder.NextTriggerAt > now);
        Assert.Equal(ETaskStatus.ToDo, task.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenRepeatAndOnDeadlineAreDue_SendsOnDeadlineAndCancelsRepeat()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = "On deadline priority",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = now.AddMinutes(-1),
            Status = ETaskStatus.Doing
        };

        var onDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                TaskStatus = task.Status
            }
        };

        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 10,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.AddRange(onDeadlineReminder, repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(2, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal(onDeadlineReminder.Id, sender.Sent[0].ReminderId);
        Assert.Equal(EReminderStatus.Sent, onDeadlineReminder.Status);
        Assert.Equal(EReminderStatus.Cancelled, repeatReminder.Status);
        Assert.Null(repeatReminder.NextTriggerAt);
        Assert.Equal(ETaskStatus.Late, task.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenRepeatAndAfterDeadlineAreDue_SendsAfterDeadlineAndCancelsRepeat()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = "After deadline priority",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = now.AddHours(-2),
            Status = ETaskStatus.Doing
        };

        var afterDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.AfterDeadline,
                IntervalUnit = ETimeUnit.Hours,
                Value = 1,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddMinutes(-1),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 10,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.AddRange(afterDeadlineReminder, repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(2, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal(afterDeadlineReminder.Id, sender.Sent[0].ReminderId);
        Assert.Equal(EReminderStatus.Pending, afterDeadlineReminder.Status);
        Assert.NotNull(afterDeadlineReminder.NextTriggerAt);
        Assert.True(afterDeadlineReminder.NextTriggerAt > now);
        Assert.Equal(EReminderStatus.Cancelled, repeatReminder.Status);
        Assert.Null(repeatReminder.NextTriggerAt);
        Assert.Equal(ETaskStatus.Late, task.Status);
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
    public async Task ProcessDueRemindersAsync_WhenOnDeadlineSendFails_PersistsTaskLateStatus()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender
        {
            FailedTargetUserId = "user-fail"
        };
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "On deadline failed send",
            AssignedTo = "user-fail",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddMinutes(-5),
            Status = ETaskStatus.Doing
        };
        var onDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddMinutes(-5),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                TaskStatus = task.Status
            }
        };

        context.Reminders.Add(onDeadlineReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(0, processedCount);
        Assert.Empty(sender.Sent);

        var taskId = task.Id;
        context.ChangeTracker.Clear();

        var persistedTask = await context.TaskItems.SingleAsync(t => t.Id == taskId);
        Assert.Equal(ETaskStatus.Late, persistedTask.Status);

        var persistedReminder = await context.Reminders.SingleAsync(r => r.TaskId == taskId);
        Assert.Equal(EReminderStatus.Pending, persistedReminder.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenAfterDeadlineReminderIsDue_TransitionsTaskToLate()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "After deadline task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddHours(-2),
            Status = ETaskStatus.Doing
        };
        var afterDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddHours(-1),
            NextTriggerAt = DateTime.UtcNow.AddHours(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.AfterDeadline,
                IntervalUnit = ETimeUnit.Hours,
                Value = 1,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.Add(afterDeadlineReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal(afterDeadlineReminder.Id, sender.Sent[0].ReminderId);
        Assert.Equal(ETaskStatus.Late, task.Status);
        Assert.Equal(EReminderStatus.Pending, afterDeadlineReminder.Status);
        Assert.NotNull(afterDeadlineReminder.NextTriggerAt);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenTodoTaskReachedDeadline_TransitionsDirectlyToLate()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "Todo overdue task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddMinutes(-10),
            Status = ETaskStatus.ToDo
        };
        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddMinutes(-20),
            NextTriggerAt = DateTime.UtcNow.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 5,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.Add(repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Empty(sender.Sent);
        Assert.Equal(ETaskStatus.Late, task.Status);
        Assert.Equal(EReminderStatus.Cancelled, repeatReminder.Status);
        Assert.Null(repeatReminder.NextTriggerAt);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenTaskAlreadyLate_CancelsRepeatingReminderWithoutSending()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "Late task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddHours(-3),
            Status = ETaskStatus.Late
        };
        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddHours(-2),
            NextTriggerAt = DateTime.UtcNow.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 15,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.Add(repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Empty(sender.Sent);
        Assert.Equal(ETaskStatus.Late, task.Status);
        Assert.Equal(EReminderStatus.Cancelled, repeatReminder.Status);
        Assert.Null(repeatReminder.NextTriggerAt);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenTaskAlreadyLate_WithAfterDeadlineAndRepeat_SendsAfterDeadlineAndCancelsRepeat()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = "Late task with mixed reminders",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = now.AddHours(-3),
            Status = ETaskStatus.Late
        };

        var afterDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddHours(-2),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.AfterDeadline,
                IntervalUnit = ETimeUnit.Hours,
                Value = 1,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        var repeatReminder = new Reminder
        {
            Task = task,
            TriggerAt = now.AddHours(-2),
            NextTriggerAt = now.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.Repeat,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 15,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.AddRange(afterDeadlineReminder, repeatReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(2, processedCount);
        Assert.Single(sender.Sent);
        Assert.Equal(afterDeadlineReminder.Id, sender.Sent[0].ReminderId);
        Assert.Equal(EReminderStatus.Pending, afterDeadlineReminder.Status);
        Assert.NotNull(afterDeadlineReminder.NextTriggerAt);
        Assert.True(afterDeadlineReminder.NextTriggerAt > now);
        Assert.Equal(EReminderStatus.Cancelled, repeatReminder.Status);
        Assert.Null(repeatReminder.NextTriggerAt);
        Assert.Equal(ETaskStatus.Late, task.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenTodoTaskHasOnlyBeforeDeadlineDue_DoesNotTransitionToLate()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "Todo before deadline task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddMinutes(20),
            Status = ETaskStatus.ToDo
        };
        var beforeDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.BeforeDeadline,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 30,
                TaskStatus = task.Status
            }
        };

        context.Reminders.Add(beforeDeadlineReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(ETaskStatus.ToDo, task.Status);
        Assert.Equal(EReminderStatus.Sent, beforeDeadlineReminder.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_TransitionsTaskToLateAndSendsAfterDeadlineReminder()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "Reminder task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddHours(-2),
            Status = ETaskStatus.Doing
        };

        var beforeReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddHours(-2.5),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.BeforeDeadline,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 30,
                TaskStatus = task.Status
            }
        };
        var onDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddHours(-2),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                TaskStatus = task.Status
            }
        };
        var repeatingAfterReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddHours(-1),
            NextTriggerAt = DateTime.UtcNow.AddHours(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.AfterDeadline,
                IntervalUnit = ETimeUnit.Hours,
                Value = 1,
                TaskStatus = task.Status,
                IsRepeat = true
            }
        };

        context.Reminders.AddRange(beforeReminder, onDeadlineReminder, repeatingAfterReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(3, processedCount);
        Assert.Equal(3, sender.Sent.Count);
        Assert.Equal(EReminderStatus.Sent, beforeReminder.Status);
        Assert.Equal(EReminderStatus.Sent, onDeadlineReminder.Status);
        Assert.Equal(EReminderStatus.Pending, repeatingAfterReminder.Status);
        Assert.NotNull(repeatingAfterReminder.NextTriggerAt);
        Assert.Equal(ETaskStatus.Late, task.Status);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_OnDeadlineReminderForReviewTask_TransitionsToLateAndClearsReviewStartedAt()
    {
        await using var context = CreateContext();
        var sender = new FakeReminderNotificationSender();
        var processor = new ReminderProcessor(context, sender, NullLogger<ReminderProcessor>.Instance);
        var task = new TaskItem
        {
            Title = "Review task",
            AssignedTo = "user-1",
            CreatedBy = "user-2",
            DueDate = DateTime.UtcNow.AddMinutes(-1),
            Status = ETaskStatus.Review,
            ReviewStartedAt = DateTime.UtcNow.AddHours(-1)
        };
        var onDeadlineReminder = new Reminder
        {
            Task = task,
            TriggerAt = DateTime.UtcNow.AddMinutes(-1),
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            StateSnapshot = ETaskStatus.Review,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                TaskStatus = ETaskStatus.Review
            }
        };

        context.Reminders.Add(onDeadlineReminder);
        await context.SaveChangesAsync();

        var processedCount = await processor.ProcessDueRemindersAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(ETaskStatus.Late, task.Status);
        Assert.Null(task.ReviewStartedAt);
        Assert.Equal(EReminderStatus.Sent, onDeadlineReminder.Status);
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
