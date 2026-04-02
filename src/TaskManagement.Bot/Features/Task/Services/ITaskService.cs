namespace TaskManagement.Bot.Features.Task.Services;

using TaskManagement.Bot.Features.Task.Models;
using TaskManagement.Bot.Features.Task.Persistence;

/// <summary>
/// Task Service - Người 1: Business logic for task management
/// </summary>
public interface ITaskService
{
    Task<TaskEntity> CreateAsync(CreateTaskDto dto);
    Task<TaskEntity?> GetAsync(int id);
    Task<TaskEntity> UpdateAsync(int id, CreateTaskDto dto);
    Task<bool> DeleteAsync(int id);
    Task<TaskEntity> ChangeStatusAsync(int id, TaskStatus status);
}

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskEntity> CreateAsync(CreateTaskDto dto)
    {
        var entity = new TaskEntity
        {
            Title = dto.Title,
            Description = dto.Description,
            Deadline = dto.Deadline,
            AssignedTo = dto.AssignedTo,
            Priority = dto.Priority,
            Status = TaskStatus.ToDo
        };
        return await _repository.CreateAsync(entity);
    }

    public async Task<TaskEntity?> GetAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<TaskEntity> UpdateAsync(int id, CreateTaskDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new KeyNotFoundException();

        entity.Title = dto.Title ?? entity.Title;
        entity.Description = dto.Description ?? entity.Description;
        entity.Deadline = dto.Deadline ?? entity.Deadline;
        entity.Priority = dto.Priority > 0 ? dto.Priority : entity.Priority;
        
        return await _repository.UpdateAsync(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<TaskEntity> ChangeStatusAsync(int id, TaskStatus status)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new KeyNotFoundException();

        entity.Status = status;
        return await _repository.UpdateAsync(entity);
    }
}
