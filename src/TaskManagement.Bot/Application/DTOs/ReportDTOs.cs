using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class UserPersonalReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<ProjectTaskGroupDto> Projects { get; set; } = new();
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
}

public class ProjectTaskGroupDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<TeamTaskGroupDto> Teams { get; set; } = new();
}

public class TeamTaskGroupDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public List<TaskSummaryDto> Tasks { get; set; } = new();
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
}

public class PMProjectListDto
{
    public string PMUserId { get; set; } = string.Empty;
    public List<ProjectSummaryDto> Projects { get; set; } = new();
}

public class ProjectSummaryDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TeamCount { get; set; }
    public int TotalTasks { get; set; }
}

public class TeamSummaryDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int TotalTasks { get; set; }
}

public class TeamDetailReportDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<MemberTaskReportDto> Members { get; set; } = new();
}

public class MemberTaskReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<TaskSummaryDto> Tasks { get; set; } = new();
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
}
