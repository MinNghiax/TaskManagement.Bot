namespace TaskManagement.Bot.Features.TaskQuery.Persistence;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.Task.Models;

/// <summary>
/// Task Query Repository - Người 2: READ-ONLY queries (extends ITaskRepository for listing)
/// </summary>
public interface ITaskQueryRepository : IRepository<TaskEntity>
{
    // Inherited from IRepository: GetByIdAsync, ListAsync, SearchAsync
}

public class TaskQueryRepository : ITaskQueryRepository
{
    // This is a placeholder. In production, this would implement 
    // optimized read queries using EF Core or Dapper
    // Pessoa 2 can extend this with specialized query methods
    
    public Task<TaskEntity> CreateAsync(TaskEntity entity) => throw new NotImplementedException();
    public Task<TaskEntity> UpdateAsync(TaskEntity entity) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
    public Task<TaskEntity?> GetByIdAsync(int id) => throw new NotImplementedException();
    public Task<List<TaskEntity>> ListAsync() => throw new NotImplementedException();
    public Task<List<TaskEntity>> SearchAsync(Func<TaskEntity, bool> predicate) => throw new NotImplementedException();
}
