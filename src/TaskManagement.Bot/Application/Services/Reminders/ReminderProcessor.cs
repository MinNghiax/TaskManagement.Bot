using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Repositories;

namespace TaskManagement.Bot.Application.Services.Reminders;

public class ReminderProcessor : ReminderService
{
    public ReminderProcessor(
        TaskManagementDbContext context,
        IReminderNotificationSender notificationSender,
        ILogger<ReminderProcessor> logger,
        IConfiguration? configuration = null)
        : base(
            new ReminderRepository(context),
            notificationSender,
            logger,
            configuration?["JobSettings:Review:AutoCompleteAfter"])
    {
    }
}
