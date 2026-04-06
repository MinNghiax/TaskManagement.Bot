namespace TaskManagement.Bot.Infrastructure.Entities;

/// <summary>
/// Report entity - Báo cáo, thống kê về các tasks
/// </summary>
public class Report : BaseEntity
{
    /// <summary>Tiêu đề báo cáo</summary>
    public required string Title { get; set; }

    /// <summary>Loại báo cáo (e.g., "TaskSummary", "TaskByStatus", "TaskByAssignee")</summary>
    public required string ReportType { get; set; }

    /// <summary>Thời gian bắt đầu của khoảng báo cáo</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Thời gian kết thúc của khoảng báo cáo</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Nội dung báo cáo (JSON format)</summary>
    public required string Content { get; set; }

    /// <summary>Người tạo báo cáo</summary>
    public required string CreatedBy { get; set; }

    /// <summary>Mô tả thêm</summary>
    public string? Description { get; set; }

    /// <summary>Số lượng tasks tổng cộng trong báo cáo</summary>
    public int TotalTasks { get; set; }

    /// <summary>Số lượng tasks đã hoàn thành</summary>
    public int CompletedTasks { get; set; }

    /// <summary>Số lượng tasks đang thực hiện</summary>
    public int InProgressTasks { get; set; }

    /// <summary>Số lượng tasks chưa bắt đầu</summary>
    public int PendingTasks { get; set; }

    /// <summary>Số lượng tasks đã hủy</summary>
    public int CancelledTasks { get; set; }
}
