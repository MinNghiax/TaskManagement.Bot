namespace TaskManagement.Bot.Application.DTOs;

public class TeamReportDto
{
    public string? ClanId { get; set; }
    public string? ChannelId { get; set; }
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    public int TotalMembers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double TeamCompletionRate { get; set; }
    public int TotalOverdueTasks { get; set; }

    public List<MemberReportDto> MemberReports { get; set; } = new();
}

public class MemberReportDto
{
    public string MemberId { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public double DoingTasks { get; set; }
    public int ReviewTasks { get; set; }
    public int PausedTasks { get; set; }
    public int LateTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
}