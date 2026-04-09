namespace TaskManagement.Bot.Infrastructure.Entities;

public class TaskChannel : BaseEntity
{
    public required string ChannelId { get; set; }

    public int TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
}
