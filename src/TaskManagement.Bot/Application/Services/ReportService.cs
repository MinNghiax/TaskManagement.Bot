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



    public async Task<TimeBasedReportDto> GetTimeBasedReportAsync(string pmUserId, TimeRangeFilter timeRange)
    {
        _logger.LogInformation("[REPORT_TIME] Getting {TimeRange} report for PM: {PMUserId}", timeRange, pmUserId);

        var (startDate, endDate) = GetDateRange(timeRange);

        var projects = await _context.Projects
            .Include(p => p.Teams)
                .ThenInclude(t => t.Tasks)
            .Where(p => p.CreatedBy == pmUserId && !p.IsDeleted)
            .ToListAsync();

        var tasks = projects
            .SelectMany(p => p.Teams)
            .SelectMany(t => t.Tasks)
            .Where(t => t.DueDate.HasValue 
                && t.DueDate.Value >= startDate 
                && t.DueDate.Value <= endDate
                && !t.IsDeleted)
            .ToList();

        var memberGroups = tasks.GroupBy(t => t.AssignedTo).ToList();
        var memberReports = new List<MemberTimeReportDto>();
        
        foreach (var group in memberGroups)
        {
            var username = await _userService.GetDisplayNameAsync(group.Key);
            
            memberReports.Add(new MemberTimeReportDto
            {
                UserId = group.Key,
                Username = username,
                Tasks = group.Select(MapToTaskSummary).OrderBy(t => t.DueDate).ToList(),
                TotalTasks = group.Count()
            });
        }

        return new TimeBasedReportDto
        {
            TimeRange = timeRange,
            StartDate = startDate,
            EndDate = endDate,
            Members = memberReports.OrderByDescending(m => m.TotalTasks).ToList()
        };
    }

    public async Task<UserReportByPMDto> GetUserReportByPMAsync(string targetUserId, string pmUserId)
    {
        _logger.LogInformation("[REPORT_USER] PM {PMUserId} requesting report for {TargetUserId}", pmUserId, targetUserId);

        var allTasks = await _context.TaskItems
            .Include(t => t.Team)
                .ThenInclude(t => t!.Project)
            .Where(t => t.AssignedTo == targetUserId && t.Team != null && !t.IsDeleted)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        var pmTasks = allTasks.Where(t => t.Team!.Project.CreatedBy == pmUserId).ToList();

        _logger.LogInformation("[REPORT_USER] Found {Count} tasks in PM's projects", pmTasks.Count);

        if (pmTasks.Count == 0)
        {
            var userExists = await _context.TeamMembers.AnyAsync(tm => tm.Username == targetUserId && !tm.IsDeleted);

            if (!userExists && allTasks.Count == 0)
            {
                throw new KeyNotFoundException($"User không tồn tại trong hệ thống");
            }

            throw new UnauthorizedAccessException($"Bạn không có quyền xem báo cáo của user này");
        }

        var completedCount = pmTasks.Count(t => t.Status == ETaskStatus.Completed);
        var statusBreakdown = pmTasks.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
        var username = await _userService.GetDisplayNameAsync(targetUserId);

        return new UserReportByPMDto
        {
            UserId = targetUserId,
            Username = username,
            Tasks = pmTasks.Select(MapToTaskSummary).ToList(),
            TotalTasks = pmTasks.Count,
            CompletedTasks = completedCount,
            CompletionRate = CalculateCompletionRate(completedCount, pmTasks.Count),
            StatusBreakdown = statusBreakdown
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

    private static (DateTime startDate, DateTime endDate) GetDateRange(TimeRangeFilter timeRange)
    {
        var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

        return timeRange switch
        {
            TimeRangeFilter.Today => (vnNow.Date, vnNow.Date.AddDays(1).AddSeconds(-1)),
            TimeRangeFilter.Week => (vnNow.Date.AddDays(-(int)vnNow.DayOfWeek), vnNow.Date.AddDays(7 - (int)vnNow.DayOfWeek).AddSeconds(-1)),
            TimeRangeFilter.Month => (new DateTime(vnNow.Year, vnNow.Month, 1), new DateTime(vnNow.Year, vnNow.Month, 1).AddMonths(1).AddSeconds(-1)),
            _ => (vnNow.Date, vnNow.Date.AddDays(1).AddSeconds(-1))
        };
    }
}
