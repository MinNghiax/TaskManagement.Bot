using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;  // ✅ THÊM: using cho ETaskStatus

namespace TaskManagement.Bot.Application.Commands.Complain;

public class ComplainCommandHandler : ICommandHandler
{
    private readonly IComplainService _complainService;
    private readonly IMezonUserService _userService;

    public ComplainCommandHandler(IComplainService complainService, IMezonUserService userService)
    {
        _complainService = complainService;
        _userService = userService;
    }

    public bool CanHandle(string command)
    {
        var trimmed = command.Trim();
        return trimmed.Equals("!complain", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("!complain ", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("!approve", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("!approve ", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken cancellationToken)
    {
        var command = message.Content?.Text?.Trim() ?? "";
        var userId = message.SenderId!;
        var clanId = message.ClanId!;

        // Handle !approve command
        if (command.Equals("!approve", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleApproveCommand(userId, clanId, cancellationToken);
        }

        // Handle !complain command
        return await HandleComplainCommand(userId, cancellationToken);
    }

    private async Task<CommandResponse> HandleComplainCommand(string userId, CancellationToken ct)
    {
        var tasks = await _complainService.GetComplainableTasksAsync(userId, ct);

        if (!tasks.Any())
        {
            return new CommandResponse("❌ You have no tasks to complain about.");
        }

        // ✅ THÊM: Lọc thêm task Review ở đây (double-check)
        var validTasks = tasks.Where(t => t.Status != ETaskStatus.Review).ToList();

        if (!validTasks.Any())
        {
            return new CommandResponse("❌ You have no tasks to complain about. (Tasks in Review status cannot be complained about)");
        }

        var options = validTasks
            .Select(t => (object)new { label = $"#{t.Id} {t.Title} [{t.Status}]", value = t.Id.ToString() })
            .ToArray();

        var formContent = ComplainFormBuilder.BuildComplainForm(options);
        return new CommandResponse(formContent);
    }

    private async Task<CommandResponse> HandleApproveCommand(string userId, string clanId, CancellationToken ct)
    {
        var pendingComplains = await _complainService.GetPendingByPMAsync(userId, ct);

        if (!pendingComplains.Any())
        {
            return new CommandResponse("❌ No pending complaints to review.");
        }

        var options = new List<object>();
        foreach (var c in pendingComplains)
        {
            var complainantName = await _userService.GetDisplayNameAsync(c.UserId, clanId, ct);
            options.Add(new
            {
                label = $"#{c.Id} - {c.TaskTitle} [{c.Type}] - From: {complainantName}",
                value = c.Id.ToString()
            });
        }

        var formContent = ComplainFormBuilder.BuildApproveForm(options.ToArray());
        return new CommandResponse(formContent);
    }
}