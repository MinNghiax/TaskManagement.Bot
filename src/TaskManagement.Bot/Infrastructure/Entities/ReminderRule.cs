namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class ReminderRule : BaseEntity
{
    public string? name { get; set; }
    public required ReminderTriggerType TriggerType { get; set; }
    public required double Value { get; set; }
    public TaskStatus TaskStatus { get; set; }
    public double? RepeatIntervalValue { get; set; }
    public TimeUnit? RepeatIntervalUnit { get; set; }
    public bool IsActive { get; set; } = true;
}