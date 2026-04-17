using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class StatisticsReportDto
{
    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }
    public ETimeRange TimeRange { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    public int TaskCreated { get; set; }
    public int TaskCompleted { get; set; }
    public int TotalTasks { get; set; }
    public int OpenTasks { get; set; }
    public int TaskPending { get; set; }
    public int TaskDoing { get; set; }
    public int TaskReview { get; set; }
    public int TaskLate { get; set; }
    public int TaskCompletedOverall { get; set; }
    public int TaskCanceled { get; set; }
    public int TaskInProgress { get; set; }
    public double CompletionRate { get; set; }
    public double PeriodCompletionRate { get; set; }
    public int OverdueTasks { get; set; }

    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public List<ProjectStatisticsDto> Projects { get; set; } = new();
}

public class ProjectStatisticsDto
{
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TeamCount { get; set; }
    public int TotalTasks { get; set; }
    public int CreatedTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OpenTasks { get; set; }
    public int DoingTasks { get; set; }
    public int ReviewTasks { get; set; }
    public int LateTasks { get; set; }
    public int CompletedTasksOverall { get; set; }
    public int CanceledTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionRate { get; set; }
    public double PeriodCompletionRate { get; set; }
}
