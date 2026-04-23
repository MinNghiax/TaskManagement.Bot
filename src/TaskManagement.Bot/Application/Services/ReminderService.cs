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
        var dueTriggerFlagsByTask = BuildDueTriggerFlagsByTask(reminders);
        var processedReminderCount = 0;

        var orderedReminders = reminders
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ThenBy(r => GetReminderProcessingPriority(r.ReminderRule?.TriggerType))
            .ToList();

        foreach (var reminder in orderedReminders)
        {
            try
            {
                await ProcessReminderAsync(reminder, now, dueTriggerFlagsByTask, cancellationToken);
                await _reminderRepository.SaveAsync(cancellationToken);
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

                try
                {
                    await _reminderRepository.SaveAsync(cancellationToken);
                }
                catch (Exception persistEx)
                {
                    _logger.LogError(
                        persistEx,
                        "Failed to persist state changes after reminder {ReminderId} processing error.",
                        reminder.Id);
                }
            }
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

    private async Task ProcessReminderAsync(
        Reminder reminder,
        DateTime now,
        IReadOnlyDictionary<int, DueTriggerFlags> dueTriggerFlagsByTask,
        CancellationToken cancellationToken)
    {
        var task = reminder.Task;
        if (task == null)
        {
            reminder.Status = EReminderStatus.Cancelled;
            reminder.UpdatedAt = now;
            return;
        }

        if (task.Status == ETaskStatus.Cancelled)
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

        if (reminder.Status != EReminderStatus.Pending)
            return;

        if (HandlePausedTaskStatuses(reminder, task, now))
            return;

        if (task.DueDate.HasValue && reminder.ReminderRule != null)
        {
            RecalculateScheduleIfNeeded(reminder, task, now);
        }

        TransitionTaskToLateIfNeeded(task, reminder, now);

        if (reminder.Status != EReminderStatus.Pending)
            return;

        if (HandleRepeatConflict(reminder, task, now, dueTriggerFlagsByTask))
            return;

        await _notificationSender.SendAsync(reminder, cancellationToken);

        ReminderScheduleBuilder.ApplyNextSchedule(reminder, now);
    }

    private static bool HandlePausedTaskStatuses(Reminder reminder, TaskItem task, DateTime now)
    {
        if (task.Status is ETaskStatus.Review or ETaskStatus.Completed)
        {
            SyncReminderTaskState(reminder, task, now);

            if (ReminderScheduleBuilder.IsRepeatRule(reminder.ReminderRule))
                ReminderScheduleBuilder.ApplyNextSchedule(reminder, now);

            return true;
        }

        if (task.Status == ETaskStatus.Doing
            && reminder.StateSnapshot is ETaskStatus.Review or ETaskStatus.Completed
            && ReminderScheduleBuilder.IsRepeatRule(reminder.ReminderRule))
        {
            SyncReminderTaskState(reminder, task, now);
            ReminderScheduleBuilder.ApplyNextSchedule(reminder, now);
            return true;
        }

        return false;
    }

    private static void SyncReminderTaskState(Reminder reminder, TaskItem task, DateTime now)
    {
        var hasChanges = false;

        if (reminder.StateSnapshot != task.Status)
        {
            reminder.StateSnapshot = task.Status;
            hasChanges = true;
        }

        if (reminder.ReminderRule is not null && reminder.ReminderRule.TaskStatus != task.Status)
        {
            reminder.ReminderRule.TaskStatus = task.Status;
            reminder.ReminderRule.UpdatedAt = now;
            hasChanges = true;
        }

        if (hasChanges)
            reminder.UpdatedAt = now;
    }

    private static bool HandleRepeatConflict(
        Reminder reminder,
        TaskItem task,
        DateTime now,
        IReadOnlyDictionary<int, DueTriggerFlags> dueTriggerFlagsByTask)
    {
        if (reminder.ReminderRule?.TriggerType != EReminderTriggerType.Repeat)
            return false;

        if (!dueTriggerFlagsByTask.TryGetValue(reminder.TaskId, out var flags) || !flags.HasRepeat)
            return false;

        if (flags.HasOnDeadline || flags.HasAfterDeadline)
        {
            reminder.Status = EReminderStatus.Cancelled;
            reminder.NextTriggerAt = null;
            reminder.UpdatedAt = now;
            reminder.StateSnapshot = task.Status;

            if (reminder.ReminderRule is not null)
            {
                reminder.ReminderRule.TaskStatus = task.Status;
                reminder.ReminderRule.UpdatedAt = now;
            }

            return true;
        }

        if (!flags.HasBeforeDeadline)
            return false;

        ReminderScheduleBuilder.ApplyNextSchedule(reminder, now);
        return true;
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

    private static void TransitionTaskToLateIfNeeded(TaskItem task, Reminder reminder, DateTime changedAtUtc)
    {
        if (task.Status is ETaskStatus.Completed or ETaskStatus.Cancelled)
            return;

        if (task.Status == ETaskStatus.Late)
        {
            CancelPendingRepeatReminders(task, changedAtUtc);
            return;
        }

        var isOverdue =
            task.DueDate.HasValue &&
            NormalizeUtc(changedAtUtc) >= NormalizeUtc(task.DueDate.Value);

        var shouldTransition =
            task.Status == ETaskStatus.ToDo &&
            isOverdue;

        if (!shouldTransition)
            return;

        var leavingReview = task.Status == ETaskStatus.Review;

        task.Status = ETaskStatus.Late;
        task.UpdatedAt = changedAtUtc;

        if (leavingReview)
            task.ReviewStartedAt = null;

        reminder.StateSnapshot = task.Status;

        if (reminder.ReminderRule is not null)
        {
            reminder.ReminderRule.TaskStatus = task.Status;
            reminder.ReminderRule.UpdatedAt = changedAtUtc;
        }

        var excludedReminder = reminder.ReminderRule?.TriggerType == EReminderTriggerType.AfterDeadline
            ? reminder
            : null;

        CancelPendingRepeatReminders(task, changedAtUtc, excludedReminder);
    }

    private static bool HasReminderReachedDeadline(Reminder reminder, DateTime deadline)
    {
        var reminderDueAt = reminder.NextTriggerAt ?? reminder.TriggerAt;
        if (reminderDueAt == default)
            return false;

        return NormalizeUtc(reminderDueAt) >= NormalizeUtc(deadline);
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static void CancelPendingRepeatReminders(
        TaskItem task,
        DateTime changedAtUtc,
        Reminder? excludedReminder = null)
    {
        foreach (var pendingRepeatReminder in task.Reminders.Where(r =>
                     r.Status == EReminderStatus.Pending
                     && !ReferenceEquals(r, excludedReminder)
                     && r.ReminderRule?.TriggerType == EReminderTriggerType.Repeat))
        {
            pendingRepeatReminder.Status = EReminderStatus.Cancelled;
            pendingRepeatReminder.NextTriggerAt = null;
            pendingRepeatReminder.UpdatedAt = changedAtUtc;
            pendingRepeatReminder.StateSnapshot = task.Status;

            if (pendingRepeatReminder.ReminderRule is not null)
            {
                pendingRepeatReminder.ReminderRule.TaskStatus = task.Status;
                pendingRepeatReminder.ReminderRule.UpdatedAt = changedAtUtc;
            }
        }
    }

    private static Dictionary<int, DueTriggerFlags> BuildDueTriggerFlagsByTask(IEnumerable<Reminder> reminders)
    {
        var flagsByTask = new Dictionary<int, DueTriggerFlags>();

        foreach (var reminder in reminders)
        {
            if (reminder.Status != EReminderStatus.Pending)
                continue;

            var taskId = reminder.TaskId;
            if (taskId <= 0)
                continue;

            flagsByTask.TryGetValue(taskId, out var flags);

            switch (reminder.ReminderRule?.TriggerType)
            {
                case EReminderTriggerType.BeforeDeadline:
                    flags.HasBeforeDeadline = true;
                    break;
                case EReminderTriggerType.OnDeadline:
                    flags.HasOnDeadline = true;
                    break;
                case EReminderTriggerType.AfterDeadline:
                    flags.HasAfterDeadline = true;
                    break;
                case EReminderTriggerType.Repeat:
                    flags.HasRepeat = true;
                    break;
            }

            flagsByTask[taskId] = flags;
        }

        return flagsByTask;
    }

    private static void RecalculateScheduleIfNeeded(Reminder reminder, TaskItem task, DateTime now)
    {
        if (!task.DueDate.HasValue || reminder.ReminderRule == null)
            return;

        var dueDate = task.DueDate.Value;

        DateTime? newTrigger = reminder.ReminderRule.TriggerType switch
        {
            EReminderTriggerType.BeforeDeadline =>
                reminder.ReminderRule.IntervalUnit.HasValue
                    ? dueDate.Subtract(ReminderScheduleBuilder.ToTimeSpan(reminder.ReminderRule.Value, reminder.ReminderRule.IntervalUnit.Value))
                    : dueDate,

            EReminderTriggerType.OnDeadline =>
                dueDate,

            EReminderTriggerType.AfterDeadline =>
                reminder.ReminderRule.IntervalUnit.HasValue
                    ? dueDate.Add(ReminderScheduleBuilder.ToTimeSpan(reminder.ReminderRule.Value, reminder.ReminderRule.IntervalUnit.Value))
                    : dueDate,

            _ => reminder.NextTriggerAt
        };

        // chỉ update khi khác (tránh loop vô hạn)
        if (newTrigger.HasValue && NormalizeUtc(newTrigger.Value) != NormalizeUtc(reminder.NextTriggerAt ?? default))
        {
            reminder.NextTriggerAt = newTrigger;
            reminder.UpdatedAt = now;
        }
    }

    private static int GetReminderProcessingPriority(EReminderTriggerType? triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => 0,
            EReminderTriggerType.BeforeDeadline => 1,
            EReminderTriggerType.AfterDeadline => 2,
            EReminderTriggerType.Repeat => 3,
            _ => 4
        };

    private struct DueTriggerFlags
    {
        public bool HasBeforeDeadline { get; set; }
        public bool HasOnDeadline { get; set; }
        public bool HasAfterDeadline { get; set; }
        public bool HasRepeat { get; set; }
    }

}
