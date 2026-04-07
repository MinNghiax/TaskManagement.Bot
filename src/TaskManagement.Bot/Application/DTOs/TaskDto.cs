namespace TaskManagement.Bot.Application.DTOs;

using TaskManagement.Bot.Infrastructure.Enums;

public class TaskDto
{
    public int Id { get; set; }
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

public class CreateTaskDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string AssignedTo { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
}

public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskStatus? Status { get; set; }
    public PriorityLevel? Priority { get; set; }
}
