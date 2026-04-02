namespace TaskManagement.Bot.Features.ThreadContext.Models;

using TaskManagement.Bot.Shared.Models;

/// <summary>
/// TaskContext Entity - Người 4: Thread context & message binding
/// </summary>
public class TaskContextEntity : BaseEntity
{
    public int TaskId { get; set; }
    public string? ThreadId { get; set; }
    public string? MessageId { get; set; }
    public string? ChannelId { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTaskContextDto
{
    public int TaskId { get; set; }
    public string? ThreadId { get; set; }
    public string? ChannelId { get; set; }
}
