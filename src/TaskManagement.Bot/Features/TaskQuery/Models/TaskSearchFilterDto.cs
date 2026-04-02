namespace TaskManagement.Bot.Features.TaskQuery.Models;

using TaskManagement.Bot.Shared.Models;

/// <summary>
/// Task Search Filter - Người 2: Task Search & Query (READ-ONLY)
/// </summary>
public class TaskSearchFilterDto
{
    public string? SearchText { get; set; }
    public TaskStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DeadlineFrom { get; set; }
    public DateTime? DeadlineTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TaskSearchResultDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? Deadline { get; set; }
    public string? AssignedTo { get; set; }
}
