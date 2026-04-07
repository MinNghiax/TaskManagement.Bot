namespace TaskManagement.Bot.Infrastructure.Entities;

public class TaskThread : BaseEntity
{
    public required string ThreadId { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
}