using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using Xunit;

namespace TaskManagement.Bot.Tests;

public class ReminderMessageBuilderTests
{
    [Fact]
    public void BuildReminderNotification_IncludesReminderFieldsWithProjectAndTeam()
    {
        var content = MessageBuilder.BuildReminderNotification(
            CreateReminder(EReminderTriggerType.BeforeDeadline),
            "alice");

        Assert.Equal("Reminder Task #42", content.Text);

        var embed = Assert.Single(content.Embed!);
        Assert.Contains("Task #42", GetProperty<string>(embed, "title"));
        Assert.Equal("#FEE75C", GetProperty<string>(embed, "color"));

        var fields = GetProperty<object[]>(embed, "fields");
        AssertFieldValue(fields, "alice");
        AssertFieldValue(fields, "Project Apollo");
        AssertFieldValue(fields, "Backend");
        AssertFieldValue(fields, "Ship reminder refactor");
        AssertFieldValue(fields, "21/04/2026 12:00");
        AssertFieldValueContains(fields, "ToDo");
        AssertFieldValueContains(fields, "Cao");
        AssertFieldValueContains(fields, "30");
    }

    [Fact]
    public void BuildReminderNotification_UsesFallbackValuesForMissingFields()
    {
        var reminder = new Reminder
        {
            TaskId = 100,
            ReminderRuleId = 1,
            TargetUserId = "123",
            TriggerAt = new DateTime(2026, 4, 21, 12, 0, 0),
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline
            }
        };

        var content = MessageBuilder.BuildReminderNotification(reminder, null);

        Assert.Equal("Deadline Task #100", content.Text);

        var fields = GetProperty<object[]>(Assert.Single(content.Embed!), "fields");
        AssertFieldValue(fields, "Unknown User");
        AssertFieldValue(fields, "None");
        AssertFieldValue(fields, "Unknown");
    }

    private static Reminder CreateReminder(EReminderTriggerType triggerType)
    {
        var project = new Project
        {
            Id = 7,
            Name = "Project Apollo",
            CreatedBy = "pm"
        };
        var team = new Team
        {
            Id = 8,
            Name = "Backend",
            CreatedBy = "pm",
            ProjectId = project.Id,
            Project = project
        };
        var task = new TaskItem
        {
            Id = 42,
            Title = "Ship reminder refactor",
            AssignedTo = "123",
            CreatedBy = "pm",
            DueDate = new DateTime(2026, 4, 21, 12, 0, 0),
            Status = ETaskStatus.ToDo,
            Priority = EPriorityLevel.High,
            TeamId = team.Id,
            Team = team
        };

        return new Reminder
        {
            Id = 1,
            TaskId = task.Id,
            ReminderRuleId = 1,
            TargetUserId = "123",
            TriggerAt = new DateTime(2026, 4, 21, 11, 30, 0),
            ReminderRule = new ReminderRule
            {
                Id = 1,
                TriggerType = triggerType,
                IntervalUnit = ETimeUnit.Minutes,
                Value = 30
            },
            Task = task
        };
    }

    private static void AssertFieldValue(object[] fields, string expectedValue)
    {
        Assert.Contains(fields, field =>
            string.Equals(GetProperty<string>(field, "value"), expectedValue, StringComparison.Ordinal));
    }

    private static void AssertFieldValueContains(object[] fields, string expectedValuePart)
    {
        Assert.Contains(fields, field =>
            GetProperty<string>(field, "value").Contains(expectedValuePart, StringComparison.Ordinal));
    }

    private static T GetProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        return Assert.IsType<T>(property.GetValue(source));
    }
}
