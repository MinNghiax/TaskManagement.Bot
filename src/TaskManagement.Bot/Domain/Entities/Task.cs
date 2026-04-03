namespace TaskManagement.Bot.Domain.Entities;

using TaskManagement.Bot.Domain.Enums;

/// <summary>
/// Task entity - Đại diện cho một công việc cần làm
/// </summary>
public class Task : BaseEntity
{
    /// <summary>Tiêu đề của task</summary>
    public required string Title { get; set; }

    /// <summary>Mô tả chi tiết</summary>
    public string? Description { get; set; }

    /// <summary>Người được giao task</summary>
    public required string AssignedTo { get; set; }

    /// <summary>Người tạo task</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Ngày hạn cuối</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Trạng thái task</summary>
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    /// <summary>Mức độ ưu tiên</summary>
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    /// <summary>Channel/Thread ID từ Mezon</summary>
    public string? ChannelId { get; set; }

    /// <summary>Message ID liên kết (nếu có)</summary>
    public string? MessageId { get; set; }

    // Navigation properties
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<Complain> Complains { get; set; } = new List<Complain>();
}
