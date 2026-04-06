namespace TaskManagement.Bot.Infrastructure.Entities;

/// <summary>
/// Reminder entity - Nhắc nhở cho task tại thời điểm nhất định
/// </summary>
public class Reminder : BaseEntity
{
    /// <summary>ID của task cần nhắc nhở</summary>
    public int TaskId { get; set; }

    /// <summary>Thời điểm nhắc nhở</summary>
    public DateTime ReminderTime { get; set; }

    /// <summary>Nội dung nhắc nhở</summary>
    public string? Message { get; set; }

    /// <summary>Đã gửi chưa</summary>
    public bool IsSent { get; set; } = false;

    /// <summary>Ngày thực gửi (nếu đã gửi)</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Người được nhắc nhở (ID Mezon)</summary>
    public required string MezonUserId { get; set; }

    // Navigation property
    public Task? Task { get; set; }
}
