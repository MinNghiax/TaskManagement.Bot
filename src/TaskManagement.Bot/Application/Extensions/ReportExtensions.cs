using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Extensions;

public static class ReportExtensions
{
    public static List<TeamTaskGroupDto> GroupByTeam(
        this IEnumerable<(int TeamId, string TeamName, int TaskId, string TaskTitle, ETaskStatus TaskStatus, EPriorityLevel TaskPriority, DateTime? TaskDueDate)> source)
    {
        return source
            .GroupBy(x => new { x.TeamId, x.TeamName })
            .Select(teamGroup =>
            {
                var tasks = teamGroup
                    .Select(x => new TaskSummaryDto
                    {
                        Id = x.TaskId,
                        Title = x.TaskTitle,
                        Status = x.TaskStatus,
                        Priority = x.TaskPriority,
                        DueDate = x.TaskDueDate
                    })
                    .ToList();

                var completedCount = tasks.Count(t => t.Status == ETaskStatus.Completed);

                return new TeamTaskGroupDto
                {
                    TeamId = teamGroup.Key.TeamId,
                    TeamName = teamGroup.Key.TeamName,
                    Tasks = tasks,
                    TotalTasks = tasks.Count,
                    CompletedTasks = completedCount,
                    CompletionRate = CalculateCompletionRate(completedCount, tasks.Count)
                };
            })
            .ToList();
    }

    public static List<ProjectTaskGroupDto> GroupByProject(
        this IEnumerable<(int ProjectId, string ProjectName, int TeamId, string TeamName, int TaskId, string TaskTitle, ETaskStatus TaskStatus, EPriorityLevel TaskPriority, DateTime? TaskDueDate)> source)
    {
        return source
            .GroupBy(x => new { x.ProjectId, x.ProjectName })
            .Select(projectGroup =>
            {
                var teams = projectGroup
                    .Select(x => (x.TeamId, x.TeamName, x.TaskId, x.TaskTitle, x.TaskStatus, x.TaskPriority, x.TaskDueDate))
                    .GroupByTeam();

                return new ProjectTaskGroupDto
                {
                    ProjectId = projectGroup.Key.ProjectId,
                    ProjectName = projectGroup.Key.ProjectName,
                    Teams = teams
                };
            })
            .ToList();
    }

    /// <summary>
    /// Map task data tuple sang TaskSummaryDto
    /// </summary>
    public static TaskSummaryDto ToTaskSummaryDto(
        this (int TaskId, string TaskTitle, ETaskStatus TaskStatus, EPriorityLevel TaskPriority, DateTime? TaskDueDate) task)
    {
        return new TaskSummaryDto
        {
            Id = task.TaskId,
            Title = task.TaskTitle,
            Status = task.TaskStatus,
            Priority = task.TaskPriority,
            DueDate = task.TaskDueDate
        };
    }

    /// <summary>
    /// Group tasks theo member và build MemberTaskReportDto với statistics
    /// </summary>
    public static List<MemberTaskReportDto> GroupByMember(
        this IEnumerable<(int TaskId, string TaskTitle, ETaskStatus TaskStatus, EPriorityLevel TaskPriority, DateTime? TaskDueDate, string AssignedTo)> tasks,
        IEnumerable<string> allMembers,
        Dictionary<string, string> userDisplayNames)
    {
        // Group tasks by member
        var tasksByMember = tasks
            .GroupBy(t => t.AssignedTo)
            .ToDictionary(g => g.Key, g => g.ToList());

        return allMembers
            .Select(username =>
            {
                // Get tasks for this member
                var memberTasks = tasksByMember.ContainsKey(username)
                    ? tasksByMember[username]
                        .Select(t => (t.TaskId, t.TaskTitle, t.TaskStatus, t.TaskPriority, t.TaskDueDate))
                        .Select(t => t.ToTaskSummaryDto())
                        .OrderBy(t => t.DueDate)
                        .ToList()
                    : new List<TaskSummaryDto>();

                var completedCount = memberTasks.Count(t => t.Status == ETaskStatus.Completed);

                return new MemberTaskReportDto
                {
                    UserId = username,
                    Username = userDisplayNames[username],
                    Tasks = memberTasks,
                    TotalTasks = memberTasks.Count,
                    CompletedTasks = completedCount,
                    CompletionRate = CalculateCompletionRate(completedCount, memberTasks.Count)
                };
            })
            .OrderByDescending(m => m.TotalTasks)
            .ThenBy(m => m.Username)
            .ToList();
    }

    public static double CalculateCompletionRate(int completed, int total)
    {
        return total > 0 ? (double)completed / total * 100 : 0;
    }
}
