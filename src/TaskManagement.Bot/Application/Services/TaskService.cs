using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

    // 🔁 Convert Entity → DTO
    private static TaskDto MapToDto(TaskItem task)
    {
        // Lấy Clan đầu tiên (thường 1 task chỉ thuộc 1 clan/channel tại 1 thời điểm)
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
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
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

        // 🔥 CLANS
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

        // 🔥 CHANNELS (bạn đang thiếu cái này)
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
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        return task == null ? null : MapToDto(task);
    }

    public async Task<List<TaskDto>> GetByAssigneeAsync(string assignee, string? channelId, CancellationToken ct = default)
    {
        var query = _context.TaskItems
            .Include(t => t.Clans)
            .Include(t => t.Channels)
            .Where(t => t.AssignedTo == assignee && !t.IsDeleted);

        // chỉ filter khi có channelId
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

        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task == null || task.IsDeleted) return;

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;

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
            .Where(t => t.Clans.Any(c => c.ClanId == channelId)) // Lọc dựa trên bảng phụ
            .ToListAsync();

        return tasks.Select(MapToDto).ToList();
    }

    public async Task UpdateAsync(int taskId, UpdateTaskDto updateDto, CancellationToken ct = default)
    {
        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);
        if (task == null) throw new Exception("Task not found");

        if (!string.IsNullOrWhiteSpace(updateDto.Title))
            task.Title = updateDto.Title;

        if (updateDto.Description != null)
            task.Description = updateDto.Description;

        if (updateDto.Priority.HasValue)
            task.Priority = updateDto.Priority.Value;

        if (updateDto.Status.HasValue)
            task.Status = updateDto.Status.Value;

        if (updateDto.DueDate.HasValue)
            task.DueDate = updateDto.DueDate.Value;

        if (!string.IsNullOrWhiteSpace(updateDto.AssignedTo))
            task.AssignedTo = updateDto.AssignedTo;

        task.UpdatedAt = DateTime.UtcNow;

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

    //Them phan Complain
    public async Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken cancellationToken = default)
    {
        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);
        if (task == null || task.IsDeleted) return;

        task.DueDate = newDueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}