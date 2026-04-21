using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public class ReportService : IReportService
{
    private readonly TaskManagementDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly IMezonUserService _userService;

    public ReportService(
        TaskManagementDbContext context,
        ILogger<ReportService> logger,
        IMezonUserService userService)
    {
        _context = context;
        _logger = logger;
        _userService = userService;
    }

    public async Task<UserPersonalReportDto> GetUserPersonalReportAsync(string userId)
    {
        _logger.LogInformation("[REPORT_ME] Getting personal report for user: {UserId}", userId);
        
        // BƯỚC 1: Lấy tasks của user (filter ngay từ đầu)
        var userTasksQuery = _context.TaskItems
            .Where(t => t.AssignedTo.Equals(userId) && !t.IsDeleted);

        // BƯỚC 2: Join với Teams
        var taskTeamQuery = from task in userTasksQuery
                            join team in _context.Teams on task.TeamId equals team.Id
                            where !team.IsDeleted
                            select new { task, team };

        // BƯỚC 3: Join với Projects
        var taskTeamProjectQuery = from x in taskTeamQuery
                                   join project in _context.Projects on x.team.ProjectId equals project.Id
                                   where !project.IsDeleted
                                   select new { x.task, x.team, project };

        // BƯỚC 4: Join với TeamMembers để verify user là member
        var fullQuery = from x in taskTeamProjectQuery
                        join member in _context.TeamMembers on x.team.Id equals member.TeamId
                        where member.Username.Equals(userId) 
                              && member.Status.Equals("Accepted") 
                              && !member.IsDeleted
                        select new
                        {
                            TaskId = x.task.Id,
                            TaskTitle = x.task.Title,
                            TaskStatus = x.task.Status,
                            TaskPriority = x.task.Priority,
                            TaskDueDate = x.task.DueDate,
                            TeamId = x.team.Id,
                            TeamName = x.team.Name,
                            ProjectId = x.project.Id,
                            ProjectName = x.project.Name
                        };

        // BƯỚC 5: Execute query một lần duy nhất
        var results = await fullQuery
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.TeamId)
            .ThenBy(x => x.TaskDueDate)
            .ToListAsync();

        _logger.LogInformation("[REPORT_ME] Found {Count} tasks across teams", results.Count);

        // BƯỚC 6: Group và build DTO
        var projects = results
            .GroupBy(x => new { x.ProjectId, x.ProjectName })
            .Select(projectGroup => new ProjectTaskGroupDto
            {
                ProjectId = projectGroup.Key.ProjectId,
                ProjectName = projectGroup.Key.ProjectName,
                Teams = projectGroup
                    .GroupBy(x => new { x.TeamId, x.TeamName })
                    .Select(teamGroup =>
                    {
                        var tasks = teamGroup.Select(x => new TaskSummaryDto
                        {
                            Id = x.TaskId,
                            Title = x.TaskTitle,
                            Status = x.TaskStatus,
                            Priority = x.TaskPriority,
                            DueDate = x.TaskDueDate
                        }).ToList();

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
                    .ToList()
            })
            .ToList();

        var totalTasks = projects.SelectMany(p => p.Teams).Sum(t => t.TotalTasks);
        var completedTasks = projects.SelectMany(p => p.Teams).Sum(t => t.CompletedTasks);
        var username = await _userService.GetDisplayNameAsync(userId);

        return new UserPersonalReportDto
        {
            UserId = userId,
            Username = username,
            Projects = projects,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            CompletionRate = CalculateCompletionRate(completedTasks, totalTasks)
        };
    }

    public async Task<PMProjectListDto> GetPMProjectsAsync(string pmUserId)
    {
        _logger.LogInformation("[REPORT_PM] Getting projects for PM: {PMUserId}", pmUserId);

        var projectsQuery = _context.Projects
            .Where(p => p.CreatedBy.Equals(pmUserId) && !p.IsDeleted)
            .Select(p => new ProjectSummaryDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                TeamCount = p.Teams.Count(t => !t.IsDeleted),
                TotalTasks = p.Teams
                    .Where(t => !t.IsDeleted)
                    .SelectMany(t => t.Tasks)
                    .Count(task => !task.IsDeleted)
            });

        var projects = await projectsQuery.ToListAsync();

        _logger.LogInformation("[REPORT_PM] Found {Count} projects", projects.Count);

        return new PMProjectListDto
        {
            PMUserId = pmUserId,
            Projects = projects
        };
    }

    public async Task<List<TeamSummaryDto>> GetTeamsByProjectAsync(int projectId)
    {
        _logger.LogInformation("[REPORT_TEAMS] Getting teams for project: {ProjectId}", projectId);

        var teamsQuery = _context.Teams
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .Select(t => new TeamSummaryDto
            {
                TeamId = t.Id,
                TeamName = t.Name,
                MemberCount = t.Members.Count(m => m.Status.Equals("Accepted") && !m.IsDeleted),
                TotalTasks = t.Tasks.Count(task => !task.IsDeleted)
            });

        return await teamsQuery.ToListAsync();
    }

    public async Task<TeamDetailReportDto> GetTeamDetailReportAsync(int teamId)
    {
        _logger.LogInformation("[REPORT_TEAM_DETAIL] Getting detail for team: {TeamId}", teamId);

        var team = await _context.Teams
            .Where(t => t.Id == teamId && !t.IsDeleted)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ProjectId,
                ProjectName = t.Project.Name
            })
            .FirstOrDefaultAsync();

        if (team == null)
        {
            throw new KeyNotFoundException($"Team {teamId} not found");
        }

        // BƯỚC 2: Lấy tasks của team, join với members
        var taskMemberQuery = from task in _context.TaskItems
                              where task.TeamId == teamId && !task.IsDeleted
                              join member in _context.TeamMembers on new { TeamId = teamId, Username = task.AssignedTo }
                                  equals new { member.TeamId, member.Username }
                              where member.Status.Equals("Accepted") && !member.IsDeleted
                              select new
                              {
                                  TaskId = task.Id,
                                  TaskTitle = task.Title,
                                  TaskStatus = task.Status,
                                  TaskPriority = task.Priority,
                                  TaskDueDate = task.DueDate,
                                  MemberUsername = member.Username
                              };

        // BƯỚC 3: Execute query một lần
        var taskResults = await taskMemberQuery
            .OrderBy(x => x.MemberUsername)
            .ThenBy(x => x.TaskDueDate)
            .ToListAsync();

        _logger.LogInformation("[REPORT_TEAM_DETAIL] Found {Count} tasks for team {TeamId}", taskResults.Count, teamId);

        // BƯỚC 4: Lấy danh sách members (distinct từ tasks + members không có task)
        var membersWithTasks = taskResults
            .Select(x => x.MemberUsername)
            .Distinct()
            .ToHashSet();

        var allMembers = await _context.TeamMembers
            .Where(m => m.TeamId == teamId && m.Status.Equals("Accepted") && !m.IsDeleted)
            .Select(m => m.Username)
            .ToListAsync();

        // BƯỚC 5: Lấy display names một lần cho tất cả members
        var userDisplayNames = new Dictionary<string, string>();
        foreach (var username in allMembers)
        {
            userDisplayNames[username] = await _userService.GetDisplayNameAsync(username);
        }

        // BƯỚC 6: Group tasks theo member và build DTO
        var memberReports = allMembers.Select(username =>
        {
            var memberTasks = taskResults
                .Where(x => x.MemberUsername.Equals(username))
                .Select(x => new TaskSummaryDto
                {
                    Id = x.TaskId,
                    Title = x.TaskTitle,
                    Status = x.TaskStatus,
                    Priority = x.TaskPriority,
                    DueDate = x.TaskDueDate
                })
                .ToList();

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

        return new TeamDetailReportDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            ProjectId = team.ProjectId,
            ProjectName = team.ProjectName,
            Members = memberReports
        };
    }

    private static TaskSummaryDto MapToTaskSummary(Infrastructure.Entities.TaskItem task)
    {
        return new TaskSummaryDto
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate
        };
    }

    private static double CalculateCompletionRate(int completed, int total)
    {
        return total > 0 ? (double)completed / total * 100 : 0;
    }
}
