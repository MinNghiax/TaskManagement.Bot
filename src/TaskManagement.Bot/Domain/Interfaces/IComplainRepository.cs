namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Domain.Entities;
using TaskManagement.Bot.Domain.Enums;

/// <summary>
/// Complain-specific repository interface
/// </summary>
public interface IComplainRepository : IRepository<Complain>
{
    Task<IEnumerable<Complain>> GetByTaskIdAsync(int taskId);
    Task<IEnumerable<Complain>> GetByStatusAsync(ComplainStatus status);
    Task<IEnumerable<Complain>> GetPendingAsync();
    Task<IEnumerable<Complain>> GetByCreatorAsync(string createdBy);
    Task<IEnumerable<Complain>> GetByTypeAsync(string complainType);
}
