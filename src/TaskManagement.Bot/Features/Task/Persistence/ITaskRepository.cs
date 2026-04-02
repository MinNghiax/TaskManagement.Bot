namespace TaskManagement.Bot.Features.Task.Persistence;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.Task.Models;

/// <summary>
/// Task Repository - Người 1: CRUD operations
/// </summary>
public interface ITaskRepository : IRepository<TaskEntity>
{
    Task<List<TaskEntity>> GetByAssigneeAsync(string assignee);
    Task<List<TaskEntity>> GetByStatusAsync(TaskStatus status);
    Task<List<TaskEntity>> GetOverdueAsync();
}

public class TaskRepository : ITaskRepository
{
    private readonly List<TaskEntity> _tasks = new();
    private int _nextId = 1;

    public async Task<TaskEntity> CreateAsync(TaskEntity entity)
    {
        entity.Id = _nextId++;
        entity.CreatedAt = DateTime.UtcNow;
        _tasks.Add(entity);
        return await Task.FromResult(entity);
    }

    public async Task<TaskEntity> UpdateAsync(TaskEntity entity)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == entity.Id);
        if (existing == null) throw new KeyNotFoundException();
        
        entity.UpdatedAt = DateTime.UtcNow;
        var index = _tasks.IndexOf(existing);
        _tasks[index] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = _tasks.FirstOrDefault(t => t.Id == id);
        if (entity == null) return false;
        _tasks.Remove(entity);
        return await Task.FromResult(true);
    }

    public async Task<TaskEntity?> GetByIdAsync(int id)
    {
        return await Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
    }

    public async Task<List<TaskEntity>> ListAsync()
    {
        return await Task.FromResult(new List<TaskEntity>(_tasks));
    }

    public async Task<List<TaskEntity>> SearchAsync(Func<TaskEntity, bool> predicate)
    {
        return await Task.FromResult(_tasks.Where(predicate).ToList());
    }

    public async Task<List<TaskEntity>> GetByAssigneeAsync(string assignee)
    {
        return await Task.FromResult(_tasks.Where(t => t.AssignedTo == assignee).ToList());
    }

    public async Task<List<TaskEntity>> GetByStatusAsync(TaskStatus status)
    {
        return await Task.FromResult(_tasks.Where(t => t.Status == status).ToList());
    }

    public async Task<List<TaskEntity>> GetOverdueAsync()
    {
        return await Task.FromResult(_tasks
            .Where(t => t.Deadline < DateTime.UtcNow && t.Status != TaskStatus.Done)
            .ToList());
    }
}
