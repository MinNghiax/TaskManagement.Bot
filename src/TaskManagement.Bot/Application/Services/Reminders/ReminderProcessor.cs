using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services.Reminders;

public class ReminderProcessor : IReminderProcessor
{
    private readonly TaskManagementDbContext _context;
    private readonly IReminderNotificationSender _notificationSender;
    private readonly ILogger<ReminderProcessor> _logger;

    public ReminderProcessor(
        TaskManagementDbContext context,
        IReminderNotificationSender notificationSender,
        ILogger<ReminderProcessor> logger)
    {
        _context = context;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminders = await _context.Reminders
            .Include(r => r.ReminderRule)
            .Include(r => r.Task!.Clans)
            .Include(r => r.Task!.Team!.Project)
            .Where(r =>
                !r.IsDeleted &&
                r.Status == EReminderStatus.Pending &&
                r.Task != null &&
                !r.Task.IsDeleted &&
                (r.NextTriggerAt ?? r.TriggerAt) <= now)
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ToListAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            await ProcessReminderAsync(reminder, now, cancellationToken);
        }

        if (reminders.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return reminders.Count;
    }

    private async Task ProcessReminderAsync(Reminder reminder, DateTime now, CancellationToken cancellationToken)
    {
        var task = reminder.Task;
        if (task == null)
        {
            reminder.Status = EReminderStatus.Cancelled;
            reminder.UpdatedAt = now;
            return;
        }

        if (task.Status is ETaskStatus.Completed or ETaskStatus.Cancelled)
        {
            reminder.Status = EReminderStatus.Cancelled;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
            return;
        }

        if (string.IsNullOrWhiteSpace(reminder.TargetUserId))
        {
            _logger.LogWarning(
                "Reminder {ReminderId} for task {TaskId} has no target user",
                reminder.Id,
                reminder.TaskId);

            reminder.Status = EReminderStatus.Cancelled;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
            return;
        }

        await _notificationSender.SendAsync(reminder, cancellationToken);

        ApplyNextSchedule(reminder, now);
    }

    private static void ApplyNextSchedule(Reminder reminder, DateTime now)
    {
        if (reminder.ReminderRule?.IsRepeat != true)
        {
            reminder.Status = EReminderStatus.Sent;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
            return;
        }

        var interval = ToTimeSpan(reminder.ReminderRule.Value, reminder.ReminderRule.IntervalUnit);
        if (interval <= TimeSpan.Zero)
        {
            reminder.Status = EReminderStatus.Sent;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
            return;
        }

        var nextTriggerAt = (reminder.NextTriggerAt ?? reminder.TriggerAt).Add(interval);
        while (nextTriggerAt <= now)
        {
            nextTriggerAt = nextTriggerAt.Add(interval);
        }

        reminder.NextTriggerAt = nextTriggerAt;
        reminder.UpdatedAt = now;
    }

    private static TimeSpan ToTimeSpan(double value, ETimeUnit? unit) =>
        unit switch
        {
            ETimeUnit.Minutes => TimeSpan.FromMinutes(value),
            ETimeUnit.Hours => TimeSpan.FromHours(value),
            ETimeUnit.Days => TimeSpan.FromDays(value),
            ETimeUnit.Weeks => TimeSpan.FromDays(value * 7),
            _ => TimeSpan.Zero
        };
}
