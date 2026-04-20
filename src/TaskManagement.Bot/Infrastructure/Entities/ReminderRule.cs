namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

public class ReminderRule : BaseEntity
{
    public EReminderTriggerType TriggerType { get; set; }
    public ETimeUnit? IntervalUnit { get; set; }
    public double Value { get; set; }
    public ETaskStatus TaskStatus { get; set; }
    public bool IsRepeat { get; set; } = false;
}
