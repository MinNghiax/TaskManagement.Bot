namespace TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Trạng thái của task
/// </summary>
public enum TaskStatus
{
    /// <summary>Chưa bắt đầu</summary>
    ToDo = 0,

    /// <summary>Đang thực hiện</summary>
    InProgress = 1,

    /// <summary>Đã hoàn thành</summary>
    Completed = 2,

    /// <summary>Bị chặn/hủy</summary>
    Cancelled = 3
}
