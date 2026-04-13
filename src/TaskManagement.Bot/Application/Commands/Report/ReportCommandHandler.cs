using Microsoft.Extensions.Logging;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.Report;

public class ReportCommandHandler : ICommandHandler
{
    private readonly ILogger<ReportCommandHandler> _logger;
    private readonly IReportService _reportService;

    public ReportCommandHandler(
        ILogger<ReportCommandHandler> logger,
        IReportService reportService)
    {
        _logger = logger;
        _reportService = reportService;
    }

    public bool CanHandle(string content)
    {
        return content.StartsWith("!report", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> HandleAsync(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            var content = message.Content?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(content))
                return "❌ Empty command";

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return "❌ Usage: !report [me|team|today|week|month]";

            var sub = parts[1].ToLower();

            return sub switch
            {
                "me" => await Personal(message, ct),
                "team" => await Team(message, ct),
                "today" => await Statistics(message, ETimeRange.Today, ct),
                "week" => await Statistics(message, ETimeRange.Week, ct),
                "month" => await Statistics(message, ETimeRange.Month, ct),
                _ => "❌ Unknown report command"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Report command error");
            return "❌ Error while generating report";
        }
    }

    private async Task<string> Personal(ChannelMessage message, CancellationToken ct)
    {
        var dto = await _reportService.GetPersonalReportAsync(
            message.Username!,
            message.ClanId!,
            message.ChannelId!,
            null);

        return $"""
📊 PERSONAL REPORT

👤 {dto.UserId}
📌 Total: {dto.TotalTasks}
✅ Completed: {dto.CompletedTasks}
🚧 Doing: {dto.DoingTasks}
⏳ Todo: {dto.ToDoTasks}
⚠️ Late: {dto.LateTasks}

📈 Completion: {dto.CompletionRate:F2}%
""";
    }

    private async Task<string> Team(ChannelMessage message, CancellationToken ct)
    {
        var dto = await _reportService.GetTeamReportAsync(
            message.ClanId!,
            message.ChannelId!,
            null);

        return $"""
👥 TEAM REPORT

👨 Members: {dto.TotalMembers}
📌 Tasks: {dto.TotalTasks}
✅ Done: {dto.CompletedTasks}
⚠️ Overdue: {dto.TotalOverdueTasks}

📈 Completion: {dto.TeamCompletionRate:F2}%
""";
    }

    private async Task<string> Statistics(
        ChannelMessage message,
        ETimeRange timeRange,
        CancellationToken ct)
    {
        var dto = await _reportService.GetStatisticsReportAsync(
            timeRange,
            message.ClanId!,
            message.ChannelId!,
            null);

        return $"""
📊 {timeRange.ToString().ToUpper()} REPORT

📌 Created: {dto.TaskCreated}
✅ Done: {dto.TaskCompleted}
🚧 Doing: {dto.TaskInProgress}
⏳ Pending: {dto.TaskPending}
⚠️ Overdue: {dto.OverdueTasks}

📈 Completion: {dto.CompletionRate:F2}%
""";
    }
}