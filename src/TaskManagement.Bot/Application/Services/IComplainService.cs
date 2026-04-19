using TaskManagement.Bot.Application.DTOs;

namespace TaskManagement.Bot.Application.Services;

public interface IComplainService
{
    Task<(ComplainDto? Result, string? Error)> CreateAsync(CreateComplainDto dto, CancellationToken ct = default);

    Task<(bool Success, string? Error)> ReviewAsync(ApproveComplainDto dto, CancellationToken ct = default);

    Task<ComplainDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<List<TaskDto>> GetComplainableTasksAsync(string userId, CancellationToken ct = default);

    Task<List<ComplainDto>> GetPendingByPMAsync(string pmUserId, CancellationToken ct = default);
}