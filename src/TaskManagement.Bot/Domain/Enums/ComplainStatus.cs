namespace TaskManagement.Bot.Domain.Enums;

/// <summary>
/// Trạng thái của complain (yêu cầu sửa chữa task)
/// </summary>
public enum ComplainStatus
{
    /// <summary>Vừa tạo</summary>
    Pending = 0,

    /// <summary>Đang xem xét</summary>
    InProgress = 1,

    /// <summary>Đã giải quyết</summary>
    Resolved = 2,

    /// <summary>Từ chối</summary>
    Rejected = 3
}
