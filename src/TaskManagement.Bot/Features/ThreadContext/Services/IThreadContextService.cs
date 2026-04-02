namespace TaskManagement.Bot.Features.ThreadContext.Services;

using TaskManagement.Bot.Features.ThreadContext.Models;
using TaskManagement.Bot.Features.ThreadContext.Persistence;

/// <summary>
/// Thread Context Service - Người 4: Bind tasks to threads/channels
/// </summary>
public interface IThreadContextService
{
    Task<TaskContextEntity> BindTaskToThreadAsync(CreateTaskContextDto dto);
    Task<TaskContextEntity?> GetAsync(int id);
    Task<TaskContextEntity?> GetByTaskIdAsync(int taskId);
    Task<bool> DeleteAsync(int id);
    Task<List<TaskContextEntity>> GetContextualRemindersAsync(string threadId);
}

public class ThreadContextService : IThreadContextService
{
    private readonly ITaskContextRepository _repository;

    public ThreadContextService(ITaskContextRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskContextEntity> BindTaskToThreadAsync(CreateTaskContextDto dto)
    {
        var entity = new TaskContextEntity
        {
            TaskId = dto.TaskId,
            ThreadId = dto.ThreadId,
            ChannelId = dto.ChannelId,
            IsActive = true
        };
        return await _repository.CreateAsync(entity);
    }

    public async Task<TaskContextEntity?> GetAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<TaskContextEntity?> GetByTaskIdAsync(int taskId)
    {
        var all = await _repository.ListAsync();
        return all.FirstOrDefault(c => c.TaskId == taskId);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<List<TaskContextEntity>> GetContextualRemindersAsync(string threadId)
    {
        var all = await _repository.ListAsync();
        return all.Where(c => c.ThreadId == threadId && c.IsActive).ToList();
    }
}
