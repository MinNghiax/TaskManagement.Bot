using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using TaskStatus = TaskManagement.Bot.Infrastructure.Enums.TaskStatus;
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
        return new TaskDto
        {
            Id = new Guid(task.Id.ToString().PadLeft(32, '0')),
            Title = task.Title,
            Description = task.Description,
            AssignedTo = task.AssignedTo,
            CreatedBy = task.CreatedBy,
            DueDate = task.DueDate,
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    private static int GuidToInt(Guid guid)
    {
        return int.Parse(guid.ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber);
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
            Status = dto.Status,
            Priority = dto.Priority
        };

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync(ct);

        return MapToDto(task);
    }

    public async Task<List<TaskDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var id = GuidToInt(taskId);

        var task = await _context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

        return task == null ? null : MapToDto(task);
    }

    public async Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Where(t => t.AssignedTo == assignee && !t.IsDeleted)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    public async Task<List<TaskDto>> GetByStatusAsync(TaskStatus status, CancellationToken ct = default)
    {
        var tasks = await _context.TaskItems
            .Where(t => t.Status == status && !t.IsDeleted)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    public async Task ChangeStatusAsync(Guid taskId, TaskStatus newStatus, CancellationToken ct = default)
    {
        var id = GuidToInt(taskId);

        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task == null || task.IsDeleted) return;

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken ct = default)
    {
        var id = GuidToInt(taskId);

        var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task == null || task.IsDeleted) return;

        task.IsDeleted = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}