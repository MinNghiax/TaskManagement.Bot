using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using TaskManagement.Bot.Application.DTOs;
using ETaskStatus = TaskManagement.Bot.Infrastructure.Enums.ETaskStatus;
namespace TaskManagement.Bot.Application.Services;
public class TaskService : ITaskService
{
    private readonly TaskManagementDbContext _context;
    public TaskService(TaskManagementDbContext context)
    {
        _context = context;
    }
    private static TaskDto MapToDto(TaskItem task)
    {
        var clanInfo = task.Clans.FirstOrDefault();
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedTo = task.AssignedTo,
            CreatedBy = task.CreatedBy,
            DueDate = task.DueDate,
            Status = task.Status,
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
        var reminders = BuildTaskReminderEntities(task, dto.ReminderRules);
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

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;
        SyncOnDeadlineReminder(task, resetSchedule: false);

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken ct = default)
    {
        var task = await _context.TaskItems
            .Include(t => t.Reminders).ThenInclude(r => r.ReminderRule)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
        if (task == null) return;

        var dueDateChanged = task.DueDate != newDueDate;
        task.DueDate = newDueDate;
        task.UpdatedAt = DateTime.UtcNow;
        SyncOnDeadlineReminder(task, resetSchedule: dueDateChanged);

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

        if (updateDto.Status.HasValue)
            task.Status = updateDto.Status.Value;

        if (updateDto.DueDate.HasValue)
            task.DueDate = updateDto.DueDate.Value;
        if (!string.IsNullOrWhiteSpace(updateDto.AssignedTo))
            task.AssignedTo = updateDto.AssignedTo;
        if (updateDto.ReminderRules is not null)
            await ReplaceTaskRemindersAsync(task, updateDto.ReminderRules, ct);

        task.UpdatedAt = DateTime.UtcNow;
        if (dueDateChanged || assigneeChanged || statusChanged || updateDto.ReminderRules is not null)
            SyncOnDeadlineReminder(task, resetSchedule: dueDateChanged);

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

        var newReminders = BuildCustomReminderEntities(task, reminderRules);
        if (newReminders.Count > 0)
            _context.Reminders.AddRange(newReminders);
    }

    private static List<Reminder> BuildTaskReminderEntities(
        TaskItem task,
        IEnumerable<CreateReminderRuleDto> reminderRules)
    {
        var reminders = new List<Reminder>();

        if (task.DueDate.HasValue && IsTaskActive(task))
            reminders.Add(CreateOnDeadlineReminder(task));

        reminders.AddRange(BuildCustomReminderEntities(task, reminderRules));

        return reminders;
    }

    private static List<Reminder> BuildCustomReminderEntities(
        TaskItem task,
        IEnumerable<CreateReminderRuleDto> reminderRules)
    {
        var validRules = reminderRules
            .Where(rule => rule.TriggerType != EReminderTriggerType.OnDeadline)
            .Where(IsValidReminderRule)
            .ToList();

        if (validRules.Count == 0)
            return [];
        if (!task.DueDate.HasValue)
            throw new InvalidOperationException("Cannot create task reminders without a due date.");
        return validRules.Select(ruleDto =>
        {
            var triggerAt = CalculateTriggerAt(task.DueDate.Value, ruleDto);
            return new Reminder
            {
                TaskId = task.Id,
                TriggerAt = triggerAt,
                TargetUserId = task.AssignedTo,
                Status = EReminderStatus.Pending,
                NextTriggerAt = ruleDto.IsRepeat ? triggerAt : null,
                StateSnapshot = task.Status,
                ReminderRule = new ReminderRule
                {
                    TriggerType = ruleDto.TriggerType,
                    IntervalUnit = ruleDto.IntervalUnit,
                    Value = ruleDto.Value,
                    TaskStatus = task.Status,
                    IsRepeat = ruleDto.IsRepeat
                }
            };
        }).ToList();
    }

    private static Reminder CreateOnDeadlineReminder(TaskItem task)
    {
        if (!task.DueDate.HasValue)
            throw new InvalidOperationException("Cannot create an on-deadline reminder without a due date.");

        return new Reminder
        {
            TaskId = task.Id,
            TriggerAt = task.DueDate.Value,
            TargetUserId = task.AssignedTo,
            Status = EReminderStatus.Pending,
            NextTriggerAt = null,
            StateSnapshot = task.Status,
            ReminderRule = new ReminderRule
            {
                TriggerType = EReminderTriggerType.OnDeadline,
                IntervalUnit = null,
                Value = 0,
                TaskStatus = task.Status,
                IsRepeat = false
            }
        };
    }

    private void SyncOnDeadlineReminder(TaskItem task, bool resetSchedule)
    {
        var now = DateTime.UtcNow;
        var reminder = task.Reminders.FirstOrDefault(r =>
            r.ReminderRule?.TriggerType == EReminderTriggerType.OnDeadline);

        if (!task.DueDate.HasValue)
        {
            CancelPendingReminder(reminder, now);
            return;
        }

        if (!IsTaskActive(task))
        {
            CancelPendingReminder(reminder, now);
            return;
        }

        if (reminder is null)
        {
            _context.Reminders.Add(CreateOnDeadlineReminder(task));
            return;
        }

        if (resetSchedule || reminder.Status == EReminderStatus.Cancelled)
        {
            reminder.TriggerAt = task.DueDate.Value;
            reminder.NextTriggerAt = null;
            reminder.Status = EReminderStatus.Pending;
        }
        else if (reminder.Status == EReminderStatus.Pending)
        {
            reminder.TriggerAt = task.DueDate.Value;
            reminder.NextTriggerAt = null;
        }

        if (reminder.Status == EReminderStatus.Pending)
            reminder.TargetUserId = task.AssignedTo;

        reminder.StateSnapshot = task.Status;
        reminder.UpdatedAt = now;

        if (reminder.ReminderRule is not null)
        {
            reminder.ReminderRule.TaskStatus = task.Status;
            reminder.ReminderRule.UpdatedAt = now;
        }
    }

    private static void CancelPendingReminder(Reminder? reminder, DateTime now)
    {
        if (reminder?.Status != EReminderStatus.Pending)
            return;

        reminder.Status = EReminderStatus.Cancelled;
        reminder.NextTriggerAt = null;
        reminder.UpdatedAt = now;
    }

    private static bool IsTaskActive(TaskItem task) =>
        task.Status is not (ETaskStatus.Completed or ETaskStatus.Cancelled);
    private static bool IsValidReminderRule(CreateReminderRuleDto rule)
    {
        return rule.Value > 0 &&
               Enum.IsDefined(typeof(ETimeUnit), rule.IntervalUnit) &&
               Enum.IsDefined(typeof(EReminderTriggerType), rule.TriggerType);
    }
    private static DateTime CalculateTriggerAt(DateTime dueDate, CreateReminderRuleDto rule)
    {
        var interval = ToTimeSpan(rule.Value, rule.IntervalUnit);
        return rule.TriggerType switch
        {
            EReminderTriggerType.BeforeDeadline => dueDate.Subtract(interval),
            EReminderTriggerType.AfterDeadline => dueDate.Add(interval),
            EReminderTriggerType.Repeat => DateTime.UtcNow.Add(interval),
            _ => dueDate
        };
    }
    private static TimeSpan ToTimeSpan(double value, ETimeUnit unit)
    {
        return unit switch
        {
            ETimeUnit.Minutes => TimeSpan.FromMinutes(value),
            ETimeUnit.Hours => TimeSpan.FromHours(value),
            ETimeUnit.Days => TimeSpan.FromDays(value),
            ETimeUnit.Weeks => TimeSpan.FromDays(value * 7),
            _ => TimeSpan.Zero
        };
    }
}