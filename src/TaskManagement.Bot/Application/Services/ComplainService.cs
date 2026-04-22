using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Domain.Interfaces;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;
using ETaskStatus = TaskManagement.Bot.Infrastructure.Enums.ETaskStatus;

namespace TaskManagement.Bot.Application.Services;

public class ComplainService : IComplainService
{
    private readonly IComplainRepository _repo;
    private readonly ITaskService _tasks;

    public ComplainService(IComplainRepository repo, ITaskService tasks)
    {
        _repo = repo;
        _tasks = tasks;
    }

    public async Task<(ComplainDto? Result, string? Error)> CreateAsync(CreateComplainDto dto, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(dto.TaskItemId, ct);
        if (task == null) return (null, "Task does not exist.");

        // ✅ THÊM: Chặn complaint nếu task đang ở trạng thái Review
        if (task.Status == ETaskStatus.Review)
            return (null, "❌ Cannot complain about a task that is in Review status. Please wait for the review to complete.");

        if (task.Status is ETaskStatus.Completed or ETaskStatus.Cancelled)
            return (null, "Cannot complain about completed or cancelled tasks.");

        if (task.Status == ETaskStatus.Late && dto.Type == EComplainType.RequestCancel)
            return (null, "Task is Late: only deadline extension requests are allowed, not cancellation.");

        if (task.AssignedTo != dto.UserId)
            return (null, "You are not the assignee of this task.");

        var pending = await _repo.GetPendingByTaskAsync(dto.TaskItemId, ct);
        if (pending != null) return (null, "This task already has a pending complaint.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "Reason cannot be empty.");

        // Validate duration for Extend
        if (dto.Type == EComplainType.RequestExtend)
        {
            if (dto.NewDueDate == null) return (null, "Must select extension duration.");
            if (dto.NewDueDate <= DateTime.UtcNow) return (null, "New deadline must be after current time.");
            if (task.DueDate.HasValue && dto.NewDueDate <= task.DueDate)
                return (null, "New deadline must be after current deadline.");
        }

        var complain = new Complain
        {
            TaskItemId = dto.TaskItemId,
            UserId = dto.UserId,
            Reason = dto.Reason,
            Type = dto.Type,
            Status = EComplainStatus.Pending,
            OldDueDate = task.DueDate,
            NewDueDate = dto.NewDueDate
        };

        var created = await _repo.CreateAsync(complain, ct);
        return (MapToDto(created, task.Title), null);
    }

    public async Task<(bool Success, string? Error)> ReviewAsync(ApproveComplainDto dto, CancellationToken ct = default)
    {
        var complain = await _repo.GetByIdAsync(dto.ComplainId, ct);
        if (complain == null) return (false, "Complaint does not exist.");
        if (complain.Status != EComplainStatus.Pending) return (false, "This complaint has already been processed.");

        var task = await _tasks.GetByIdAsync(complain.TaskItemId, ct);
        if (task == null) return (false, "Task does not exist.");

        // Only PM (task creator) can review
        if (task.CreatedBy != dto.ApprovedBy)
            return (false, "Only the task creator (PM) can review this complaint.");

        complain.ApprovedBy = dto.ApprovedBy;
        complain.ApprovedAt = DateTime.UtcNow;

        if (dto.IsApproved)
        {
            complain.Status = EComplainStatus.Approved;

            if (complain.Type == EComplainType.RequestExtend)
            {
                await _tasks.UpdateDueDateAsync(task.Id, complain.NewDueDate!.Value, ct);

                // Nếu deadline mới trong tương lai -> Doing, ngược lại -> Late
                if (complain.NewDueDate.Value > DateTime.UtcNow)
                {
                    await _tasks.ChangeStatusAsync(task.Id, ETaskStatus.Doing, ct);
                }
                else
                {
                    await _tasks.ChangeStatusAsync(task.Id, ETaskStatus.Late, ct);
                }
            }
            else // RequestCancel
            {
                await _tasks.ChangeStatusAsync(task.Id, ETaskStatus.Cancelled, ct);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.RejectReason))
                return (false, "Must provide rejection reason.");
            complain.Status = EComplainStatus.Rejected;
            complain.RejectReason = dto.RejectReason;
            // Task status unchanged
        }

        await _repo.SaveAsync(ct);
        return (true, null);
    }

    public async Task<ComplainDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _repo.GetByIdAsync(id, ct);
        return c == null ? null : MapToDto(c, c.TaskItem?.Title);
    }

    public async Task<List<TaskDto>> GetComplainableTasksAsync(string userId, CancellationToken ct = default)
    {
        var myTasks = await _tasks.GetByAssigneeAsync(userId, null, ct);
        var result = new List<TaskDto>();
        foreach (var t in myTasks)
        {
            // ✅ THÊM: Loại trừ task Review
            if (t.Status is ETaskStatus.Completed or ETaskStatus.Cancelled or ETaskStatus.Review)
                continue;
            var pending = await _repo.GetPendingByTaskAsync(t.Id, ct);
            if (pending == null) result.Add(t);
        }
        return result;
    }

    public async Task<List<ComplainDto>> GetPendingByPMAsync(string pmUserId, CancellationToken ct = default)
    {
        var complains = await _repo.GetPendingByPMAsync(pmUserId, ct);
        var result = new List<ComplainDto>();
        foreach (var c in complains)
        {
            var task = await _tasks.GetByIdAsync(c.TaskItemId, ct);
            result.Add(MapToDto(c, task?.Title));
        }
        return result;
    }

    private static ComplainDto MapToDto(Complain c, string? taskTitle) => new()
    {
        Id = c.Id,
        TaskItemId = c.TaskItemId,
        TaskTitle = taskTitle,
        UserId = c.UserId,
        Reason = c.Reason,
        Type = c.Type.ToString(),
        Status = c.Status.ToString(),
        NewDueDate = c.NewDueDate,
        OldDueDate = c.OldDueDate,
        ApprovedBy = c.ApprovedBy,
        ApprovedAt = c.ApprovedAt,
        RejectReason = c.RejectReason,
        CreatedAt = c.CreatedAt
    };
}