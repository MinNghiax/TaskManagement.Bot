using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums; 
namespace TaskManagement.Bot.Application.Commands.Complain;

public class ComplainCommandHandler : ICommandHandler
{
    private readonly IComplainService _complainService;
    private readonly IMezonUserService _userService;
    private readonly ITaskService _taskService;

    public ComplainCommandHandler(IComplainService complainService, IMezonUserService userService, ITaskService taskService)
    {
        _complainService = complainService;
        _userService = userService;
        _taskService = taskService;
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

        //  Lọc thêm task Review
        var validTasks = tasks.Where(t => t.Status != ETaskStatus.Review).ToList();

        if (!validTasks.Any())
        {
            return new CommandResponse("❌ You have no tasks to complain about. (Tasks in Review status cannot be complained about)");
        }

        var options = validTasks
            .Select(t => (object)new { label = $"#{t.Id} {t.Title} [{t.Status}]", value = t.Id.ToString() })
            .ToArray();

        var formContent = ComplainFormBuilder.BuildComplainForm(options, userId);
        return new CommandResponse(formContent);
    }

    private async Task<CommandResponse> HandleApproveCommand(string userId, string clanId, CancellationToken ct)
    {
        var pendingComplains = await _complainService.GetPendingByPMAsync(userId, ct);

        if (!pendingComplains.Any())
        {
            return new CommandResponse("❌ No pending complaints to review.");
        }

        var taskIds = pendingComplains.Select(c => c.TaskItemId).Distinct();
        var taskDict = new Dictionary<int, TaskDto>();
        foreach (var taskId in taskIds)
        {
            var task = await _taskService.GetByIdAsync(taskId, ct);
            if (task != null)
            {
                taskDict[taskId] = task;
            }
        }

        var options = new List<object>();
        foreach (var c in pendingComplains)
        {
            var complainantName = await _userService.GetDisplayNameAsync(c.UserId, clanId, ct);
            var shortReason = c.Reason?.Length > 30 ? c.Reason.Substring(0, 27) + "..." : c.Reason;
            var createdAt = FormatDateWithVietnamTime(c.CreatedAt);

            // Tính số giờ gia hạn nếu là RequestExtend
            var extendInfo = "";
            if (c.Type == "RequestExtend" && c.NewDueDate.HasValue)
            {
                var task = taskDict.GetValueOrDefault(c.TaskItemId);
                if (task?.DueDate.HasValue == true)
                {
                    var hours = Math.Round((c.NewDueDate.Value - task.DueDate.Value).TotalHours, 1);
                    extendInfo = $"+{hours}h";
                }
            }

            options.Add(new
            {
                label = $"ID: {c.Id} || Task: {c.TaskTitle} || Type: {c.Type}  {extendInfo} || Reason: {shortReason} || By: {complainantName} || {createdAt}",
                value = c.Id.ToString()
            });
        }

        var formContent = ComplainFormBuilder.BuildApproveForm(options.ToArray(), userId);
        return new CommandResponse(formContent);
    }

    private string FormatDateWithVietnamTime(DateTime date)
    {
        var vietnamTime = date.AddHours(7);
        return vietnamTime.ToString("dd/MM/yyyy HH:mm");
    }
}