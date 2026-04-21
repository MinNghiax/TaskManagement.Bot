namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;

public interface IReminderRepository : IRepository<Reminder>
{
    Task<IEnumerable<Reminder>> GetByTaskIdAsync(int taskId);
    Task<IEnumerable<Reminder>> GetPendingAsync();
    Task<IEnumerable<Reminder>> GetByUserAsync(string mezonUserId);
    Task<IEnumerable<Reminder>> GetDueAsync(DateTime beforeTime);
}
