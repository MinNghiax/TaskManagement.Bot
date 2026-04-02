namespace TaskManagement.Bot.Features.Reminder.Persistence;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.Reminder.Models;

/// <summary>
/// Reminder Repository - Người 3: Persistence layer
/// </summary>
public interface IReminderRepository : IRepository<ReminderEntity>
{
    Task<List<ReminderEntity>> GetByTaskIdAsync(int taskId);
    Task<List<ReminderEntity>> GetDueAsync();
}

public class ReminderRepository : IReminderRepository
{
    private readonly List<ReminderEntity> _reminders = new();
    private int _nextId = 1;

    public async Task<ReminderEntity> CreateAsync(ReminderEntity entity)
    {
        entity.Id = _nextId++;
        entity.CreatedAt = DateTime.UtcNow;
        _reminders.Add(entity);
        return await Task.FromResult(entity);
    }

    public async Task<ReminderEntity> UpdateAsync(ReminderEntity entity)
    {
        var existing = _reminders.FirstOrDefault(r => r.Id == entity.Id);
        if (existing == null) throw new KeyNotFoundException();

        entity.UpdatedAt = DateTime.UtcNow;
        var index = _reminders.IndexOf(existing);
        _reminders[index] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = _reminders.FirstOrDefault(r => r.Id == id);
        if (entity == null) return false;
        _reminders.Remove(entity);
        return await Task.FromResult(true);
    }

    public async Task<ReminderEntity?> GetByIdAsync(int id)
    {
        return await Task.FromResult(_reminders.FirstOrDefault(r => r.Id == id));
    }

    public async Task<List<ReminderEntity>> ListAsync()
    {
        return await Task.FromResult(new List<ReminderEntity>(_reminders));
    }

    public async Task<List<ReminderEntity>> SearchAsync(Func<ReminderEntity, bool> predicate)
    {
        return await Task.FromResult(_reminders.Where(predicate).ToList());
    }

    public async Task<List<ReminderEntity>> GetByTaskIdAsync(int taskId)
    {
        return await Task.FromResult(_reminders.Where(r => r.TaskId == taskId).ToList());
    }

    public async Task<List<ReminderEntity>> GetDueAsync()
    {
        return await Task.FromResult(_reminders
            .Where(r => r.ReminderTime <= DateTime.UtcNow && !r.IsTriggered)
            .ToList());
    }
}
