// TaskManagement.Bot.Application.Commands.Complain.ComplainCommandHandler.cs
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands.Complain;

/// <summary>
/// Handles "!complain" command. Shows single form for all complain info.
/// </summary>
public class ComplainCommandHandler : ICommandHandler
{
    private readonly IComplainService _complainService;

    public ComplainCommandHandler(IComplainService complainService)
        => _complainService = complainService;

    public bool CanHandle(string command)
        => command.Trim().Equals("!complain", StringComparison.OrdinalIgnoreCase)
        || command.Trim().StartsWith("!complain ", StringComparison.OrdinalIgnoreCase);

    public async Task<string> HandleAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        // Return empty — BotService will send the form directly (needs Content, not string)
        return string.Empty;
    }

    /// <summary>
    /// Returns the form content to send. Called by BotService.
    /// </summary>
    public async Task<ChannelMessageContent?> GetFormAsync(string userId, CancellationToken ct = default)
    {
        var tasks = await _complainService.GetComplainableTasksAsync(userId, ct);
        if (!tasks.Any())
            return null; 

        var options = tasks
            .Select(t => (object)new { label = $"#{t.Id} {t.Title} [{t.Status}]", value = t.Id.ToString() })
            .ToArray();

        return ComplainFormBuilder.BuildComplainForm(options);
    }
}