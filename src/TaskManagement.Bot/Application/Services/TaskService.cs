// TaskManagement.Bot.Application.Services.TaskService.cs
using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public class TaskService : ITaskService
{
    private readonly TaskManagementDbContext _ctx;
    public TaskService(TaskManagementDbContext ctx) => _ctx = ctx;

    private static TaskDto Map(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        AssignedTo = t.AssignedTo,
        CreatedBy = t.CreatedBy,
        DueDate = t.DueDate,
        Status = t.Status,
        Priority = t.Priority,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    public async Task<TaskDto?> CreateAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.AssignedTo))
            return null;
        var t = new TaskItem
        {
            Title = dto.Title!,
            Description = dto.Description,
            AssignedTo = dto.AssignedTo!,
            CreatedBy = dto.CreatedBy ?? "system",
            DueDate = dto.DueDate,
            Status = dto.Status,
            Priority = dto.Priority
        };
        _ctx.TaskItems.Add(t);
        await _ctx.SaveChangesAsync(ct);
        return Map(t);
    }

    public Task<TaskDto?> GetByIdAsync(int taskId, CancellationToken ct = default)
        => _ctx.TaskItems.Where(t => t.Id == taskId && !t.IsDeleted)
            .Select(t => (TaskDto?)Map(t)).FirstOrDefaultAsync(ct);

    public async Task<List<TaskDto>> GetByAssigneeAsync(string assignee, CancellationToken ct = default)
        => (await _ctx.TaskItems.Where(t => t.AssignedTo == assignee && !t.IsDeleted).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<List<TaskDto>> GetByStatusAsync(ETaskStatus status, CancellationToken ct = default)
        => (await _ctx.TaskItems.Where(t => t.Status == status && !t.IsDeleted).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task<List<TaskDto>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.TaskItems.Where(t => !t.IsDeleted).OrderByDescending(t => t.CreatedAt).ToListAsync(ct))
            .Select(Map).ToList();

    public async Task ChangeStatusAsync(int taskId, ETaskStatus newStatus, CancellationToken ct = default)
    {
        var t = await _ctx.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t == null || t.IsDeleted) return;
        t.Status = newStatus; t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateDueDateAsync(int taskId, DateTime newDueDate, CancellationToken ct = default)
    {
        var t = await _ctx.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t == null || t.IsDeleted) return;
        t.DueDate = newDueDate; t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int taskId, CancellationToken ct = default)
    {
        var t = await _ctx.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (t == null || t.IsDeleted) return;
        t.IsDeleted = true; t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }
}