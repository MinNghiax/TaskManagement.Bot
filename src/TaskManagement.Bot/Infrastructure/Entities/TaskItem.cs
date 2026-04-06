namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class TaskItem : BaseEntity
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public required string AssignedTo { get; set; }

    public required string CreatedBy { get; set; }

    public DateTime? DueDate { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    public string? ChannelId { get; set; }

    public string? MessageId { get; set; }

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<Complain> Complains { get; set; } = new List<Complain>();
}
