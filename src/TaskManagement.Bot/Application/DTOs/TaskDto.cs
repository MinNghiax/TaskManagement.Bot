namespace TaskManagement.Bot.Application.DTOs;

using TaskManagement.Bot.Infrastructure.Enums;

public class TaskDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public ETaskStatus Status { get; set; }
    public EPriorityLevel Priority { get; set; }

    public int? TeamId { get; set; }
    public List<string>? ClanIds { get; set; }
    public List<string>? ChannelIds { get; set; }
    public List<string>? ThreadIds { get; set; }
    public List<CreateReminderRuleDto> ReminderRules { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTaskDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string AssignedTo { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public EPriorityLevel Priority { get; set; } = EPriorityLevel.Medium;
    public int? TeamId { get; set; }
    public List<string>? ClanIds { get; set; }
    public List<string>? ChannelIds { get; set; }
    public List<string>? ThreadIds { get; set; }
    public List<CreateReminderRuleDto> ReminderRules { get; set; } = new();
}

public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public ETaskStatus? Status { get; set; }
    public EPriorityLevel? Priority { get; set; }
    public List<CreateReminderRuleDto>? ReminderRules { get; set; }
}

public class CreateReminderRuleDto
{
    public EReminderTriggerType TriggerType { get; set; }
    public ETimeUnit IntervalUnit { get; set; }
    public double Value { get; set; }
    public bool IsRepeat { get; set; }
}
