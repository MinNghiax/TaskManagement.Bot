namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Task-specific repository interface
/// </summary>
public interface ITaskRepository : IRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetByAssigneeAsync(string assignedTo);
    Task<IEnumerable<TaskItem>> GetByStatusAsync(ETaskStatus status);
    Task<IEnumerable<TaskItem>> GetByCreatedByAsync(string createdBy);
    Task<IEnumerable<TaskItem>> GetOverdueAsync();
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(EPriorityLevel priority);
    Task<IEnumerable<TaskItem>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
