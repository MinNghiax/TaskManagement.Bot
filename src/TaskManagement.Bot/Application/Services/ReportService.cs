using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Extensions;
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

        // BƯỚC 1: Query từ TaskItems với navigation properties
        var query = _context.TaskItems
            .Where(t => 
                t.AssignedTo.Equals(userId) 
                && !t.IsDeleted
                && t.Team != null 
                && !t.Team.IsDeleted
                && t.Team.Project != null
                && !t.Team.Project.IsDeleted
                // ⭐ Validate user là member của team bằng Any()
                && t.Team.Members.Any(m => 
                    m.Username.Equals(userId) 
                    && m.Status.Equals("Accepted") 
                    && !m.IsDeleted))
            // ⭐ Chỉ select đúng fields cần thiết
            .Select(t => new
            {
                ProjectId = t.Team!.Project!.Id,
                ProjectName = t.Team.Project.Name,
                TeamId = t.Team.Id,
                TeamName = t.Team.Name,
                TaskId = t.Id,
                TaskTitle = t.Title,
                TaskStatus = t.Status,
                TaskPriority = t.Priority,
                TaskDueDate = t.DueDate
            })
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.TeamId)
            .ThenBy(x => x.TaskDueDate);

        // BƯỚC 2: Execute query một lần duy nhất
        var results = await query.ToListAsync();

        _logger.LogInformation("[REPORT_ME] Found {Count} tasks across teams", results.Count);

        // BƯỚC 3: Group và build DTO bằng extension methods
        var projects = results
            .Select(x => (
                x.ProjectId, 
                x.ProjectName, 
                x.TeamId, 
                x.TeamName, 
                x.TaskId, 
                x.TaskTitle, 
                x.TaskStatus, 
                x.TaskPriority, 
                x.TaskDueDate))
            .GroupByProject();

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
            CompletionRate = ReportExtensions.CalculateCompletionRate(completedTasks, totalTasks)
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

        // BƯỚC 1: Lấy team info + tasks với navigation properties
        var teamQuery = _context.Teams
            .Where(t => t.Id == teamId && !t.IsDeleted)
            .Select(t => new
            {
                TeamId = t.Id,
                TeamName = t.Name,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                // Lấy luôn tasks và members trong 1 query
                Tasks = t.Tasks
                    .Where(task => !task.IsDeleted)
                    .Select(task => new
                    {
                        TaskId = task.Id,
                        TaskTitle = task.Title,
                        TaskStatus = task.Status,
                        TaskPriority = task.Priority,
                        TaskDueDate = task.DueDate,
                        AssignedTo = task.AssignedTo
                    })
                    .ToList(),
                Members = t.Members
                    .Where(m => m.Status.Equals("Accepted") && !m.IsDeleted)
                    .Select(m => m.Username)
                    .ToList()
            });

        var teamData = await teamQuery.FirstOrDefaultAsync();

        if (teamData == null)
        {
            throw new KeyNotFoundException($"Team {teamId} not found");
        }

        _logger.LogInformation(
            "[REPORT_TEAM_DETAIL] Found team {TeamId} with {TaskCount} tasks and {MemberCount} members",
            teamId,
            teamData.Tasks.Count,
            teamData.Members.Count);

        // BƯỚC 2: Lấy display names một lần cho tất cả members
        var userDisplayNames = new Dictionary<string, string>();
        foreach (var username in teamData.Members)
        {
            userDisplayNames[username] = await _userService.GetDisplayNameAsync(username);
        }

        // BƯỚC 3: Group tasks theo member
        var tasksData = teamData.Tasks
            .Select(t => (
                TaskId: t.TaskId,
                TaskTitle: t.TaskTitle,
                TaskStatus: t.TaskStatus,
                TaskPriority: t.TaskPriority,
                TaskDueDate: t.TaskDueDate,
                AssignedTo: t.AssignedTo
            ));

        var memberReports = tasksData.GroupByMember(teamData.Members, userDisplayNames);

        return new TeamDetailReportDto
        {
            TeamId = teamData.TeamId,
            TeamName = teamData.TeamName,
            ProjectId = teamData.ProjectId,
            ProjectName = teamData.ProjectName,
            Members = memberReports
        };
    }
}
