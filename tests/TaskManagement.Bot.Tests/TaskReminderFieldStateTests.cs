using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;

namespace TaskManagement.Bot.Tests;

public class TaskReminderFieldStateTests
{
    [Fact]
    public void Validate_DefaultState_CreatesBeforeAndAfterRules()
    {
        var result = TaskReminderFieldState.Default().Validate();

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Rules.Count);

        Assert.Equal(EReminderTriggerType.BeforeDeadline, result.Rules[0].TriggerType);
        Assert.Equal(30, result.Rules[0].Value);
        Assert.Equal(ETimeUnit.Minutes, result.Rules[0].IntervalUnit);
        Assert.False(result.Rules[0].IsRepeat);

        Assert.Equal(EReminderTriggerType.AfterDeadline, result.Rules[1].TriggerType);
        Assert.Equal(10, result.Rules[1].Value);
        Assert.Equal(ETimeUnit.Minutes, result.Rules[1].IntervalUnit);
        Assert.True(result.Rules[1].IsRepeat);
    }

    [Fact]
    public void Validate_DisabledState_CreatesNoRules()
    {
        var result = TaskReminderFieldState.Default(isEnabled: false).Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Rules);
    }

    [Fact]
    public void Validate_EnabledWithoutAnyRule_ReturnsValidWithoutCustomRules()
    {
        var result = new TaskReminderFieldState
        {
            IsEnabled = true,
            BeforeValue = string.Empty,
            BeforeUnit = ETimeUnit.Minutes,
            AfterValue = string.Empty,
            AfterUnit = ETimeUnit.Minutes,
            RepeatValue = string.Empty,
            RepeatUnit = null
        }.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Rules);
    }

    [Fact]
    public void Validate_WeekValueOutsideRange_ReturnsInvalid()
    {
        var result = new TaskReminderFieldState
        {
            IsEnabled = true,
            BeforeValue = "5",
            BeforeUnit = ETimeUnit.Weeks,
            AfterValue = string.Empty,
            AfterUnit = ETimeUnit.Minutes,
            RepeatValue = string.Empty,
            RepeatUnit = null
        }.Validate();

        Assert.False(result.IsValid);
    }
}
