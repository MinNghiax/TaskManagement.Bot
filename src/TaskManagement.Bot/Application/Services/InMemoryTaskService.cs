// TaskManagement.Bot.Application.Services.InMemoryTaskService.cs
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

/// <summary>In-memory task service for testing. All data lost on restart.</summary>
public class InMemoryTaskService : ITaskService
{
    private readonly ILogger<InMemoryTaskService> _logger;
    private static int _nextId = 1;
    private static readonly Dictionary<int, TaskDto> _store = new();

    public InMemoryTaskService(ILogger<InMemoryTaskService> logger) => _logger = logger;

    public Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        var id = _nextId++;
        var t = new TaskDto
        {
            Id = id,
            Title = dto.Title ?? "Untitled",
            Description = dto.Description ?? "",
            AssignedTo = dto.AssignedTo ?? "unknown",
            CreatedBy = dto.CreatedBy ?? "unknown",
            Status = ETaskStatus.ToDo,
            DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _store[id] = t;
        _logger.LogInformation("Task created (in-memory): {Title} ID:{Id}", t.Title, id);
        return Task.FromResult<TaskDto?>(t);
    }

    public Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(taskId, out var t) ? t : null);

    public Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Where(t => t.AssignedTo == assignee).ToList());

    public Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Where(t => t.Status == status).ToList());

    public Task<List<TaskDto>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(_store.Values.ToList());

    public Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken ct = default)
    {
        if (_store.TryGetValue(taskId, out var t)) { t.Status = newStatus; t.UpdatedAt = DateTime.UtcNow; }
        return Task.CompletedTask;
    }

    public Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken ct = default)
    {
        if (_store.TryGetValue(taskId, out var t)) { t.DueDate = newDueDate; t.UpdatedAt = DateTime.UtcNow; }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int taskId, CancellationToken ct = default)
    {
        _store.Remove(taskId);
        return Task.CompletedTask;
    }
}