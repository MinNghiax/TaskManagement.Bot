namespace TaskManagement.Bot.Application.DTOs;

/// <summary>
/// Reminder DTO
/// </summary>
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

/// <summary>
/// Create Reminder DTO
/// </summary>
public class CreateReminderDto
{
    public int TaskId { get; set; }
    public DateTime ReminderTime { get; set; }
    public string? Message { get; set; }
    public required string MezonUserId { get; set; }
}

/// <summary>
/// Update Reminder DTO
/// </summary>
public class UpdateReminderDto
{
    public DateTime? ReminderTime { get; set; }
    public string? Message { get; set; }
}
