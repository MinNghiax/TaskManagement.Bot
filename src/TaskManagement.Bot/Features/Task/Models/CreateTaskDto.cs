namespace TaskManagement.Bot.Features.Task.Models;

/// <summary>
/// Create Task DTO - Người 1: Task Management
/// </summary>
public class CreateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public string? AssignedTo { get; set; }
    public int Priority { get; set; }
}
