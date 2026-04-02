namespace TaskManagement.Bot.Features.Task.Models;

using TaskManagement.Bot.Shared.Models;

/// <summary>
/// Task entity - Người 1: Task Management
/// </summary>
public class TaskEntity : BaseEntity
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public string? AssignedTo { get; set; }
    public int Priority { get; set; }
}

public enum TaskStatus
{
    ToDo = 0,
    InProgress = 1,
    Done = 2,
    Blocked = 3
}
