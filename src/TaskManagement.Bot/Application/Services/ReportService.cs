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

        var teamMembers = await _context.TeamMembers
            .Include(tm => tm.Team)
                .ThenInclude(t => t.Project)
            .Include(tm => tm.Team)
                .ThenInclude(t => t.Tasks)
            .Where(tm => tm.Username == userId && tm.Status == "Accepted" && !tm.IsDeleted)
            .ToListAsync();

        _logger.LogInformation("[REPORT_ME] Found {Count} teams", teamMembers.Count);

        var projects = BuildProjectTaskGroups(teamMembers, userId);
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

        var projects = await _context.Projects
            .Include(p => p.Teams)
                .ThenInclude(t => t.Tasks)
            .Where(p => p.CreatedBy == pmUserId && !p.IsDeleted)
            .ToListAsync();

        _logger.LogInformation("[REPORT_PM] Found {Count} projects", projects.Count);

        return new PMProjectListDto
        {
            PMUserId = pmUserId,
            Projects = projects.Select(p => new ProjectSummaryDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                TeamCount = p.Teams.Count,
                TotalTasks = p.Teams.SelectMany(t => t.Tasks).Count(t => !t.IsDeleted)
            }).ToList()
        };
    }

    public async Task<List<TeamSummaryDto>> GetTeamsByProjectAsync(int projectId)
    {
        _logger.LogInformation("[REPORT_TEAMS] Getting teams for project: {ProjectId}", projectId);

        var teams = await _context.Teams
            .Include(t => t.Members)
            .Include(t => t.Tasks)
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync();

        return teams.Select(t => new TeamSummaryDto
        {
            TeamId = t.Id,
            TeamName = t.Name,
            MemberCount = t.Members.Count(m => m.Status == "Accepted" && !m.IsDeleted),
            TotalTasks = t.Tasks.Count(task => !task.IsDeleted)
        }).ToList();
    }

    public async Task<TeamDetailReportDto> GetTeamDetailReportAsync(int teamId)
    {
        _logger.LogInformation("[REPORT_TEAM_DETAIL] Getting detail for team: {TeamId}", teamId);

        var team = await _context.Teams
            .Include(t => t.Project)
            .Include(t => t.Members)
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Id == teamId && !t.IsDeleted);

        if (team == null)
        {
            throw new KeyNotFoundException($"Team {teamId} not found");
        }

        var members = team.Members
            .Where(m => m.Status == "Accepted" && !m.IsDeleted)
            .ToList();

        var memberReports = new List<MemberTaskReportDto>();

        foreach (var member in members)
        {
            var tasks = team.Tasks
                .Where(t => t.AssignedTo == member.Username && !t.IsDeleted)
                .OrderBy(t => t.DueDate)
                .ToList();

            var completedCount = tasks.Count(t => t.Status == ETaskStatus.Completed);
            var username = await _userService.GetDisplayNameAsync(member.Username);

            memberReports.Add(new MemberTaskReportDto
            {
                UserId = member.Username,
                Username = username,
                Tasks = tasks.Select(MapToTaskSummary).ToList(),
                TotalTasks = tasks.Count,
                CompletedTasks = completedCount,
                CompletionRate = CalculateCompletionRate(completedCount, tasks.Count)
            });
        }

        return new TeamDetailReportDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            ProjectId = team.ProjectId,
            ProjectName = team.Project.Name,
            Members = memberReports
        };
    }

    private List<ProjectTaskGroupDto> BuildProjectTaskGroups(
        List<Infrastructure.Entities.TeamMember> teamMembers, 
        string userId)
    {
        var projects = new List<ProjectTaskGroupDto>();
        var projectGroups = teamMembers
            .GroupBy(tm => new { tm.Team.ProjectId, tm.Team.Project.Name })
            .ToList();

        foreach (var projectGroup in projectGroups)
        {
            var projectDto = new ProjectTaskGroupDto
            {
                ProjectId = projectGroup.Key.ProjectId,
                ProjectName = projectGroup.Key.Name,
                Teams = new List<TeamTaskGroupDto>()
            };

            foreach (var teamMember in projectGroup)
            {
                var team = teamMember.Team;
                var tasks = team.Tasks
                    .Where(t => t.AssignedTo == userId && !t.IsDeleted)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                var completedCount = tasks.Count(t => t.Status == ETaskStatus.Completed);

                projectDto.Teams.Add(new TeamTaskGroupDto
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    Tasks = tasks.Select(MapToTaskSummary).ToList(),
                    TotalTasks = tasks.Count,
                    CompletedTasks = completedCount,
                    CompletionRate = CalculateCompletionRate(completedCount, tasks.Count)
                });
            }

            projects.Add(projectDto);
        }

        return projects;
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
