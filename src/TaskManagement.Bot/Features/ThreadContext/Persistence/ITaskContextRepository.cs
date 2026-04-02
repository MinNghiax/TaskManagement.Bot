namespace TaskManagement.Bot.Features.ThreadContext.Persistence;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.ThreadContext.Models;

/// <summary>
/// Task Context Repository - Pessoa 4: Persistence layer
/// </summary>
public interface ITaskContextRepository : IRepository<TaskContextEntity>
{
    Task<TaskContextEntity?> GetByTaskIdAsync(int taskId);
    Task<List<TaskContextEntity>> GetByThreadIdAsync(string threadId);
}

public class TaskContextRepository : ITaskContextRepository
{
    private readonly List<TaskContextEntity> _contexts = new();
    private int _nextId = 1;

    public async Task<TaskContextEntity> CreateAsync(TaskContextEntity entity)
    {
        entity.Id = _nextId++;
        entity.CreatedAt = DateTime.UtcNow;
        _contexts.Add(entity);
        return await Task.FromResult(entity);
    }

    public async Task<TaskContextEntity> UpdateAsync(TaskContextEntity entity)
    {
        var existing = _contexts.FirstOrDefault(c => c.Id == entity.Id);
        if (existing == null) throw new KeyNotFoundException();

        entity.UpdatedAt = DateTime.UtcNow;
        var index = _contexts.IndexOf(existing);
        _contexts[index] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = _contexts.FirstOrDefault(c => c.Id == id);
        if (entity == null) return false;
        _contexts.Remove(entity);
        return await Task.FromResult(true);
    }

    public async Task<TaskContextEntity?> GetByIdAsync(int id)
    {
        return await Task.FromResult(_contexts.FirstOrDefault(c => c.Id == id));
    }

    public async Task<List<TaskContextEntity>> ListAsync()
    {
        return await Task.FromResult(new List<TaskContextEntity>(_contexts));
    }

    public async Task<List<TaskContextEntity>> SearchAsync(Func<TaskContextEntity, bool> predicate)
    {
        return await Task.FromResult(_contexts.Where(predicate).ToList());
    }

    public async Task<TaskContextEntity?> GetByTaskIdAsync(int taskId)
    {
        return await Task.FromResult(_contexts.FirstOrDefault(c => c.TaskId == taskId));
    }

    public async Task<List<TaskContextEntity>> GetByThreadIdAsync(string threadId)
    {
        return await Task.FromResult(_contexts.Where(c => c.ThreadId == threadId).ToList());
    }
}
