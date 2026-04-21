using Mezon.Sdk;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services.Reminders;

public class MezonReminderNotificationSender : IReminderNotificationSender
{
    private static readonly TimeSpan UserLookupRetryDelay = TimeSpan.FromMinutes(1);

    private readonly MezonClient _client;
    private readonly IMezonUserService _userService;
    private readonly ILogger<MezonReminderNotificationSender> _logger;

    public MezonReminderNotificationSender(
        MezonClient client,
        IMezonUserService userService,
        ILogger<MezonReminderNotificationSender> logger)
    {
        _client = client;
        _userService = userService;
        _logger = logger;
    }

    public async Task SendAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        var assigneeUsername = await ResolveAssigneeUsernameAsync(reminder, cancellationToken);
        var message = MessageBuilder.BuildReminderNotification(reminder, assigneeUsername);

        var user = await _client.GetUserAsync(reminder.TargetUserId, cancellationToken);
        await user.SendDMAsync(message, cancellationToken: cancellationToken);
    }

    private async Task<string> ResolveAssigneeUsernameAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var clanId = GetClanId(reminder);

        while (true)
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

                _logger.LogWarning(
                    "Could not resolve user {UserId} for reminder {ReminderId}. Retrying in {DelayMinutes} minute(s).",
                    reminder.TargetUserId,
                    reminder.Id,
                    UserLookupRetryDelay.TotalMinutes);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to resolve user {UserId} for reminder {ReminderId}. Retrying in {DelayMinutes} minute(s).",
                    reminder.TargetUserId,
                    reminder.Id,
                    UserLookupRetryDelay.TotalMinutes);
            }

            attempt++;
            await Task.Delay(UserLookupRetryDelay, cancellationToken);
        }
    }

    private static string? GetClanId(Reminder reminder) =>
        reminder.Task?.Clans.FirstOrDefault()?.ClanId;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
