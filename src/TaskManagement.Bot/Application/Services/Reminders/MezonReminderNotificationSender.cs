using Mezon.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services.Reminders;

public class MezonReminderNotificationSender : IReminderNotificationSender
{
    private readonly MezonClient _client;
    private readonly IMezonUserService _userService;
    private readonly ILogger<MezonReminderNotificationSender> _logger;
    private readonly TimeZoneInfo _timeZone;

    public MezonReminderNotificationSender(
        MezonClient client,
        IMezonUserService userService,
        ILogger<MezonReminderNotificationSender> logger,
        IConfiguration configuration)
    {
        _client = client;
        _userService = userService;
        _logger = logger;
        _timeZone = ReminderSchedulerConfiguration.CreateTimeZone(configuration);
    }

    public async Task SendAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        var assigneeUsername = await ResolveAssigneeUsernameAsync(reminder, cancellationToken);
        var message = MessageBuilder.BuildReminderNotification(reminder, assigneeUsername, _timeZone);

        try
        {
            _logger.LogInformation(
                "Sending reminder {ReminderId} for task {TaskId} to user {TargetUserId}",
                reminder.Id,
                reminder.TaskId,
                reminder.TargetUserId);

            var user = await _client.GetUserAsync(reminder.TargetUserId, cancellationToken);
            var ack = await user.SendDMAsync(message, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Sent reminder {ReminderId} for task {TaskId} to user {TargetUserId}. DmChannelId={DmChannelId}, AckChannelId={AckChannelId}, AckMessageId={AckMessageId}",
                reminder.Id,
                reminder.TaskId,
                reminder.TargetUserId,
                user.DmChannelId,
                ack.ChannelId,
                ack.MessageId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send reminder {ReminderId} for task {TaskId} to user {TargetUserId}",
                reminder.Id,
                reminder.TaskId,
                reminder.TargetUserId);

            throw;
        }
    }

    private async Task<string> ResolveAssigneeUsernameAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        var clanId = GetClanId(reminder);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var user = attempt == 0
                    ? await _userService.GetUserAsync(reminder.TargetUserId, clanId, cancellationToken)
                    : await _userService.RefreshUserAsync(reminder.TargetUserId, clanId, cancellationToken);
                var username = FirstNonEmpty(user?.Username, user?.DisplayName, user?.ClanNick);

                if (!string.IsNullOrWhiteSpace(username))
                {
                    return username;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to resolve display name for user {UserId} on reminder {ReminderId}",
                    reminder.TargetUserId,
                    reminder.Id);
            }
        }

        _logger.LogWarning(
            "Could not resolve display name for user {UserId} on reminder {ReminderId}. Using user id fallback.",
            reminder.TargetUserId,
            reminder.Id);

        return reminder.TargetUserId;
    }

    private static string? GetClanId(Reminder reminder) =>
        reminder.Task?.Clans.FirstOrDefault()?.ClanId;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
