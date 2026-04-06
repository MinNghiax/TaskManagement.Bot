namespace TaskManagement.Bot.Infrastructure.Enums;

/// <summary>
/// Mức độ ưu tiên của task
/// </summary>
public enum PriorityLevel
{
    /// <summary>Thấp</summary>
    Low = 0,

    /// <summary>Trung bình</summary>
    Medium = 1,

    /// <summary>Cao</summary>
    High = 2,

    /// <summary>Rất cao - Cấp tốc</summary>
    Critical = 3
}
