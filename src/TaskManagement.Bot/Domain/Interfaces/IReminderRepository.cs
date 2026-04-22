namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;

public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Reminder>> GetByTaskIdAsync(int taskId, CancellationToken ct = default);
    Task<List<Reminder>> GetPendingAsync(CancellationToken ct = default);
    Task<List<Reminder>> GetByUserAsync(string targetUserId, CancellationToken ct = default);
    Task<List<Reminder>> GetDueAsync(DateTime beforeTimeUtc, CancellationToken ct = default);
    Task<List<TaskItem>> GetReviewTasksDueForAutoCompleteAsync(
        DateTime reviewStartedBeforeOrAtUtc,
        int batchSize,
        CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
