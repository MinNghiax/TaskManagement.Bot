namespace TaskManagement.Bot.Infrastructure.Entities;

using TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Complain entity - Yêu cầu sửa chữa/thay đổi task từ người được giao
/// </summary>
public class Complain : BaseEntity
{
    /// <summary>ID của task cần sửa</summary>
    public int TaskId { get; set; }

    /// <summary>Tiêu đề yêu cầu sửa chữa</summary>
    public required string Title { get; set; }

    /// <summary>Nội dung chi tiết yêu cầu</summary>
    public required string Content { get; set; }

    /// <summary>Loại yêu cầu (e.g., "Clarification", "ChangeDeadline", "ChangeAssignee", "Difficulty")</summary>
    public required string ComplainType { get; set; }

    /// <summary>Người gửi yêu cầu (người được giao task)</summary>
    public required string CreatedBy { get; set; }

    /// <summary>ID Mezon của người tạo yêu cầu</summary>
    public required string MezonUserId { get; set; }

    /// <summary>Trạng thái yêu cầu</summary>
    public ComplainStatus Status { get; set; } = ComplainStatus.Pending;

    /// <summary>Người xử lý yêu cầu (PM hoặc Mentor)</summary>
    public string? RespondedBy { get; set; }

    /// <summary>Phản hồi/giải quyết</summary>
    public string? Response { get; set; }

    /// <summary>Ngày phản hồi</summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>Số người like/support yêu cầu này</summary>
    public int SupportCount { get; set; } = 0;

    // Navigation property
    public Task? Task { get; set; }
}
