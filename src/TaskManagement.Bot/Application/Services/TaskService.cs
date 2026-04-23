using Mezon.Sdk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services.Reminders;
using ETaskStatus = TaskManagement.Bot.Infrastructure.Enums.ETaskStatus;
namespace TaskManagement.Bot.Application.Services;
public class TaskService : ITaskService
{
    private readonly TaskManagementDbContext _context;
    private readonly MezonClient? _client;
    private readonly TimeZoneInfo _timeZone;

    public TaskService(TaskManagementDbContext context, MezonClient? client = null, IConfiguration? configuration = null)
    {
        _context = context;
        _client = client;
        _timeZone = configuration is null
            ? TimeZoneInfo.Utc
            : ReminderSchedulerConfiguration.CreateTimeZone(configuration);
    }

    private TaskDto MapToDto(TaskItem task)
    {
        var clanInfo = task.Clans.FirstOrDefault();
        var clanId = clanInfo?.ClanId;

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedTo = task.AssignedTo,
            CreatedBy = task.CreatedBy,
            DueDate = task.DueDate,
            Status = task.Status,
            ReviewStartedAt = task.ReviewStartedAt,
            Priority = task.Priority,
            TeamId = task.TeamId,
            ClanIds = task.Clans.Select(c => c.ClanId).ToList(),
            ChannelIds = task.Channels.Select(c => c.ChannelId).ToList(),
            ReminderRules = MapReminderRules(task.Reminders),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
    private static List<CreateReminderRuleDto> MapReminderRules(IEnumerable<Reminder> reminders)
    {
        return reminders
            .Select(r => r.ReminderRule)
            .Where(rule =>
                rule?.TriggerType != EReminderTriggerType.OnDeadline &&
                rule?.IntervalUnit is not null &&
                rule.Value > 0)
            .Select(rule => new CreateReminderRuleDto
            {
                TriggerType = rule!.TriggerType,
                IntervalUnit = rule.IntervalUnit!.Value,
                Value = rule.Value,
                IsRepeat = rule.IsRepeat
            })
            .ToList();
    }
    public async Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.AssignedTo))
            return null;
        var task = new TaskItem
        {
            Title = dto.Title!,
            Description = dto.Description,
            AssignedTo = dto.AssignedTo!,
            CreatedBy = dto.CreatedBy ?? "system",
            DueDate = dto.DueDate,
            Status = ETaskStatus.ToDo,
            Priority = dto.Priority,
            TeamId = dto.TeamId
        };
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync(ct);
        if (dto.ClanIds != null)
        {
            _context.TaskClans.AddRange(
                dto.ClanIds.Select(c => new TaskClan
                {
                    ClanId = c,
                    TaskItemId = task.Id
                })
            );
        }
        if (dto.ChannelIds != null)
        {
            _context.TaskChannels.AddRange(
                dto.ChannelIds.Select(c => new TaskChannel
                {
                    ChannelId = c,
                    TaskItemId = task.Id
                })
            );
        }
        var reminders = ReminderScheduleBuilder.BuildTaskReminderEntities(task, dto.ReminderRules, _timeZone);
        if (reminders.Count > 0)
            _context.Reminders.AddRange(reminders);

        await _context.SaveChangesAsync(ct);
        return await GetByIdAsync(task.Id, ct);
    }
    public async Task<List<TaskDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return tasks.Select(MapToDto).ToList();
    }
    public async Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken ct = default)
    {
        var task = await _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
        return task == null ? null : MapToDto(task);
    }
    public async Task<List<TaskDto>> GetByAssigneeAsync(string assignee, string? channelId, CancellationToken ct = default)
    {
        var query = _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => t.AssignedTo == assignee && !t.IsDeleted);
        if (!string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t => t.Clans.Any(c => c.ClanId == channelId));
        }
        var tasks = await query.ToListAsync(ct);
        return tasks.Select(MapToDto).ToList();
    }
    public async Task<List<TaskDto>> GetByCreatorAsync(string username, string? channelId, CancellationToken ct)
    {
        var query = _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => t.CreatedBy == username && !t.IsDeleted);
        if (!string.IsNullOrEmpty(channelId))
        {
            query = query.Where(t => t.Channels.Any(c => c.ChannelId == channelId));
        }
        var tasks = await query.ToListAsync(ct);
        return tasks.Select(MapToDto).ToList();
    }
    public async Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => t.Status == status && !t.IsDeleted)
            .ToListAsync(ct);
        return tasks.Select(MapToDto).ToList();
    }
    public async Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken ct = default)
    {
        var id = taskId;

        var task = await _context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task == null || task.IsDeleted) return;

        var now = DateTime.UtcNow;
        ApplyStatusTransition(task, newStatus, now);
        task.UpdatedAt = now;
        await SyncCustomRemindersAsync(task, resetSchedule: false, now, ct);
        ReminderScheduleBuilder.SyncOnDeadlineReminder(
            task,
            reminder => _context.Reminders.Add(reminder),
            resetSchedule: false,
            nowUtc: now,
            timeZone: _timeZone);

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken ct = default)
    {
        var task = await _context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
        if (task == null) return;

        var dueDateChanged = task.DueDate != newDueDate;
        var now = DateTime.UtcNow;
        task.DueDate = newDueDate;
        task.UpdatedAt = now;
        await SyncCustomRemindersAsync(task, resetSchedule: dueDateChanged, now, ct);
        ReminderScheduleBuilder.SyncOnDeadlineReminder(
            task,
            reminder => _context.Reminders.Add(reminder),
            resetSchedule: dueDateChanged,
            nowUtc: now,
            timeZone: _timeZone);

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int taskId, CancellationToken ct = default)
    {
        var id = taskId;

        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task == null || task.IsDeleted) return;
        task.IsDeleted = true;
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }
    public async Task<List<TaskDto>> GetTasksByChannelAsync(string channelId)
    {
        var tasks = await _context.TaskItems
            .Include(t => t.Clans)
            .Where(t => t.Clans.Any(c => c.ClanId == channelId))
            .ToListAsync();
        return tasks.Select(MapToDto).ToList();
    }
    public async Task UpdateAsync(int taskId, UpdateTaskDto updateDto, CancellationToken ct = default)
    {
        var task = await _context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
        if (task == null) throw new Exception("Task not found");
        if (!string.IsNullOrWhiteSpace(updateDto.Title))
            task.Title = updateDto.Title;
        if (updateDto.Description != null)
            task.Description = updateDto.Description;
        if (updateDto.Priority.HasValue)
            task.Priority = updateDto.Priority.Value;
        var dueDateChanged = updateDto.DueDate.HasValue && task.DueDate != updateDto.DueDate.Value;
        var assigneeChanged = !string.IsNullOrWhiteSpace(updateDto.AssignedTo) && task.AssignedTo != updateDto.AssignedTo;
        var statusChanged = updateDto.Status.HasValue && task.Status != updateDto.Status.Value;

        var now = DateTime.UtcNow;

        if (updateDto.Status.HasValue)
            ApplyStatusTransition(task, updateDto.Status.Value, now);

        if (updateDto.DueDate.HasValue)
            task.DueDate = updateDto.DueDate.Value;
        if (!string.IsNullOrWhiteSpace(updateDto.AssignedTo))
            task.AssignedTo = updateDto.AssignedTo;
        if (updateDto.ReminderRules is not null)
            await ReplaceTaskRemindersAsync(task, updateDto.ReminderRules, ct);
        else if (dueDateChanged || assigneeChanged || statusChanged)
            await SyncCustomRemindersAsync(task, resetSchedule: dueDateChanged, now, ct);

        task.UpdatedAt = now;
        if (dueDateChanged || assigneeChanged || statusChanged || updateDto.ReminderRules is not null)
            ReminderScheduleBuilder.SyncOnDeadlineReminder(
                task,
                reminder => _context.Reminders.Add(reminder),
                resetSchedule: dueDateChanged,
                nowUtc: now,
                timeZone: _timeZone);

        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<TaskDto>> GetTasksByTeamAsync(int teamId, CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => t.TeamId == teamId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return tasks.Select(MapToDto).ToList();
    }
    private async Task ReplaceTaskRemindersAsync(
        TaskItem task,
        IReadOnlyCollection<CreateReminderRuleDto> reminderRules,
        CancellationToken ct)
    {
        var customReminders = task.Reminders
            .Where(r => r.ReminderRule?.TriggerType != EReminderTriggerType.OnDeadline)
            .ToList();

        if (customReminders.Count > 0)
        {
            var oldRuleIds = customReminders
                .Select(r => r.ReminderRuleId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            _context.Reminders.RemoveRange(customReminders);

            if (oldRuleIds.Count > 0)
            {
                var sharedRuleIds = await _context.Reminders
                    .Where(r => r.TaskId != task.Id && oldRuleIds.Contains(r.ReminderRuleId))
                    .Select(r => r.ReminderRuleId)
                    .Distinct()
                    .ToListAsync(ct);

                var rulesToRemove = customReminders
                    .Select(r => r.ReminderRule)
                    .Where(rule => rule is not null && !sharedRuleIds.Contains(rule.Id))
                    .DistinctBy(rule => rule!.Id)
                    .ToList();
                _context.ReminderRules.RemoveRange(rulesToRemove!);
            }
        }

        if (!ReminderScheduleBuilder.IsTaskActive(task))
            return;

        var newReminders = ReminderScheduleBuilder.BuildCustomReminderEntities(task, reminderRules, _timeZone);
        if (newReminders.Count > 0)
            _context.Reminders.AddRange(newReminders);
    }

    private async Task SyncCustomRemindersAsync(
        TaskItem task,
        bool resetSchedule,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var customReminders = task.Reminders
            .Where(r => r.ReminderRule?.TriggerType != EReminderTriggerType.OnDeadline)
            .ToList();

        if (customReminders.Count == 0)
            return;

        if (resetSchedule)
        {
            var reminderRules = ReminderScheduleBuilder.IsTaskActive(task)
                ? MapReminderRules(customReminders)
                : [];
            await ReplaceTaskRemindersAsync(task, reminderRules, ct);
            return;
        }

        foreach (var reminder in customReminders)
        {
            if (ReminderScheduleBuilder.IsTaskActive(task))
            {
                if (reminder.Status == EReminderStatus.Cancelled
                    && task.Status == ETaskStatus.Doing
                    && reminder.ReminderRule?.TaskStatus is ETaskStatus.Review or ETaskStatus.Completed)
                {
                    reminder.Status = EReminderStatus.Pending;

                    if (ReminderScheduleBuilder.IsRepeatRule(reminder.ReminderRule))
                        ReminderScheduleBuilder.ApplyNextSchedule(reminder, nowUtc);
                }

                if (reminder.Status != EReminderStatus.Pending)
                    continue;

                reminder.TargetUserId = task.AssignedTo;
                reminder.StateSnapshot = task.Status;
            }
            else
            {
                if (reminder.Status != EReminderStatus.Pending)
                    continue;

                reminder.Status = EReminderStatus.Cancelled;
                reminder.NextTriggerAt = null;
            }

            reminder.UpdatedAt = nowUtc;

            if (reminder.ReminderRule is not null)
            {
                reminder.ReminderRule.TaskStatus = task.Status;
                reminder.ReminderRule.UpdatedAt = nowUtc;
            }
        }
    }

    private static void ApplyStatusTransition(TaskItem task, ETaskStatus newStatus, DateTime changedAtUtc)
    {
        var previousStatus = task.Status;
        task.Status = newStatus;

        if (newStatus == ETaskStatus.Review)
        {
            if (previousStatus != ETaskStatus.Review || !task.ReviewStartedAt.HasValue)
                task.ReviewStartedAt = changedAtUtc;

            return;
        }

        if (previousStatus == ETaskStatus.Review)
            task.ReviewStartedAt = null;
    }

    private string GetDisplayName(string userId, string clanId)
    {
        var user = _client?.Clans.Get(clanId)?.Users.Get(userId);

        return user?.DisplayName
            ?? user?.ClanNick
            ?? user?.Username
            ?? $"User-{userId.Substring(0, 4)}";
    }

    public async Task<List<TaskDto>> GetByAssigneeAndTeamAsync(string assignee, int teamId, CancellationToken ct)
    {
        return await _context.TaskItems
            .Where(t =>
                t.AssignedTo == assignee &&
                t.TeamId == teamId &&
                !t.IsDeleted
            )
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                AssignedTo = t.AssignedTo,
                CreatedBy = t.CreatedBy,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                TeamId = t.TeamId,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<List<TaskDto>> GetByAssigneeAndTeamsAsync(
    string userId,
    List<int> teamIds,
    CancellationToken ct)
    {
        var tasks = await _context.TaskItems
            .Where(t =>
                t.AssignedTo == userId &&
                t.TeamId.HasValue &&
                teamIds.Contains(t.TeamId.Value) &&
                !t.IsDeleted
            )
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }
}
