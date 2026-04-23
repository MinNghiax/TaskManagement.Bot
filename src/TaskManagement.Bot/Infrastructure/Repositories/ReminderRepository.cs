using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Domain.Interfaces;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly TaskManagementDbContext _ctx;

    public ReminderRepository(TaskManagementDbContext ctx) => _ctx = ctx;

    public Task<Reminder?> GetByIdAsync(int id, CancellationToken ct = default) =>
        QueryWithGraph()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

    public Task<List<Reminder>> GetByTaskIdAsync(int taskId, CancellationToken ct = default) =>
        QueryWithGraph()
            .Where(r => r.TaskId == taskId && !r.IsDeleted)
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ToListAsync(ct);

    public Task<List<Reminder>> GetPendingAsync(CancellationToken ct = default) =>
        QueryWithGraph()
            .Where(r => !r.IsDeleted && r.Status == EReminderStatus.Pending)
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ToListAsync(ct);

    public Task<List<Reminder>> GetByUserAsync(string targetUserId, CancellationToken ct = default) =>
        QueryWithGraph()
            .Where(r => !r.IsDeleted && r.TargetUserId == targetUserId)
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ToListAsync(ct);

    public Task<List<Reminder>> GetDueAsync(DateTime beforeTimeUtc, CancellationToken ct = default) =>
        QueryWithGraph()
            .Where(r =>
                !r.IsDeleted &&
                r.Status == EReminderStatus.Pending &&
                r.Task != null &&
                !r.Task.IsDeleted &&
                (r.NextTriggerAt ?? r.TriggerAt) <= beforeTimeUtc)
            .OrderBy(r => r.NextTriggerAt ?? r.TriggerAt)
            .ToListAsync(ct);

    public Task<List<TaskItem>> GetReviewTasksDueForAutoCompleteAsync(
        DateTime reviewStartedBeforeOrAtUtc,
        int batchSize,
        CancellationToken ct = default)
    {
        var normalizedBatchSize = Math.Clamp(batchSize, 1, 500);

        return _ctx.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .Where(t =>
                !t.IsDeleted &&
                t.Status == ETaskStatus.Review &&
                t.ReviewStartedAt.HasValue &&
                t.ReviewStartedAt.Value <= reviewStartedBeforeOrAtUtc)
            .OrderBy(t => t.ReviewStartedAt)
            .Take(normalizedBatchSize)
            .ToListAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    private IQueryable<Reminder> QueryWithGraph() =>
        _ctx.Reminders
            .Include(r => r.ReminderRule)
            .Include(r => r.Task!.Clans)
            .Include(r => r.Task!.Reminders).ThenInclude(r => r.ReminderRule)
            .Include(r => r.Task!.Team!.Project);
}
