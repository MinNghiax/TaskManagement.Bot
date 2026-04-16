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

        if (!string.IsNullOrEmpty(clanId) && !string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t =>
                t.Clans.Any(c => c.ClanId == clanId) &&
                t.Channels.Any(c => c.ChannelId == channelId)
            );
        }
        else if (!string.IsNullOrEmpty(clanId))
        {
            query = query.Where(t => t.Clans.Any(c => c.ClanId == clanId));
        }
        else if (!string.IsNullOrEmpty(channelId))
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

        var query = _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => !t.IsDeleted && t.AssignedTo == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(clanId) && !string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t =>
                t.Clans.Any(c => c.ClanId == clanId) &&
                t.Channels.Any(c => c.ChannelId == channelId));
        }
        else if (!string.IsNullOrEmpty(clanId))
        {
            query = query.Where(t => t.Clans.Any(c => c.ClanId == clanId));
        }
        else if (!string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t => t.Channels.Any(c => c.ChannelId == channelId));
        }

        var tasks = await query.ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == ETaskStatus.Completed);

        return new PersonalReportDto
        {
            UserId = userId,
            TotalTasks = total,
            ToDoTasks = tasks.Count(t => t.Status == ETaskStatus.ToDo),
            DoingTasks = tasks.Count(t => t.Status == ETaskStatus.Doing),
            ReviewTasks = tasks.Count(t => t.Status == ETaskStatus.Review),
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
        var now = DateTime.UtcNow;
        var openStatuses = new[] { ETaskStatus.ToDo, ETaskStatus.Doing, ETaskStatus.Review, ETaskStatus.Late };

        var scopedTasks = await BuildTaskQuery(clanId, channelId)
            .Include(t => t.Team)
                .ThenInclude(t => t!.Project)
            .ToListAsync();

        var createdInPeriod = scopedTasks
            .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
            .ToList();

        var completedInPeriod = scopedTasks
            .Where(t => t.Status == ETaskStatus.Completed)
            .Where(t =>
            {
                var completedAt = t.UpdatedAt ?? t.CreatedAt;
                return completedAt >= start && completedAt <= end;
            })
            .ToList();

        var createdAndCompleted = createdInPeriod.Count(t => t.Status == ETaskStatus.Completed);
        var totalTasks = scopedTasks.Count;
        var todoTasks = scopedTasks.Count(t => t.Status == ETaskStatus.ToDo);
        var doingTasks = scopedTasks.Count(t => t.Status == ETaskStatus.Doing);
        var reviewTasks = scopedTasks.Count(t => t.Status == ETaskStatus.Review);
        var lateTasks = scopedTasks.Count(t => t.Status == ETaskStatus.Late);
        var completedTasksOverall = scopedTasks.Count(t => t.Status == ETaskStatus.Completed);
        var canceledTasks = scopedTasks.Count(t => t.Status == ETaskStatus.Canceled);
        var openTasks = scopedTasks.Count(t => openStatuses.Contains(t.Status));
        var overdueTasks = scopedTasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            t.Status != ETaskStatus.Completed &&
            t.Status != ETaskStatus.Canceled);

        var projectStats = scopedTasks
            .GroupBy(t => new
            {
                ProjectId = t.Team?.ProjectId,
                ProjectName = t.Team?.Project?.Name
            })
            .Select(group =>
            {
                var projectTasks = group.ToList();
                var projectCreated = projectTasks
                    .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                    .ToList();

                var projectCompleted = projectTasks
                    .Where(t => t.Status == ETaskStatus.Completed)
                    .Where(t =>
                    {
                        var completedAt = t.UpdatedAt ?? t.CreatedAt;
                        return completedAt >= start && completedAt <= end;
                    })
                    .ToList();

                var projectCreatedAndCompleted = projectCreated.Count(t => t.Status == ETaskStatus.Completed);
                var projectTodoTasks = projectTasks.Count(t => t.Status == ETaskStatus.ToDo);
                var projectDoingTasks = projectTasks.Count(t => t.Status == ETaskStatus.Doing);
                var projectReviewTasks = projectTasks.Count(t => t.Status == ETaskStatus.Review);
                var projectLateTasks = projectTasks.Count(t => t.Status == ETaskStatus.Late);
                var projectCompletedOverall = projectTasks.Count(t => t.Status == ETaskStatus.Completed);
                var projectCanceledTasks = projectTasks.Count(t => t.Status == ETaskStatus.Canceled);
                var projectOpenTasks = projectTasks.Count(t => openStatuses.Contains(t.Status));
                var projectOverdueTasks = projectTasks.Count(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value < now &&
                    t.Status != ETaskStatus.Completed &&
                    t.Status != ETaskStatus.Canceled);

                return new ProjectStatisticsDto
                {
                    ProjectId = group.Key.ProjectId,
                    ProjectName = string.IsNullOrWhiteSpace(group.Key.ProjectName)
                        ? "No Project"
                        : group.Key.ProjectName,
                    TeamCount = projectTasks
                        .Where(t => t.TeamId.HasValue)
                        .Select(t => t.TeamId!.Value)
                        .Distinct()
                        .Count(),
                    TotalTasks = projectTasks.Count,
                    CreatedTasks = projectCreated.Count,
                    CompletedTasks = projectCompleted.Count,
                    OpenTasks = projectOpenTasks,
                    DoingTasks = projectDoingTasks,
                    ReviewTasks = projectReviewTasks,
                    LateTasks = projectLateTasks,
                    CompletedTasksOverall = projectCompletedOverall,
                    CanceledTasks = projectCanceledTasks,
                    InProgressTasks = projectDoingTasks + projectReviewTasks,
                    PendingTasks = projectTodoTasks,
                    OverdueTasks = projectOverdueTasks,
                    CompletionRate = projectTasks.Count == 0
                        ? 0
                        : (double)projectCompletedOverall / projectTasks.Count * 100,
                    PeriodCompletionRate = projectCreated.Count == 0
                        ? 0
                        : (double)projectCreatedAndCompleted / projectCreated.Count * 100
                };
            })
            .OrderByDescending(x => x.CreatedTasks + x.CompletedTasks)
            .ThenByDescending(x => x.TotalTasks)
            .ThenBy(x => x.ProjectName)
            .ToList();

        return new StatisticsReportDto
        {
            ClanId = clanId,
            ChannelId = channelId,
            TimeRange = timeRange,
            StartDate = start,
            EndDate = end,
            TaskCreated = createdInPeriod.Count,
            TaskCompleted = completedInPeriod.Count,
            TotalTasks = totalTasks,
            OpenTasks = openTasks,
            TaskPending = todoTasks,
            TaskDoing = doingTasks,
            TaskReview = reviewTasks,
            TaskLate = lateTasks,
            TaskCompletedOverall = completedTasksOverall,
            TaskCanceled = canceledTasks,
            TaskInProgress = doingTasks + reviewTasks,
            OverdueTasks = overdueTasks,
            CompletionRate = totalTasks == 0
                ? 0
                : (double)completedTasksOverall / totalTasks * 100,
            PeriodCompletionRate = createdInPeriod.Count == 0
                ? 0
                : (double)createdAndCompleted / createdInPeriod.Count * 100,
            TotalProjects = projectStats.Count(x => x.ProjectId.HasValue),
            ActiveProjects = projectStats.Count(x =>
                x.ProjectId.HasValue &&
                (x.OpenTasks > 0 || x.CreatedTasks > 0 || x.CompletedTasks > 0)),
            Projects = projectStats
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

    private static readonly TimeZoneInfo VN_TZ =
    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private (DateTime start, DateTime end) GetTimeRange(ETimeRange range)
    {
        var nowUtc = DateTime.UtcNow;
        var nowVN = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, VN_TZ);

        DateTime startVN = range switch
        {
            ETimeRange.Today => nowVN.Date,
            ETimeRange.Week => nowVN.Date.AddDays(-((7 + (int)nowVN.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
            ETimeRange.Month => new DateTime(nowVN.Year, nowVN.Month, 1),
            _ => nowVN.Date
        };

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startVN, VN_TZ);
        return (startUtc, nowUtc);
    }

    // ===== NEW COMPREHENSIVE METHODS =====

    public async Task<ComprehensiveTaskReportDto> GetComprehensiveTaskReportAsync(int taskId)
    {
        var task = await _context.TaskItems
            .Include(t => t.Team).ThenInclude(tm => tm!.Project)
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .Include(t => t.Complains)
            .FirstOrDefaultAsync(t => !t.IsDeleted && t.Id == taskId);

        if (task == null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found");

        var now = DateTime.UtcNow;

        // Calculate time metrics
        var daysOverdue = task.DueDate.HasValue && task.DueDate < now
            ? (int)(now - task.DueDate.Value).TotalDays
            : 0;

        var daysUntilDue = task.DueDate.HasValue
            ? (int)(task.DueDate.Value - now).TotalDays
            : int.MaxValue;

        var totalDaysAllocated = task.DueDate.HasValue
            ? (int)(task.DueDate.Value - task.CreatedAt).TotalDays
            : 0;

        var progressPercentage = totalDaysAllocated > 0
            ? Math.Min(100, (double)(now - task.CreatedAt).TotalDays / totalDaysAllocated * 100)
            : 0;

        // Determine health status
        var isCompleted = task.Status == ETaskStatus.Completed;
        var isCanceled = task.Status == ETaskStatus.Canceled;
        var isOverdue = daysOverdue > 0 && !isCompleted && !isCanceled;
        var isAtRisk = !isCompleted && !isCanceled && daysUntilDue <= 3 && daysUntilDue > 0;

        var healthStatus = isCompleted || isCanceled ? "Done"
            : isOverdue ? "Overdue"
            : isAtRisk ? "At Risk"
            : "On Track";

        var timeStatusIcon = isCompleted || isCanceled ? "✅"
            : isOverdue ? "🔴"
            : isAtRisk ? "🟡"
            : "🟢";

        // Build reminder summaries
        var reminders = task.Reminders?.Select(r => new ReminderSummaryDto
        {
            Id = r.Id,
            TriggerAt = r.TriggerAt,
            NextTriggerAt = r.NextTriggerAt,
            Status = r.Status,
            ReminderRuleName = r.ReminderRule?.Name,
            TriggerType = r.ReminderRule?.TriggerType
        }).OrderBy(r => r.TriggerAt).ToList() ?? new();

        // Build complaint summaries
        var complaints = task.Complains?.Select(c => new ComplaintSummaryDto
        {
            Id = c.Id,
            UserId = c.UserId,
            Reason = c.Reason,
            Type = c.Type,
            Status = c.Status,
            NewDueDate = c.NewDueDate,
            ApprovedBy = c.ApprovedBy,
            ApprovedAt = c.ApprovedAt,
            RejectReason = c.RejectReason,
            CreatedAt = c.CreatedAt
        }).OrderByDescending(c => c.CreatedAt).ToList() ?? new();

        return new ComprehensiveTaskReportDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedTo = task.AssignedTo,
            CreatedBy = task.CreatedBy,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DaysOverdue = daysOverdue,
            DaysUntilDue = daysUntilDue,
            TotalDaysAllocated = totalDaysAllocated,
            ProgressPercentage = progressPercentage,
            TimeStatusIcon = timeStatusIcon,
            TeamId = task.TeamId,
            TeamName = task.Team?.Name,
            ProjectId = task.Team?.ProjectId,
            ProjectName = task.Team?.Project?.Name,
            ClanIds = task.Clans?.Select(c => c.ClanId).ToList() ?? new(),
            ChannelIds = task.Channels?.Select(c => c.ChannelId).ToList() ?? new(),
            TotalReminders = task.Reminders?.Count ?? 0,
            PendingReminders = task.Reminders?.Count(r => r.Status == EReminderStatus.Pending) ?? 0,
            SentReminders = task.Reminders?.Count(r => r.Status == EReminderStatus.Sent) ?? 0,
            NextReminderAt = task.Reminders?.Where(r => r.Status == EReminderStatus.Pending)
                .OrderBy(r => r.TriggerAt).FirstOrDefault()?.TriggerAt,
            Reminders = reminders,
            TotalComplaints = task.Complains?.Count ?? 0,
            PendingComplaints = task.Complains?.Count(c => c.Status == EComplainStatus.Pending) ?? 0,
            ApprovedComplaints = task.Complains?.Count(c => c.Status == EComplainStatus.Approved) ?? 0,
            RejectedComplaints = task.Complains?.Count(c => c.Status == EComplainStatus.Rejected) ?? 0,
            Complaints = complaints,
            HealthStatus = healthStatus,
            StatusIcon = task.Status switch
            {
                ETaskStatus.ToDo => "⏳",
                ETaskStatus.Doing => "🚧",
                ETaskStatus.Review => "👀",
                ETaskStatus.Late => "⚠️",
                ETaskStatus.Completed => "✅",
                ETaskStatus.Canceled => "❌",
                _ => "❓"
            },
            PriorityIcon = task.Priority switch
            {
                EPriorityLevel.Low => "🟢",
                EPriorityLevel.Medium => "🟡",
                EPriorityLevel.High => "🔴",
                EPriorityLevel.Critical => "🔥",
                _ => "⚪"
            },
            IsAtRisk = isAtRisk,
            IsOverdue = isOverdue,
            IsCompleted = isCompleted,
            IsCanceled = isCanceled
        };
    }

    public async Task<EnhancedPersonalTaskReportDto> GetEnhancedPersonalReportAsync(
        string userId,
        string? clanId = null,
        string? channelId = null)
    {
        var now = DateTime.UtcNow;

        var tasks = await _context.TaskItems
            .Include(t => t.Team).ThenInclude(tm => tm!.Project)
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Include(t => t.Reminders)
            .Include(t => t.Complains)
            .Where(t => !t.IsDeleted && t.AssignedTo == userId)
            .ToListAsync();

        // Apply filters
        if (!string.IsNullOrEmpty(clanId) && !string.IsNullOrEmpty(channelId))
            tasks = tasks.Where(t => t.Clans.Any(c => c.ClanId == clanId) && t.Channels.Any(c => c.ChannelId == channelId)).ToList();
        else if (!string.IsNullOrEmpty(clanId))
            tasks = tasks.Where(t => t.Clans.Any(c => c.ClanId == clanId)).ToList();
        else if (!string.IsNullOrEmpty(channelId))
            tasks = tasks.Where(t => t.Channels.Any(c => c.ChannelId == channelId)).ToList();

        var allTaskReports = new List<ComprehensiveTaskReportDto>();
        var overdueTasks = new List<ComprehensiveTaskReportDto>();
        var atRiskTasks = new List<ComprehensiveTaskReportDto>();

        foreach (var task in tasks)
        {
            var report = await GetComprehensiveTaskReportAsync(task.Id);
            allTaskReports.Add(report);

            if (!task.IsDeleted && task.Status != ETaskStatus.Completed && task.Status != ETaskStatus.Canceled)
            {
                if (report.IsOverdue)
                    overdueTasks.Add(report);
                else if (report.IsAtRisk)
                    atRiskTasks.Add(report);
            }
        }

        var completedCount = tasks.Count(t => t.Status == ETaskStatus.Completed);
        var totalOverdueDays = tasks
            .Where(t => t.DueDate.HasValue && t.DueDate < now && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled)
            .Sum(t => (now - t.DueDate!.Value).Days);

        // Calculate health score (0-100)
        var healthScore = tasks.Count == 0 ? 100 :
            Math.Max(0, (completedCount * 100.0 / tasks.Count) - (overdueTasks.Count * 5.0) - (atRiskTasks.Count * 3.0));

        return new EnhancedPersonalTaskReportDto
        {
            UserId = userId,
            ReportGeneratedAt = DateTime.UtcNow,
            TotalTasks = tasks.Count,
            ToDoCount = tasks.Count(t => t.Status == ETaskStatus.ToDo),
            DoingCount = tasks.Count(t => t.Status == ETaskStatus.Doing),
            ReviewCount = tasks.Count(t => t.Status == ETaskStatus.Review),
            LateCount = tasks.Count(t => t.Status == ETaskStatus.Late),
            CompletedCount = completedCount,
            CanceledCount = tasks.Count(t => t.Status == ETaskStatus.Canceled),
            CompletionRate = tasks.Count == 0 ? 0 : (completedCount * 100.0 / tasks.Count),
            TotalOverdueDays = totalOverdueDays,
            OverdueTasksCount = overdueTasks.Count,
            AtRiskTasksCount = atRiskTasks.Count,
            HealthScore = Math.Min(100, healthScore),
            TeamCount = tasks.Where(t => t.Team != null).Select(t => t.Team!.Id).Distinct().Count(),
            TeamNames = tasks.Where(t => t.Team != null).Select(t => t.Team!.Name).Distinct().ToList(),
            TotalPendingReminders = tasks.SelectMany(t => t.Reminders).Count(r => r.Status == EReminderStatus.Pending),
            TotalActiveComplaints = tasks.SelectMany(t => t.Complains).Count(c => c.Status == EComplainStatus.Pending),
            AllTasks = allTaskReports,
            OverdueTasks = overdueTasks.OrderByDescending(t => t.DaysOverdue).ToList(),
            AtRiskTasks = atRiskTasks.OrderBy(t => t.DaysUntilDue).ToList()
        };
    }

    public async Task<TeamHealthReportDto> GetTeamHealthReportAsync(int teamId)
    {
        var team = await _context.Teams
            .Include(t => t.Project)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
            throw new KeyNotFoundException($"Team with ID {teamId} not found");

        var tasks = await _context.TaskItems
            .Include(t => t.Reminders)
            .Include(t => t.Complains)
            .Where(t => !t.IsDeleted && t.TeamId == teamId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var completedTasks = tasks.Count(t => t.Status == ETaskStatus.Completed);
        var overdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate < now && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled);
        var atRiskCount = tasks.Count(t => !t.DueDate.HasValue || (t.DueDate >= now && (t.DueDate.Value - now).TotalDays <= 3 && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled));

        var memberBreakdowns = tasks.GroupBy(t => t.AssignedTo).Select(g =>
        {
            var memberTasks = g.ToList();
            var memberCompleted = memberTasks.Count(t => t.Status == ETaskStatus.Completed);
            return new MemberTaskBreakdownDto
            {
                MemberId = g.Key,
                TotalTasks = memberTasks.Count,
                CompletedTasks = memberCompleted,
                CompletionRate = memberTasks.Count == 0 ? 0 : (memberCompleted * 100.0 / memberTasks.Count),
                OverdueCount = memberTasks.Count(t => t.DueDate.HasValue && t.DueDate < now && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled),
                AtRiskCount = memberTasks.Count(t => !t.DueDate.HasValue || (t.DueDate >= now && (t.DueDate.Value - now).TotalDays <= 3 && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled)),
                HighestPriorityTask = memberTasks.Max(t => (EPriorityLevel?)t.Priority),
                CriticalTaskTitle = memberTasks.Where(t => t.Priority == EPriorityLevel.Critical && t.Status != ETaskStatus.Completed).FirstOrDefault()?.Title,
                WorkloadScore = memberTasks.Count(t => t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled)
            };
        }).ToList();

        var teamHealthStatus = overdueCount > tasks.Count * 0.2 ? "Critical" : atRiskCount > tasks.Count * 0.1 ? "At Risk" : "Healthy";

        return new TeamHealthReportDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            ProjectId = team.ProjectId,
            ProjectName = team.Project?.Name,
            ReportGeneratedAt = DateTime.UtcNow,
            TotalMembers = team.Members.Count,
            ActiveMembers = memberBreakdowns.Count,
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks,
            TeamCompletionRate = tasks.Count == 0 ? 0 : (completedTasks * 100.0 / tasks.Count),
            OverdueTasksCount = overdueCount,
            AtRiskTasksCount = atRiskCount,
            OverduePercentage = tasks.Count == 0 ? 0 : (overdueCount * 100.0 / tasks.Count),
            TeamHealthStatus = teamHealthStatus,
            MemberBreakdowns = memberBreakdowns,
            LowPriorityTasks = tasks.Count(t => t.Priority == EPriorityLevel.Low),
            MediumPriorityTasks = tasks.Count(t => t.Priority == EPriorityLevel.Medium),
            HighPriorityTasks = tasks.Count(t => t.Priority == EPriorityLevel.High),
            CriticalPriorityTasks = tasks.Count(t => t.Priority == EPriorityLevel.Critical)
        };
    }

    public async Task<TaskAnalyticsReportDto> GetTaskAnalyticsReportAsync(
        ETimeRange timeRange,
        string? clanId = null,
        string? channelId = null)
    {
        var (startUtc, endUtc) = GetTimeRange(timeRange);
        var daysDiff = (endUtc - startUtc).TotalDays;

        var tasksInPeriod = await _context.TaskItems
            .Include(t => t.Complains)
            .Where(t => !t.IsDeleted && t.CreatedAt >= startUtc && t.CreatedAt <= endUtc)
            .ToListAsync();

        // Apply filters
        if (!string.IsNullOrEmpty(clanId) && !string.IsNullOrEmpty(channelId))
            tasksInPeriod = tasksInPeriod.Where(t => t.Clans!.Any(c => c.ClanId == clanId) && t.Channels!.Any(c => c.ChannelId == channelId)).ToList();
        else if (!string.IsNullOrEmpty(clanId))
            tasksInPeriod = tasksInPeriod.Where(t => t.Clans!.Any(c => c.ClanId == clanId)).ToList();
        else if (!string.IsNullOrEmpty(channelId))
            tasksInPeriod = tasksInPeriod.Where(t => t.Channels!.Any(c => c.ChannelId == channelId)).ToList();

        var tasksCreated = tasksInPeriod.Count;
        var tasksCompleted = tasksInPeriod.Count(t => t.Status == ETaskStatus.Completed && t.UpdatedAt.HasValue && t.UpdatedAt >= startUtc);
        var completionVelocity = daysDiff > 0 ? tasksCompleted / daysDiff : 0;
        var deliveryRate = tasksCreated > 0 ? (tasksCompleted * 100.0 / tasksCreated) : 0;

        var criticalTasks = tasksInPeriod.Where(t => t.Priority == EPriorityLevel.Critical).ToList();
        var criticalCompleted = criticalTasks.Count(t => t.Status == ETaskStatus.Completed);

        var allComplaints = tasksInPeriod.SelectMany(t => t.Complains).ToList();
        var approvedComplaints = allComplaints.Count(c => c.Status == EComplainStatus.Approved);

        return new TaskAnalyticsReportDto
        {
            ReportGeneratedAt = DateTime.UtcNow,
            ReportPeriod = timeRange,
            PeriodStartDate = startUtc,
            PeriodEndDate = endUtc,
            TasksCreated = tasksCreated,
            TasksCompleted = tasksCompleted,
            CompletionVelocity = completionVelocity,
            DeliveryRate = deliveryRate,
            CriticalTasksCompleted = criticalCompleted,
            CriticalTasksPending = criticalTasks.Count(t => t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled),
            CriticalCompletionRate = criticalTasks.Count == 0 ? 0 : (criticalCompleted * 100.0 / criticalTasks.Count),
            TotalComplaints = allComplaints.Count,
            TimeExtensionRequests = allComplaints.Count(c => c.Type == EComplainType.Extend),
            CancellationRequests = allComplaints.Count(c => c.Type == EComplainType.Cancel),
            PauseRequests = allComplaints.Count(c => c.Type == EComplainType.Paused),
            ApprovalRate = allComplaints.Count == 0 ? 0 : (approvedComplaints * 100.0 / allComplaints.Count)
        };
    }

    public async Task<List<ComprehensiveTaskReportDto>> FindTasksAsync(
        string? status = null,
        string? priority = null,
        string? assignedTo = null,
        string? createdBy = null,
        bool? onlyOverdue = false,
        bool? onlyAtRisk = false)
    {
        var query = _context.TaskItems
            .Include(t => t.Team).ThenInclude(tm => tm!.Project)
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .Include(t => t.Complains)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ETaskStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<EPriorityLevel>(priority, out var priorityEnum))
            query = query.Where(t => t.Priority == priorityEnum);

        if (!string.IsNullOrEmpty(assignedTo))
            query = query.Where(t => t.AssignedTo == assignedTo);

        if (!string.IsNullOrEmpty(createdBy))
            query = query.Where(t => t.CreatedBy == createdBy);

        var now = DateTime.UtcNow;

        if (onlyOverdue == true)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate < now && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled);

        if (onlyAtRisk == true)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate >= now && (t.DueDate.Value - now).TotalDays <= 3 && t.Status != ETaskStatus.Completed && t.Status != ETaskStatus.Canceled);

        var tasks = await query.ToListAsync();
        var results = new List<ComprehensiveTaskReportDto>();

        foreach (var task in tasks)
        {
            results.Add(await GetComprehensiveTaskReportAsync(task.Id));
        }

        return results;
    }
}

