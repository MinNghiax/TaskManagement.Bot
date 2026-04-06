namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Task-specific repository interface
/// </summary>
public interface ITaskRepository : IRepository<Task>
{
    Task<IEnumerable<Task>> GetByAssigneeAsync(string assignedTo);
    Task<IEnumerable<Task>> GetByStatusAsync(TaskStatus status);
    Task<IEnumerable<Task>> GetByCreatedByAsync(string createdBy);
    Task<IEnumerable<Task>> GetOverdueAsync();
    Task<IEnumerable<Task>> GetByPriorityAsync(PriorityLevel priority);
    Task<IEnumerable<Task>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
