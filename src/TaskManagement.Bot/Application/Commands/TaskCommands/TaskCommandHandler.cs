using Mezon.Sdk.Domain;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.Sessions;

namespace TaskManagement.Bot.Application.Commands;

public class TaskCommandHandler : ICommandHandler
{
    private readonly SessionService _sessionService;

    public TaskCommandHandler(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public bool CanHandle(string command)
    {
        return command.StartsWith("!team") || command.StartsWith("!task");
    }

    public async Task<CommandResponse> HandleAsync(
        ChannelMessage message,
        CancellationToken cancellationToken)
    {
        var content = ParseContent(message.Content?.Text);

        if (string.IsNullOrWhiteSpace(content))
            return new CommandResponse("❌ Empty command");

        if (!long.TryParse(message.SenderId, out var userId))
            return new CommandResponse("❌ Invalid user");

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];

        return cmd switch
        {
            "!team" => await HandleTeam(parts, message),
            "!task" => await HandleTask(parts, message, userId),
            _ => new CommandResponse("❌ Unknown command")
        };
    }

    // ================= TEAM =================
    private async Task<CommandResponse> HandleTeam(string[] parts, ChannelMessage message)
    {
        if (parts.Length >= 2 && parts[1] == "init")
        {
            var form = TaskFormBuilder.BuildTeamForm(message.ClanId!);

            return new CommandResponse(form); // 🔥 gửi form
        }

        return new CommandResponse("❌ Usage: !team init");
    }

    // ================= TASK =================
    private async Task<CommandResponse> HandleTask(
        string[] parts,
        ChannelMessage message,
        long userId)
    {
        var session = _sessionService.Get(userId);

        if (session == null)
            return new CommandResponse("❌ Bạn chưa có session");

        if (session.TeamId == null)
            return new CommandResponse("❌ Bạn phải tạo team trước");

        if (parts.Length >= 2 && parts[1] == "create")
        {
            session.Step = "TITLE";

            return new CommandResponse("📝 TẠO TASK\n👉 Nhập tiêu đề:");
        }

        return new CommandResponse("❌ Usage: !task create");
    }

    // ================= PARSE =================
    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (raw.StartsWith("{"))
        {
            try
            {
                var json = JsonDocument.Parse(raw);
                return json.RootElement.GetProperty("t").GetString();
            }
            catch { }
        }

        return raw;
    }
}