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
        if (task == null) return (null, "Task không tồn tại.");

        if (task.Status is ETaskStatus.Completed or ETaskStatus.Cancelled)
            return (null, "Không thể khiếu nại task đã hoàn thành hoặc đã hủy.");

        if (task.Status == ETaskStatus.Late && dto.Type == EComplainType.RequestCancel)
            return (null, "Task đang Late: chỉ được xin dời deadline, không được xin hủy.");

        if (task.AssignedTo != dto.UserId)
            return (null, "Bạn không phải người được giao task này.");

        var pending = await _repo.GetPendingByTaskAsync(dto.TaskItemId, ct);
        if (pending != null) return (null, "Task đã có khiếu nại đang chờ duyệt.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "Lý do không được để trống.");

        if (dto.Type == EComplainType.RequestExtend)
        {
            if (dto.NewDueDate == null) return (null, "Phải chọn thời gian dời hạn.");
            if (dto.NewDueDate <= DateTime.UtcNow) return (null, "Deadline mới phải sau thời điểm hiện tại.");
            if (task.DueDate.HasValue && dto.NewDueDate <= task.DueDate)
                return (null, "Deadline mới phải sau deadline cũ.");
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
        if (complain == null) return (false, "Complain không tồn tại.");
        if (complain.Status != EComplainStatus.Pending) return (false, "Complain này đã được xử lý.");

        var task = await _tasks.GetByIdAsync(complain.TaskItemId, ct);
        if (task == null) return (false, "Task không tồn tại.");

        // Only PM (task creator) can review
        if (task.CreatedBy != dto.ApprovedBy)
            return (false, "Chỉ người tạo task (PM) mới được duyệt complain.");

        

        complain.ApprovedBy = dto.ApprovedBy;
        complain.ApprovedAt = DateTime.UtcNow;

        if (dto.IsApproved)
        {
            complain.Status = EComplainStatus.Approved;

            if (complain.Type == EComplainType.RequestExtend)
            {
                // Update deadline; Late → Doing
                await _tasks.UpdateDueDateAsync(task.Id, complain.NewDueDate!.Value, ct);
                if (task.Status == ETaskStatus.Late)
                    await _tasks.ChangeStatusAsync(task.Id, ETaskStatus.Doing, ct);
            }
            else // RequestCancel
            {
                await _tasks.ChangeStatusAsync(task.Id, ETaskStatus.Cancelled, ct);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.RejectReason))
                return (false, "Phải nhập lý do từ chối.");
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
        var myTasks = await _tasks.GetByAssigneeAsync(userId, ct);
        var result = new List<TaskDto>();
        foreach (var t in myTasks)
        {
            if (t.Status is ETaskStatus.Completed or ETaskStatus.Cancelled) continue;
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