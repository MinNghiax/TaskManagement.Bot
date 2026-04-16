using System.Text.Json;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Commands;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public class TeamCommandHandler : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.StartsWith("!team", StringComparison.OrdinalIgnoreCase);
    }

    public Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        var content = ParseContent(message.Content?.Text);
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new CommandResponse("Empty command"));
        }

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && string.Equals(parts[1], "init", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(message.ClanId))
            {
                return Task.FromResult(new CommandResponse("Khong tim thay clan de khoi tao team"));
            }

            return Task.FromResult(new CommandResponse(TeamFormBuilder.BuildTeamForm(message.ClanId)));
        }

        return Task.FromResult(new CommandResponse("Usage: !team init"));
    }

    private static string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!raw.StartsWith("{", StringComparison.Ordinal))
        {
            return raw;
        }

        try
        {
            using var json = JsonDocument.Parse(raw);
            return json.RootElement.GetProperty("t").GetString();
        }
        catch
        {
            return raw;
        }
    }
}
