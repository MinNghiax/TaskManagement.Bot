using Microsoft.Extensions.Logging;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;
using System.Text.Json;

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

    public async Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(JsonSerializer.Serialize(message));
            var content = message.Content?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(content))
                return new CommandResponse("Empty command");

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return new CommandResponse("Usage: !report [me|team|today|week|month]");

            var sub = parts[1].ToLower();

            _logger.LogInformation($"[REPORT] Command received: {sub} from {message.SenderId}");

            return await (sub switch
            {
                "me" => Personal(message, ct),
                "team" => Team(message, ct),
                "today" => Statistics(message, ETimeRange.Today, ct),
                "week" => Statistics(message, ETimeRange.Week, ct),
                "month" => Statistics(message, ETimeRange.Month, ct),
                _ => Task.FromResult(new CommandResponse($"Unknown report command: {sub}. Use: me, team, today, week, month"))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report command error");
            return new CommandResponse($"Report error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> Personal(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation($"[PERSONAL] Server data: ClanNick='{message.ClanNick}' DisplayName='{message.DisplayName}' Username='{message.Username}' SenderId='{message.SenderId}'");
            _logger.LogInformation($"[PERSONAL] Channel context: ClanId={message.ClanId} ChannelId={message.ChannelId} Mode={message.Mode} IsPublic={message.IsPublic}");

            var identifiersToTry = new[]
            {
                (value: message.ClanNick, source: "ClanNick"),
                (value: message.DisplayName, source: "DisplayName"),
                (value: message.Username, source: "Username"),
                (value: message.SenderId, source: "SenderId"),
            }
            .Where(x => !string.IsNullOrWhiteSpace(x.value))
            .ToList();

            if (!identifiersToTry.Any())
            {
                _logger.LogWarning("[PERSONAL] No valid identifier found in message");
                return new CommandResponse("Cannot identify user - no valid identifier in message");
            }

            PersonalReportDto dto = new PersonalReportDto { TotalTasks = 0 };
            string? usedIdentifier = null;

            foreach (var (identifier, source) in identifiersToTry)
            {
                _logger.LogInformation($"[PERSONAL] Attempting lookup with {source}='{identifier}'");
                var result = await _reportService.GetPersonalReportAsync(identifier!, message.ClanId!, message.ChannelId!);

                if (result.TotalTasks > 0)
                {
                    dto = result;
                    usedIdentifier = identifier;
                    _logger.LogInformation($"[PERSONAL] ✓ Found tasks using {source}");
                    break;
                }
                else
                {
                    _logger.LogInformation($"[PERSONAL] ✗ No tasks found with {source}, trying next...");
                }
            }

            if (dto.TotalTasks == 0)
            {
                _logger.LogWarning($"[PERSONAL] No tasks found with any identifier");
            }

            _logger.LogInformation($"[PERSONAL] Final result: Total={dto.TotalTasks}, Completed={dto.CompletedTasks}, Doing={dto.DoingTasks}, Todo={dto.ToDoTasks}, Late={dto.LateTasks}, Rate={dto.CompletionRate:F2}%");

            var displayName = message.DisplayName ?? message.Username ?? message.SenderId ?? "Unknown User";
            var form = ReportFormBuilder.BuildPersonalReportForm(dto, displayName);
            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personal report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> Team(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation($"[TEAM] Getting team report for Clan: {message.ClanId} Channel: {message.ChannelId}");

            var dto = await _reportService.GetTeamReportAsync(
                message.ClanId!,
                message.ChannelId!);

            _logger.LogInformation($"[TEAM] Query result: Members={dto.TotalMembers}, Total={dto.TotalTasks}, Completed={dto.CompletedTasks}, Rate={dto.TeamCompletionRate:F2}%");

            var form = ReportFormBuilder.BuildTeamReportForm(dto, message.ChannelLabel);
            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> Statistics(
        ChannelMessage message,
        ETimeRange timeRange,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation($"[STATS] Getting statistics for {timeRange} in Clan: {message.ClanId} Channel: {message.ChannelId}");

            var dto = await _reportService.GetStatisticsReportAsync(
                timeRange,
                message.ClanId!,
                message.ChannelId!);

            _logger.LogInformation($"[STATS] Query result: Created={dto.TaskCreated}, Completed={dto.TaskCompleted}, Rate={dto.CompletionRate:F2}%");

            var form = ReportFormBuilder.BuildStatisticsReportForm(dto);
            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }
}
