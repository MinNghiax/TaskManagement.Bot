using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Commands.TeamCommands;

namespace TaskManagement.Bot.Application.Commands;

public class TeamCommandHandler : ICommandHandler
{
    public bool CanHandle(string command) =>
        command.StartsWith("!team", StringComparison.OrdinalIgnoreCase);

    public Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        var content = ParseContent(message.Content?.Text);
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(new CommandResponse("❌ Empty command"));

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return Task.FromResult(new CommandResponse(GetHelpText()));

        return parts[1].ToLowerInvariant() switch
        {
            "init" => Task.FromResult(new CommandResponse(TeamFormBuilder.BuildTeamForm(3))),
            _ => Task.FromResult(new CommandResponse(GetHelpText()))
        };
    }

    private static string GetHelpText() => """
        📋 **Quản lý Team**
        
        `!team init` - Tạo project và team mới
        """;

    private static string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!raw.StartsWith("{"))
            return raw;

        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(raw);
            return json.RootElement.GetProperty("t").GetString();
        }
        catch
        {
            return raw;
        }
    }
}
