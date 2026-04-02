namespace TaskManagement.Bot.Features.TaskQuery.Services;

using TaskManagement.Bot.Features.Task.Models;
using TaskManagement.Bot.Features.Task.Persistence;
using TaskManagement.Bot.Features.TaskQuery.Models;

/// <summary>
/// Task Search Service - Người 2: Search, List, Filter operations
/// </summary>
public interface ITaskSearchService
{
    Task<List<TaskSearchResultDto>> SearchAsync(TaskSearchFilterDto filter);
    Task<List<TaskSearchResultDto>> ListAsync(int page = 1, int pageSize = 10);
    Task<TaskSearchResultDto?> GetDetailsAsync(int id);
}

public class TaskSearchService : ITaskSearchService
{
    private readonly ITaskRepository _repository;

    public TaskSearchService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TaskSearchResultDto>> SearchAsync(TaskSearchFilterDto filter)
    {
        var all = await _repository.ListAsync();
        var filtered = all.AsEnumerable();

        if (!string.IsNullOrEmpty(filter.SearchText))
            filtered = filtered.Where(t => t.Title?.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

        if (filter.Status.HasValue)
            filtered = filtered.Where(t => t.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.AssignedTo))
            filtered = filtered.Where(t => t.AssignedTo == filter.AssignedTo);

        return filtered
            .Select(t => new TaskSearchResultDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Deadline = t.Deadline,
                AssignedTo = t.AssignedTo
            })
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();
    }

    public async Task<List<TaskSearchResultDto>> ListAsync(int page = 1, int pageSize = 10)
    {
        var all = await _repository.ListAsync();
        return all
            .Select(t => new TaskSearchResultDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Deadline = t.Deadline,
                AssignedTo = t.AssignedTo
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<TaskSearchResultDto?> GetDetailsAsync(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null) return null;

        return new TaskSearchResultDto
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            Deadline = task.Deadline,
            AssignedTo = task.AssignedTo
        };
    }
}
