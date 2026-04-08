namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Complain-specific repository interface
/// </summary>
public interface IComplainRepository : IRepository<Complain>
{
    Task<IEnumerable<Complain>> GetByTaskIdAsync(int taskId);
    Task<IEnumerable<Complain>> GetByStatusAsync(EComplainStatus status);
    Task<IEnumerable<Complain>> GetPendingAsync();
    Task<IEnumerable<Complain>> GetByCreatorAsync(string createdBy);
    Task<IEnumerable<Complain>> GetByTypeAsync(string complainType);
}
