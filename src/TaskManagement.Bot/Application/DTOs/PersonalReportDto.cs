namespace TaskManagement.Bot.Application.DTOs;

using TaskManagement.Bot.Infrastructure.Enums;

public class PersonalReportDto
{
    public string UserId { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public int DoingTasks { get; set; }
    public int ReviewTasks { get; set; }
    public int PausedTasks { get; set; }
    public int LateTasks { get; set; }
    public int CompletedTasks { get; set; }

    public double CompletionRate { get; set; }
    public int OverdueDays { get; set; }

    public List<TaskSummaryDto> Tasks { get; set; } = new();
}

public class TaskSummaryDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public ETaskStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public EPriorityLevel Priority { get; set; }

    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }
    public string? ThreadId { get; set; }
    
    public int OverdueDays { get; set; }
}