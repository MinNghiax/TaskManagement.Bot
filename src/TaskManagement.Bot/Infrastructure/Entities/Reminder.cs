namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class Reminder : BaseEntity
{
    public int TaskId { get; set; }

    public int ReminderRuleId { get; set; }

    public DateTime TriggerAt { get; set; }

    public required string TargetUserId { get; set; }

    public EReminderStatus Status { get; set; } = EReminderStatus.Pending;

    public DateTime? NextTriggerAt { get; set; }

    public ETaskStatus StateSnapshot { get; set; }

    public ReminderRule? ReminderRule { get; set; }
    public TaskItem? Task { get; set; }
}
