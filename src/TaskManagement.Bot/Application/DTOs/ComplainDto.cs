// TaskManagement.Bot.Application.DTOs.ComplainDto.cs
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class ComplainDto
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public string? TaskTitle { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? NewDueDate { get; set; }
    public DateTime? OldDueDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateComplainDto
{
    public int TaskItemId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public EComplainType Type { get; set; }
    public DateTime? NewDueDate { get; set; } // Chỉ dùng cho RequestExtend
}

public class ApproveComplainDto
{
    public int ComplainId { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string? RejectReason { get; set; } // Chỉ dùng khi reject
    public bool IsApproved { get; set; }
}