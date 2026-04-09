using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class StatisticsReportDto
{
    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }
    public string? ThreadId { get; set; }
    public ETimeRange TimeRange { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    
    public int TaskCreated { get; set; }
    public int TaskCompleted { get; set; }
    public int TaskInProgress { get; set; }
    public int TaskPending { get; set; }

    public double CompletionRate { get; set; }
    public int OverdueTasks { get; set; }
}