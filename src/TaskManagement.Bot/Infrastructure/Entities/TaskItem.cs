namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class TaskItem : BaseEntity
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public required string AssignedTo { get; set; }

    public required string CreatedBy { get; set; }

    public DateTime? DueDate { get; set; }

    public ETaskStatus Status { get; set; } = ETaskStatus.ToDo;

    public DateTime? ReviewStartedAt { get; set; }

    public EPriorityLevel Priority { get; set; } = EPriorityLevel.Medium;

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public ICollection<TaskClan> Clans { get; set; } = new List<TaskClan>();
    public ICollection<TaskChannel> Channels { get; set; } = new List<TaskChannel>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<Complain> Complains { get; set; } = new List<Complain>();
}
