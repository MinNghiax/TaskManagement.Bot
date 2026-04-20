namespace TaskManagement.Bot.Infrastructure.Enums;

public enum EReminderTriggerType
{
    BeforeDeadline = 0,
    AfterDeadline = 1,
    Repeat = 2,

    TimeBefore = BeforeDeadline,
    TimeAfter = AfterDeadline,
}
