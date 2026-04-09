namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class ReminderRule : BaseEntity
{
    public string? Name { get; set; }
    public EReminderTriggerType TriggerType { get; set; }
    public ETimeUnit? IntervalUnit { get; set; }
    public double Value { get; set; }
    public ETaskStatus TaskStatus { get; set; }
    public double? RepeatIntervalValue { get; set; }
    public ETimeUnit? RepeatIntervalUnit { get; set; }
    public bool IsActive { get; set; } = true;
}