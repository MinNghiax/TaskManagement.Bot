using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;
using ETaskStatus = TaskManagement.Bot.Infrastructure.Enums.ETaskStatus;
namespace TaskManagement.Bot.Application.Services;

public class InMemoryTaskService : ITaskService
{
    private readonly ILogger<InMemoryTaskService> _logger;
    private static readonly Dictionary<int, TaskDto> _store = new();

    public InMemoryTaskService(ILogger<InMemoryTaskService> logger)
    {
        _logger = logger;
    }

    public Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = new TaskDto
            {
                Id = new Random().Next(1, 100000),
                Title = dto.Title ?? "Untitled",
                Description = dto.Description ?? "",
                AssignedTo = dto.AssignedTo ?? "unknown",
                CreatedBy = dto.CreatedBy ?? "unknown",
                Status = ETaskStatus.ToDo,  
                ReviewStartedAt = null,
                DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                ClanIds = dto.ClanIds,
                ChannelIds = dto.ChannelIds,
                ReminderRules = dto.ReminderRules.ToList()
            };

            _store.Add(task.Id, task);
            _logger.LogInformation($"✅ Task created (in-memory): {task.Title} | ID: {task.Id}");
            return Task.FromResult<TaskDto?>(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return Task.FromResult<TaskDto?>(null);
        }
    }

    public Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = _store.Values.FirstOrDefault(t => t.Id == taskId);
            return Task.FromResult(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task");
            return Task.FromResult<TaskDto?>(null);
        }
    }

    public Task<List<TaskDto>> GetByAssigneeAsync(string assignee, string? channelId, CancellationToken cancellationToken = default)
    {
        var tasks = _store.Values
        .Where(t =>
            t.AssignedTo == assignee &&
            (string.IsNullOrEmpty(channelId) || (t.ChannelIds != null && t.ChannelIds.Contains(channelId))))
        .ToList();

        return Task.FromResult(tasks);
    }

    public Task<List<TaskDto>> GetByCreatorAsync(string creator, string? channelId, CancellationToken ct = default)
    {
        try
        {
            var tasks = _store.Values
                .Where(t => t.CreatedBy?.Equals(creator, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            return Task.FromResult(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks by creator");
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

    public Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken cancellationToken = default)
    {
        var task = _store.Values.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            var previousStatus = task.Status;
            task.Status = newStatus;
            task.UpdatedAt = DateTime.UtcNow;
            task.ReviewStartedAt = newStatus == ETaskStatus.Review
                ? task.ReviewStartedAt ?? task.UpdatedAt
                : previousStatus == ETaskStatus.Review
                    ? null
                    : task.ReviewStartedAt;
        }

        return Task.CompletedTask;
    }

    public Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken cancellationToken = default)
    {
        var task = _store.Values.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            task.DueDate = newDueDate;
            task.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = _store.Values.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            _store.Remove(taskId);
        }

        return Task.CompletedTask;
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

    public Task UpdateAsync(int taskId, UpdateTaskDto updateDto, CancellationToken ct = default)
    {
        var task = _store.Values.FirstOrDefault(t => t.Id == taskId);
        if (task == null) throw new Exception("Task not found");

        if (!string.IsNullOrWhiteSpace(updateDto.Title))
            task.Title = updateDto.Title;

        if (updateDto.Description != null)
            task.Description = updateDto.Description;

        if (updateDto.Priority.HasValue)
            task.Priority = updateDto.Priority.Value;

        if (updateDto.Status.HasValue)
        {
            var previousStatus = task.Status;
            task.Status = updateDto.Status.Value;
            task.ReviewStartedAt = updateDto.Status.Value == ETaskStatus.Review
                ? task.ReviewStartedAt ?? DateTime.UtcNow
                : previousStatus == ETaskStatus.Review
                    ? null
                    : task.ReviewStartedAt;
        }

        if (updateDto.DueDate.HasValue)
            task.DueDate = updateDto.DueDate.Value;

        if (!string.IsNullOrWhiteSpace(updateDto.AssignedTo))
            task.AssignedTo = updateDto.AssignedTo;

        if (updateDto.ReminderRules is not null)
            task.ReminderRules = updateDto.ReminderRules.ToList();

        task.UpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    public Task<List<TaskDto>> GetTasksByTeamAsync(int teamId, CancellationToken ct = default)
    {
        var tasks = _store.Values.Where(t => t.TeamId == teamId).ToList();
        return Task.FromResult(tasks);
    }

}
