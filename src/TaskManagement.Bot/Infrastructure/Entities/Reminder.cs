namespace TaskManagement.Bot.Infrastructure.Entities;

public class Reminder : BaseEntity
{
    public int TaskId { get; set; }

    public DateTime ReminderTime { get; set; }

    public string? Message { get; set; }

    public bool IsSent { get; set; } = false;

    public DateTime? SentAt { get; set; }

    public required string MezonUserId { get; set; }

    public TaskItem? Task { get; set; }
}
