using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;
namespace TaskManagement.Bot.Tests;
public class TaskServiceReminderTests
{
    [Fact]
    public async Task CreateAsync_WithoutCustomReminderRules_CreatesOnDeadlineReminder()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var dueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc);

        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = "123",
            CreatedBy = "456",
            DueDate = dueDate,
            ReminderRules = []
        });

        Assert.NotNull(created);
        Assert.Empty(created.ReminderRules);

        var reminder = await context.Reminders
            .Include(r => r.ReminderRule)
            .SingleAsync();

        Assert.Equal(dueDate, reminder.TriggerAt);
        Assert.Equal("123", reminder.TargetUserId);
        Assert.Null(reminder.NextTriggerAt);
        Assert.Equal(EReminderStatus.Pending, reminder.Status);
        Assert.Equal(EReminderTriggerType.OnDeadline, reminder.ReminderRule!.TriggerType);
        Assert.Null(reminder.ReminderRule.IntervalUnit);
        Assert.Equal(0, reminder.ReminderRule.Value);
        Assert.False(reminder.ReminderRule.IsRepeat);
    }

    [Fact]
    public async Task CreateAsync_WithReminderRules_CreatesRemindersWithRules()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var dueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc);
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = "123",
            CreatedBy = "456",
            DueDate = dueDate,
            ReminderRules =
            [
                new()
                {
                    TriggerType = EReminderTriggerType.BeforeDeadline,
                    Value = 30,
                    IntervalUnit = ETimeUnit.Minutes
                },
                new()
                {
                    TriggerType = EReminderTriggerType.AfterDeadline,
                    Value = 1,
                    IntervalUnit = ETimeUnit.Hours,
                    IsRepeat = true
                },
                new()
                {
                    TriggerType = EReminderTriggerType.Repeat,
                    Value = 0,
                    IntervalUnit = ETimeUnit.Hours,
                    IsRepeat = true
                }
            ]
        });
        Assert.NotNull(created);
        Assert.Equal(2, created.ReminderRules.Count);

        var task = await context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .SingleAsync(t => t.Id == created.Id);
        Assert.Equal(3, task.Reminders.Count);
        Assert.Equal(3, await context.ReminderRules.CountAsync());

        var onDeadlineReminder = task.Reminders.Single(r =>
            r.ReminderRule!.TriggerType == EReminderTriggerType.OnDeadline);
        Assert.Equal(dueDate, onDeadlineReminder.TriggerAt);
        Assert.Null(onDeadlineReminder.NextTriggerAt);
        Assert.False(onDeadlineReminder.ReminderRule!.IsRepeat);

        var beforeReminder = task.Reminders.Single(r =>
            r.ReminderRule!.TriggerType == EReminderTriggerType.BeforeDeadline);
        Assert.Equal(dueDate.AddMinutes(-30), beforeReminder.TriggerAt);
        Assert.Equal("123", beforeReminder.TargetUserId);
        Assert.False(beforeReminder.ReminderRule!.IsRepeat);
        var afterReminder = task.Reminders.Single(r =>
            r.ReminderRule!.TriggerType == EReminderTriggerType.AfterDeadline);
        Assert.Equal(dueDate.AddHours(1), afterReminder.TriggerAt);
        Assert.Equal(afterReminder.TriggerAt, afterReminder.NextTriggerAt);
        Assert.True(afterReminder.ReminderRule!.IsRepeat);
    }
    [Fact]
    public async Task CreateAsync_WithNonNumericAssignedUser_PreservesReminderTargetUserId()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        const string assignedTo = "user-12345678901234567890";
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = assignedTo,
            CreatedBy = "456",
            DueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc),
            ReminderRules =
            [
                new()
                {
                    TriggerType = EReminderTriggerType.BeforeDeadline,
                    Value = 30,
                    IntervalUnit = ETimeUnit.Minutes
                }
            ]
        });
        Assert.NotNull(created);

        var reminders = await context.Reminders.ToListAsync();

        Assert.Equal(2, reminders.Count);
        Assert.All(reminders, reminder => Assert.Equal(assignedTo, reminder.TargetUserId));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAndDisablesCustomTaskRemindersWithoutRemovingOnDeadline()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var dueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc);
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = "123",
            CreatedBy = "456",
            DueDate = dueDate,
            ReminderRules =
            [
                new()
                {
                    TriggerType = EReminderTriggerType.BeforeDeadline,
                    Value = 30,
                    IntervalUnit = ETimeUnit.Minutes
                }
            ]
        });
        Assert.NotNull(created);
        await service.UpdateAsync(created.Id, new UpdateTaskDto
        {
            ReminderRules =
            [
                new()
                {
                    TriggerType = EReminderTriggerType.Repeat,
                    Value = 2,
                    IntervalUnit = ETimeUnit.Hours,
                    IsRepeat = true
                }
            ]
        });
        context.ChangeTracker.Clear();
        var updatedTask = await context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .SingleAsync(t => t.Id == created.Id);

        Assert.Equal(2, updatedTask.Reminders.Count);
        Assert.Contains(updatedTask.Reminders, r =>
            r.ReminderRule!.TriggerType == EReminderTriggerType.OnDeadline &&
            r.TriggerAt == dueDate);
        Assert.Contains(updatedTask.Reminders, r =>
            r.ReminderRule!.TriggerType == EReminderTriggerType.Repeat);
        Assert.Equal(2, await context.ReminderRules.CountAsync());

        await service.UpdateAsync(created.Id, new UpdateTaskDto
        {
            ReminderRules = []
        });
        context.ChangeTracker.Clear();

        var remainingReminder = await context.Reminders
            .Include(r => r.ReminderRule)
            .SingleAsync();
        Assert.Equal(EReminderTriggerType.OnDeadline, remainingReminder.ReminderRule!.TriggerType);
        Assert.Equal(dueDate, remainingReminder.TriggerAt);
        Assert.Single(await context.ReminderRules.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_ChangingDueDateReschedulesOnDeadlineReminder()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var originalDueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc);
        var newDueDate = new DateTime(2026, 4, 22, 9, 30, 0, DateTimeKind.Utc);
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = "123",
            CreatedBy = "456",
            DueDate = originalDueDate,
            ReminderRules = []
        });

        Assert.NotNull(created);

        var onDeadlineReminder = await context.Reminders
            .Include(r => r.ReminderRule)
            .SingleAsync(r => r.ReminderRule!.TriggerType == EReminderTriggerType.OnDeadline);
        onDeadlineReminder.Status = EReminderStatus.Sent;
        await context.SaveChangesAsync();

        await service.UpdateAsync(created.Id, new UpdateTaskDto
        {
            DueDate = newDueDate
        });

        context.ChangeTracker.Clear();

        onDeadlineReminder = await context.Reminders
            .Include(r => r.ReminderRule)
            .SingleAsync(r => r.ReminderRule!.TriggerType == EReminderTriggerType.OnDeadline);

        Assert.Equal(newDueDate, onDeadlineReminder.TriggerAt);
        Assert.Equal(EReminderStatus.Pending, onDeadlineReminder.Status);
        Assert.Null(onDeadlineReminder.NextTriggerAt);
    }

    [Fact]
    public async Task ChangeStatusAsync_TracksReviewStartedAtForReviewAutoComplete()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Review lifecycle task",
            AssignedTo = "123",
            CreatedBy = "456",
            DueDate = new DateTime(2026, 4, 21, 12, 0, 0, DateTimeKind.Utc),
            ReminderRules = []
        });
        Assert.NotNull(created);

        await service.ChangeStatusAsync(created.Id, ETaskStatus.Review);

        var task = await context.TaskItems.SingleAsync(t => t.Id == created.Id);
        Assert.Equal(ETaskStatus.Review, task.Status);
        Assert.NotNull(task.ReviewStartedAt);

        await service.ChangeStatusAsync(created.Id, ETaskStatus.Doing);

        Assert.Equal(ETaskStatus.Doing, task.Status);
        Assert.Null(task.ReviewStartedAt);
    }

    private static TaskManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TaskManagementDbContext(options);
    }
}
