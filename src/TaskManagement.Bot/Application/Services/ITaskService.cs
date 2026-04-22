namespace TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

public interface ITaskService
{
    Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);

    Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken cancellationToken = default);

    Task<List<TaskDto>> GetByCreatorAsync(string creator, string? channelId, CancellationToken ct);

    Task<List<TaskDto>> GetByAssigneeAsync(string assignee, string? channelId, CancellationToken cancellationToken = default);

    Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken cancellationToken = default);

    Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken cancellationToken = default);

    Task DeleteAsync(int taskId, CancellationToken cancellationToken = default);

    Task<List<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(int taskId, UpdateTaskDto updateDto, CancellationToken ct = default);
    Task<List<TaskDto>> GetTasksByTeamAsync(int teamId, CancellationToken ct = default);
}

