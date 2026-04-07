namespace TaskManagement.Bot.Infrastructure.Entities;

public class TaskClan : BaseEntity
{
    public required string ClanId { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
}