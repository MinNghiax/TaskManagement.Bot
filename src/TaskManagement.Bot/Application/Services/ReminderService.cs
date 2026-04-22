using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Domain.Interfaces;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public class ReminderService : IReminderProcessor
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IReminderNotificationSender _notificationSender;
    private readonly ILogger _logger;
    private readonly string? _reviewAutoCompleteAfter;

    public ReminderService(
        IReminderRepository reminderRepository,
        IReminderNotificationSender notificationSender,
        ILogger<ReminderService> logger)
        : this(reminderRepository, notificationSender, (ILogger)logger, reviewAutoCompleteAfter: null)
    {
    }

    public ReminderService(
        IReminderRepository reminderRepository,
        IReminderNotificationSender notificationSender,
        ILogger<ReminderService> logger,
        IConfiguration configuration)
        : this(
            reminderRepository,
            notificationSender,
            (ILogger)logger,
            configuration["JobSettings:Review:AutoCompleteAfter"])
    {
    }

    protected ReminderService(
        IReminderRepository reminderRepository,
        IReminderNotificationSender notificationSender,
        ILogger logger,
        string? reviewAutoCompleteAfter = null)
    {
        _reminderRepository = reminderRepository;
        _notificationSender = notificationSender;
        _logger = logger;
        _reviewAutoCompleteAfter = reviewAutoCompleteAfter;
    }

    public Task<Reminder?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _reminderRepository.GetByIdAsync(id, ct);

    public Task<List<Reminder>> GetByTaskIdAsync(int taskId, CancellationToken ct = default) =>
        _reminderRepository.GetByTaskIdAsync(taskId, ct);

    public Task<List<Reminder>> GetPendingAsync(CancellationToken ct = default) =>
        _reminderRepository.GetPendingAsync(ct);

    public Task<List<Reminder>> GetByUserAsync(string targetUserId, CancellationToken ct = default) =>
        _reminderRepository.GetByUserAsync(targetUserId, ct);

    public Task<List<Reminder>> GetDueAsync(DateTime beforeTimeUtc, CancellationToken ct = default) =>
        _reminderRepository.GetDueAsync(beforeTimeUtc, ct);

    public async Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var completedReviewCount = await ProcessReviewAutoCompleteAsync(now, cancellationToken);
        if (completedReviewCount > 0)
        {
            await _reminderRepository.SaveAsync(cancellationToken);
        }

        var reminders = await _reminderRepository.GetDueAsync(now, cancellationToken);
        var processedReminderCount = 0;

        foreach (var reminder in reminders)
        {
            try
            {
                await ProcessReminderAsync(reminder, now, cancellationToken);
                processedReminderCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process reminder {ReminderId} for task {TaskId} and user {TargetUserId}. It will be retried later.",
                    reminder.Id,
                    reminder.TaskId,
                    reminder.TargetUserId);
            }
        }

        if (processedReminderCount > 0)
        {
            await _reminderRepository.SaveAsync(cancellationToken);
        }

        return processedReminderCount + completedReviewCount;
    }

    private async Task<int> ProcessReviewAutoCompleteAsync(DateTime now, CancellationToken cancellationToken)
    {
        var delay = ReviewAutoCompletePolicy.GetDelay(_reviewAutoCompleteAfter);
        if (!delay.HasValue)
            return 0;

        var thresholdUtc = now.Subtract(delay.Value);
        var tasks = await _reminderRepository.GetReviewTasksDueForAutoCompleteAsync(
            thresholdUtc,
            batchSize: 100,
            ct: cancellationToken);

        var completedCount = 0;
        foreach (var task in tasks)
        {
            if (!ReviewAutoCompletePolicy.IsDue(task, _reviewAutoCompleteAfter, now))
                continue;

            var reviewStartedAt = task.ReviewStartedAt;
            AutoCompleteReviewTask(task, now);
            completedCount++;

            _logger.LogInformation(
                "Task {TaskId} auto-completed after staying in review since {ReviewStartedAtUtc}",
                task.Id,
                reviewStartedAt);
        }

        return completedCount;
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

        ReminderScheduleBuilder.ApplyNextSchedule(reminder, now);
    }

    private static void AutoCompleteReviewTask(TaskItem task, DateTime now)
    {
        task.Status = ETaskStatus.Completed;
        task.ReviewStartedAt = null;
        task.UpdatedAt = now;

        foreach (var reminder in task.Reminders.Where(r => r.Status == EReminderStatus.Pending))
        {
            reminder.Status = EReminderStatus.Cancelled;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
        }
    }

}
