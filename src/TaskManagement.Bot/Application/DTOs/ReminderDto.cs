namespace TaskManagement.Bot.Application.DTOs;

public class ReminderDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime ReminderTime { get; set; }
    public string? Message { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? MezonUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReminderDto
{
    public int TaskId { get; set; }
    public DateTime ReminderTime { get; set; }
    public string? Message { get; set; }
    public required string MezonUserId { get; set; }
}

public class UpdateReminderDto
{
    public DateTime? ReminderTime { get; set; }
    public string? Message { get; set; }
}
