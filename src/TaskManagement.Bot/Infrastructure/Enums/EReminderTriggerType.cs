namespace TaskManagement.Bot.Infrastructure.Enums;
public enum EReminderTriggerType
{
    BeforeDeadline = 0,
    AfterDeadline = 1,
    Repeat = 2,
    OnDeadline = 3,

    TimeBefore = BeforeDeadline,
    TimeAfter = AfterDeadline,
}
