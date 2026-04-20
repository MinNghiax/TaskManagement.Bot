using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Domain.Interfaces;

public interface IComplainRepository
{
    Task<Complain?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Complain?> GetPendingByTaskAsync(int taskItemId, CancellationToken ct = default);
    Task<List<Complain>> GetByTaskAsync(int taskItemId, CancellationToken ct = default);
    Task<Complain> CreateAsync(Complain complain, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
    Task<List<Complain>> GetPendingByPMAsync(string pmUserId, CancellationToken ct = default);
}