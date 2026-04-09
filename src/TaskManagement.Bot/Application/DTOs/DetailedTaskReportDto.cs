using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class DetailedTaskReportDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }

    public ETaskStatus Status { get; set; }
    public EPriorityLevel Priority { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }
    public string? ThreadId { get; set; }

    public int OverdueDays { get; set; }
    public int TotalDays { get; set; }
    public int CommentCount { get; set; }
}