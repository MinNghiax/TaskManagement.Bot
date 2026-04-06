namespace TaskManagement.Bot.Application.Services;

using TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Interface for task management service.
/// Provides methods to create, read, update, and delete tasks.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Create a new task.
    /// </summary>
    Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a task by ID.
    /// </summary>
    Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks assigned to a specific user.
    /// </summary>
    Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks with a specific status.
    /// </summary>
    Task<List<TaskDto>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the status of a task.
    /// </summary>
    Task ChangeStatusAsync(Guid taskId, TaskStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a task.
    /// </summary>
    Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks.
    /// </summary>
    Task<List<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for creating a task.
/// </summary>
public class CreateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// DTO for reading task data.
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
