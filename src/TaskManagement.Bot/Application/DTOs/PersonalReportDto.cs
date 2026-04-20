namespace TaskManagement.Bot.Application.DTOs;

using TaskManagement.Bot.Infrastructure.Enums;

public class TaskSummaryDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public ETaskStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public EPriorityLevel Priority { get; set; }

    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }

    public int OverdueDays { get; set; }
}