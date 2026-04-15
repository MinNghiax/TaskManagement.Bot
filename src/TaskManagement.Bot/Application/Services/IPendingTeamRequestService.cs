using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services;

public interface IPendingTeamRequestService
{
    Task AddAsync(PendingTeamRequest request, CancellationToken cancellationToken = default);
    Task<PendingTeamRequest?> GetAsync(string requestId, CancellationToken cancellationToken = default);
    Task UpdateAsync(PendingTeamRequest request, CancellationToken cancellationToken = default);
    Task RemoveAsync(string requestId, CancellationToken cancellationToken = default);
}
