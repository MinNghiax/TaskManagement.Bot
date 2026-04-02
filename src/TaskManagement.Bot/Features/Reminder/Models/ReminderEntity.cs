namespace TaskManagement.Bot.Features.Reminder.Models;

using TaskManagement.Bot.Shared.Models;

/// <summary>
/// Reminder Entity - Người 3: Reminders & Scheduling
/// </summary>
public class ReminderEntity : BaseEntity
{
    public int TaskId { get; set; }
    public DateTime ReminderTime { get; set; }
    public ReminderRepeatType RepeatType { get; set; }
    public bool IsTriggered { get; set; }
    public string? NotificationMessage { get; set; }
}

public enum ReminderRepeatType
{
    Once = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public class CreateReminderDto
{
    public int TaskId { get; set; }
    public DateTime ReminderTime { get; set; }
    public ReminderRepeatType RepeatType { get; set; }
}
