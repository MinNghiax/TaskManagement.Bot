using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services;

public class PendingTeamRequestService : IPendingTeamRequestService
{
    private readonly TaskManagementDbContext _context;

    public PendingTeamRequestService(TaskManagementDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PendingTeamRequest request, CancellationToken cancellationToken = default)
    {
        _context.PendingTeamRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PendingTeamRequest?> GetAsync(string requestId, CancellationToken cancellationToken = default)
    {
        return _context.PendingTeamRequests
            .FirstOrDefaultAsync(x => x.MessageId == requestId, cancellationToken);
    }

    public async Task UpdateAsync(PendingTeamRequest request, CancellationToken cancellationToken = default)
    {
        _context.PendingTeamRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var request = await GetAsync(requestId, cancellationToken);
        if (request == null)
        {
            return;
        }

        _context.PendingTeamRequests.Remove(request);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
