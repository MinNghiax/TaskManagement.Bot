namespace TaskManagement.Bot.Domain.Entities;

/// <summary>
/// Base entity class - tất cả entities phải inherit lớp này
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }

    /// <summary>Ngày tạo</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối cùng</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Đã bị xóa mềm (soft delete)</summary>
    public bool IsDeleted { get; set; } = false;
}
