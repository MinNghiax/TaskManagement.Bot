namespace TaskManagement.Bot.Features.Reminder.Services;

using TaskManagement.Bot.Features.Reminder.Models;
using TaskManagement.Bot.Features.Reminder.Persistence;

/// <summary>
/// Reminder Service - Người 3: Schedule, snooze, trigger reminders
/// </summary>
public interface IReminderService
{
    Task<ReminderEntity> CreateAsync(CreateReminderDto dto);
    Task<ReminderEntity?> GetAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<ReminderEntity> SnoozeAsync(int id, int minutesFromNow);
    Task<List<ReminderEntity>> GetDueRemindersAsync();
}

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _repository;

    public ReminderService(IReminderRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReminderEntity> CreateAsync(CreateReminderDto dto)
    {
        var entity = new ReminderEntity
        {
            TaskId = dto.TaskId,
            ReminderTime = dto.ReminderTime,
            RepeatType = dto.RepeatType,
            IsTriggered = false
        };
        return await _repository.CreateAsync(entity);
    }

    public async Task<ReminderEntity?> GetAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<ReminderEntity> SnoozeAsync(int id, int minutesFromNow)
    {
        var reminder = await _repository.GetByIdAsync(id);
        if (reminder == null) throw new KeyNotFoundException();

        reminder.ReminderTime = DateTime.UtcNow.AddMinutes(minutesFromNow);
        return await _repository.UpdateAsync(reminder);
    }

    public async Task<List<ReminderEntity>> GetDueRemindersAsync()
    {
        var all = await _repository.ListAsync();
        return all.Where(r => r.ReminderTime <= DateTime.UtcNow && !r.IsTriggered).ToList();
    }
}
