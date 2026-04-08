using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Enums;
using ETaskStatus = TaskManagement.Bot.Infrastructure.Enums.ETaskStatus;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// In-memory task service for testing without database.
/// All tasks are stored in RAM and lost when application restarts.
/// </summary>
public class InMemoryTaskService : ITaskService
{
    private readonly ILogger<InMemoryTaskService> _logger;
    private static readonly Dictionary<Guid, TaskDto> _store = new();

    public InMemoryTaskService(ILogger<InMemoryTaskService> logger)
    {
        _logger = logger;
    }

    public Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var taskId = Guid.NewGuid();
            var task = new TaskDto
            {
                Id = taskId,
                Title = dto.Title ?? "Untitled",
                Description = dto.Description ?? "",
                AssignedTo = dto.AssignedTo ?? "unknown",
                CreatedBy = dto.CreatedBy ?? "unknown",
                Status = ETaskStatus.ToDo,  // Changed from Pending to ToDo
                DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                ChannelId = dto.ChannelId ?? "",
                MessageId = dto.MessageId ?? ""
            };

            _store[taskId] = task;
            _logger.LogInformation($"✅ Task created (in-memory): {task.Title} | ID: {taskId}");
            return Task.FromResult<TaskDto?>(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return Task.FromResult<TaskDto?>(null);
        }
    }

    public Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = _store.TryGetValue(taskId, out var result) ? result : null;
            return Task.FromResult(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task");
            return Task.FromResult<TaskDto?>(null);
        }
    }

    public Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = _store.Values
                .Where(t => t.AssignedTo?.Equals(assignee, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            return Task.FromResult(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks");
            return Task.FromResult(new List<TaskDto>());
        }
    }

    public Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = _store.Values.Where(t => t.Status == status).ToList();
            return Task.FromResult(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks by status");
            return Task.FromResult(new List<TaskDto>());
        }
    }

    public Task ChangeStatusAsync(Guid taskId, ETaskStatus newStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_store.TryGetValue(taskId, out var task))
            {
                var oldStatus = task.Status;
                task.Status = newStatus;
                task.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation($"✅ Task status updated: {task.Title} | {oldStatus} → {newStatus}");
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status");
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_store.Remove(taskId))
            {
                _logger.LogInformation($"✅ Task deleted (in-memory): {taskId}");
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task");
            return Task.CompletedTask;
        }
    }

    public Task<List<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_store.Values.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tasks");
            return Task.FromResult(new List<TaskDto>());
        }
    }
}
