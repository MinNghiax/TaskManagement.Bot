namespace TaskManagement.Bot.Application.Services.Reminders;

public interface IReminderProcessor
{
    Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
