namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class Reminder : BaseEntity
{
    public int TaskId { get; set; }

    public Guid ReminderRuleId { get; set; }

    public DateTime TriggerAt { get; set; }

    public Guid TargetUserId { get; set; }

    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public DateTime? NextTriggerAt { get; set; }

    public TaskStatus StateSnapshot { get; set; }

    public ReminderRule? ReminderRule { get; set; }
    public TaskItem? Task { get; set; }
}
