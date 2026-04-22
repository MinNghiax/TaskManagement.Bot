using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services.Reminders;

public interface IReminderNotificationSender
{
    Task SendAsync(Reminder reminder, CancellationToken cancellationToken);
}
