// TaskManagement.Bot.Application.Services.ITaskService.cs
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public interface ITaskService
{
    Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken ct = default);
    Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken ct = default);
    Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken ct = default);
    Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken ct = default);
    Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken ct = default);
    Task DeleteAsync(int taskId, CancellationToken ct = default);
    Task<List<TaskDto>> GetAllAsync(CancellationToken ct = default);
}

public class CreateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public ETaskStatus Status { get; set; } = ETaskStatus.ToDo;
    public EPriorityLevel Priority { get; set; } = EPriorityLevel.Medium;
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
}

public class TaskDto
{
    public int Id { get; set; }          // ← int, not Guid
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public ETaskStatus Status { get; set; }
    public EPriorityLevel Priority { get; set; }
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}