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

        Assert.Equal(2, task.Reminders.Count);
        Assert.Equal(2, await context.ReminderRules.CountAsync());

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

        var reminder = await context.Reminders.SingleAsync();

        Assert.Equal(assignedTo, reminder.TargetUserId);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAndDisablesTaskReminders()
    {
        await using var context = CreateContext();
        var service = new TaskService(context);
        var created = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Reminder task",
            AssignedTo = "123",
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

        var reminder = Assert.Single(updatedTask.Reminders);
        Assert.Equal(EReminderTriggerType.Repeat, reminder.ReminderRule!.TriggerType);
        Assert.Equal(1, await context.ReminderRules.CountAsync());

        await service.UpdateAsync(created.Id, new UpdateTaskDto
        {
            ReminderRules = []
        });

        context.ChangeTracker.Clear();

        Assert.Empty(await context.Reminders.ToListAsync());
        Assert.Empty(await context.ReminderRules.ToListAsync());
    }

    private static TaskManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskManagementDbContext(options);
    }
}
