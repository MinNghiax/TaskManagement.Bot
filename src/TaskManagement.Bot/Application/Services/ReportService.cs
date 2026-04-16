using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public class ReportService : IReportService
{
    private readonly TaskManagementDbContext _context;

    public ReportService(TaskManagementDbContext context)
    {
        _context = context;
    }

    private IQueryable<TaskItem> BuildTaskQuery(
        string? clanId,
        string? channelId)
    {
        var query = _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(clanId))
        {
            query = query.Where(t => t.Clans.Any(c => c.ClanId == clanId));
        }
        if (!string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t => t.Channels.Any(c => c.ChannelId == channelId));
        }
        return query;
    }

    public async Task<PersonalReportDto> GetPersonalReportAsync(
        string userId,
        string? clanId = null,
        string? channelId = null)
    {
        var now = DateTime.UtcNow;

        var tasks = await BuildTaskQuery(clanId, channelId)
            .Where(t => t.AssignedTo == userId)
            .ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == ETaskStatus.Completed);

        return new PersonalReportDto
        {
            UserId = userId,
            TotalTasks = total,
            ToDoTasks = tasks.Count(t => t.Status == ETaskStatus.ToDo),
            DoingTasks = tasks.Count(t => t.Status == ETaskStatus.Doing),
            ReviewTasks = tasks.Count(t => t.Status == ETaskStatus.Review),
            //PausedTasks = tasks.Count(t => t.Status == ETaskStatus.Paused),
            LateTasks = tasks.Count(t => t.Status == ETaskStatus.Late),
            CompletedTasks = completed,
            CompletionRate = total == 0 ? 0 : (double)completed / total * 100,
            OverdueDays = tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != ETaskStatus.Completed)
                .Sum(t => (now - t.DueDate!.Value).Days),

            Tasks = tasks.Select(t => new TaskSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                DueDate = t.DueDate,
                Priority = t.Priority,
                ClanId = t.Clans.FirstOrDefault()?.ClanId,
                ChannelId = t.Channels.FirstOrDefault()?.ChannelId,
                OverdueDays = t.DueDate.HasValue && t.DueDate.Value < now
                    ? (now - t.DueDate.Value).Days
                    : 0
            }).ToList()
        };
    }

    public async Task<TeamReportDto> GetTeamReportAsync(
        string? clanId = null,
        string? channelId = null)
    {
        var task = await BuildTaskQuery(clanId, channelId).ToListAsync();

        var members = task.Select(t => t.AssignedTo).Distinct();

        var memberReports = members.Select(member =>
        {
            var userTasks = task.Where(t => t.AssignedTo == member).ToList();

            var total = userTasks.Count;
            var completed = userTasks.Count(t => t.Status == ETaskStatus.Completed);

            return new MemberReportDto
            {
                MemberId = member,
                TotalTasks = total,
                ToDoTasks = userTasks.Count(t => t.Status == ETaskStatus.ToDo),
                DoingTasks = userTasks.Count(t => t.Status == ETaskStatus.Doing),
                ReviewTasks = userTasks.Count(t => t.Status == ETaskStatus.Review),
                //PausedTasks = userTasks.Count(t => t.Status == ETaskStatus.Paused),
                LateTasks = userTasks.Count(t => t.Status == ETaskStatus.Late),
                CompletedTasks = completed,
                CompletionRate = total == 0 ? 0 : (double)completed / total * 100
            };
        }).ToList();

        var totalTasks = task.Count;
        var completedTasks = task.Count(t => t.Status == ETaskStatus.Completed);

        return new TeamReportDto
        {
            TotalMembers = memberReports.Count,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            TotalOverdueTasks = task.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != ETaskStatus.Completed),
            TeamCompletionRate = totalTasks == 0 ? 0 : (double)completedTasks / totalTasks * 100,
            MemberReports = memberReports
        };
    }

    public async Task<StatisticsReportDto> GetStatisticsReportAsync(
        ETimeRange timeRange,
        string? clanId = null,
        string? channelId = null)
    {
        var (start, end) = GetTimeRange(timeRange);

        var tasks = await BuildTaskQuery(clanId, channelId)
                .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                .ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == ETaskStatus.Completed);

        return new StatisticsReportDto
        {
            TimeRange = timeRange,
            StartDate = start,
            EndDate = end,
            TaskCreated = total,
            TaskCompleted = completed,
            TaskInProgress = tasks.Count(t => t.Status == ETaskStatus.Doing),
            TaskPending = tasks.Count(t => t.Status == ETaskStatus.ToDo),
            OverdueTasks = tasks.Count(t =>
                t.DueDate.HasValue &&
                t.DueDate < DateTime.UtcNow &&
                t.Status != ETaskStatus.Completed),

            CompletionRate = total == 0 ? 0 : (double)completed / total * 100
        };
    }

    public async Task<List<DetailedTaskReportDto>> GetOverdueTasksAsync(
            string? clanId = null,
            string? channelId = null)
    {
        var now = DateTime.UtcNow;

        return await BuildTaskQuery(clanId, channelId)
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate < now &&
                        t.Status != ETaskStatus.Completed)
            .Select(t => new DetailedTaskReportDto
            {
                Id = t.Id,
                Title = t.Title,
                AssignedTo = t.AssignedTo,
                CreatedBy = t.CreatedBy,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                OverdueDays = (now - t.DueDate!.Value).Days,
                TotalDays = t.DueDate.HasValue
                    ? (t.DueDate.Value - t.CreatedAt).Days
                    : 0,
                ClanId = t.Clans.FirstOrDefault()!.ClanId,
                ChannelId = t.Channels.FirstOrDefault()!.ChannelId
            })
            .ToListAsync();
    }

    public async Task<List<DetailedTaskReportDto>> GetProgressReportAsync(
            string? userId = null,
            string? clanId = null,
            string? channelId = null)
    {
        var query = BuildTaskQuery(clanId, channelId);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(t => t.AssignedTo == userId);

        return await query
            .Select(t => new DetailedTaskReportDto
            {
                Id = t.Id,
                Title = t.Title,
                AssignedTo = t.AssignedTo,
                CreatedBy = t.CreatedBy,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                TotalDays = t.DueDate.HasValue
                    ? (t.DueDate.Value - t.CreatedAt).Days
                    : 0,
                ClanId = t.Clans.FirstOrDefault()!.ClanId,
                ChannelId = t.Channels.FirstOrDefault()!.ChannelId
            })
            .ToListAsync();
    }

    private (DateTime start, DateTime end) GetTimeRange(ETimeRange range)
    {
        var now = DateTime.UtcNow;

        return range switch
        {
            ETimeRange.Today => (now.Date, now),
            ETimeRange.Week => (now.AddDays(-7), now),
            ETimeRange.Month => (now.AddMonths(-1), now),
            _ => (now.Date, now)
        };
    }
}
