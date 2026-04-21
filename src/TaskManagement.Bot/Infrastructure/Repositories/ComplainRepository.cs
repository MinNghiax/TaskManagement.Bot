using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Domain.Interfaces;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Repositories;

public class ComplainRepository : IComplainRepository
{
    private readonly TaskManagementDbContext _ctx;

    public ComplainRepository(TaskManagementDbContext ctx) => _ctx = ctx;

    public Task<Complain?> GetByIdAsync(int id, CancellationToken ct = default)
        => _ctx.Complains.Include(c => c.TaskItem).FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public Task<Complain?> GetPendingByTaskAsync(int taskItemId, CancellationToken ct = default)
        => _ctx.Complains.FirstOrDefaultAsync(
            c => c.TaskItemId == taskItemId && c.Status == EComplainStatus.Pending && !c.IsDeleted, ct);

    public Task<List<Complain>> GetByTaskAsync(int taskItemId, CancellationToken ct = default)
        => _ctx.Complains.Where(c => c.TaskItemId == taskItemId && !c.IsDeleted).ToListAsync(ct);

    public async Task<Complain> CreateAsync(Complain complain, CancellationToken ct = default)
    {
        _ctx.Complains.Add(complain);
        await _ctx.SaveChangesAsync(ct);
        return complain;
    }

    public Task SaveAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    public async Task<List<Complain>> GetPendingByPMAsync(string pmUserId, CancellationToken ct = default)
    {
        return await _ctx.Complains
            .Include(c => c.TaskItem)
            .Where(c => c.Status == EComplainStatus.Pending
                        && !c.IsDeleted
                        && c.TaskItem != null
                        && c.TaskItem.CreatedBy == pmUserId)
            .ToListAsync(ct);
    }
}