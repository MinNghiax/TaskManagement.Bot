using System.Text.Json;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.Report;

public class ReportCommandHandler : ICommandHandler
{
    private static readonly HashSet<string> KnownSubCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "me",
        "team",
        "today",
        "week",
        "month",
        "task",
        "enhanced",
        "health",
        "analytics"
    };

    private readonly ILogger<ReportCommandHandler> _logger;
    private readonly IReportService _reportService;
    private readonly MezonClient _client;

    public ReportCommandHandler(
        ILogger<ReportCommandHandler> logger,
        IReportService reportService,
        MezonClient client)
    {
        _logger = logger;
        _reportService = reportService;
        _client = client;
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
            {
                return new CommandResponse("Empty command");
            }

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var mentionUserIds = (message.Mentions ?? Enumerable.Empty<ApiMessageMention>())
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .Select(x => x.UserId!)
                .ToArray();

            _logger.LogInformation(
                "[REPORT] Raw='{Content}' SenderId='{SenderId}' ClanId='{ClanId}' ChannelId='{ChannelId}' MentionCount={MentionCount} MentionUserIds=[{MentionUserIds}]",
                content,
                message.SenderId,
                message.ClanId,
                message.ChannelId,
                mentionUserIds.Length,
                string.Join(", ", mentionUserIds));

            var mentionResponse = await TryHandleMentionReportAsync(message, parts, ct);
            if (mentionResponse != null)
            {
                return mentionResponse;
            }

            if (parts.Length < 2)
            {
                return new CommandResponse(BuildUsageText());
            }

            var sub = parts[1].ToLowerInvariant();
            _logger.LogInformation("[REPORT] Command received: {SubCommand} from {SenderId}", sub, message.SenderId);

            return await (sub switch
            {
                "me" => Personal(message, ct),
                "team" => Team(message, ct),
                "today" => Statistics(message, ETimeRange.Today, ct),
                "week" => Statistics(message, ETimeRange.Week, ct),
                "month" => Statistics(message, ETimeRange.Month, ct),
                "task" => ComprehensiveTask(message, parts, ct),
                "enhanced" => EnhancedPersonal(message, ct),
                "health" => TeamHealth(message, parts, ct),
                "analytics" => Analytics(message, parts, ct),
                _ => Task.FromResult(new CommandResponse($"Unknown report command: {sub}\n\n{BuildUsageText()}"))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report command error");
            return new CommandResponse($"Report error: {ex.Message}");
        }
    }

    private async Task<CommandResponse?> TryHandleMentionReportAsync(
        ChannelMessage message,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 2)
        {
            return null;
        }

        var rawTargetToken = parts[1];
        var hasMentionMetadata = (message.Mentions ?? Enumerable.Empty<ApiMessageMention>())
            .Any(x => !string.IsNullOrWhiteSpace(x.UserId));

        var looksLikeMentionToken =
            rawTargetToken.StartsWith("@", StringComparison.Ordinal)
            || rawTargetToken.StartsWith("<@", StringComparison.Ordinal)
            || rawTargetToken.All(char.IsDigit);

        if (!looksLikeMentionToken && !(hasMentionMetadata && !KnownSubCommands.Contains(rawTargetToken)))
        {
            return null;
        }

        var (target, error) = await ResolveMentionTargetAsync(message, rawTargetToken, ct);
        if (target == null)
        {
            _logger.LogWarning(
                "[REPORT_TAG] Failed to resolve target. Token='{Token}' Error='{Error}'",
                rawTargetToken,
                error);

            return new CommandResponse(error ?? "Khong tim thay nguoi duoc tag.");
        }

        _logger.LogInformation(
            "[REPORT_TAG] Resolved token '{Token}' -> UserId='{UserId}' Username='{Username}' DisplayName='{DisplayName}' ClanNick='{ClanNick}'",
            rawTargetToken,
            target.UserId,
            target.Username,
            target.DisplayName,
            target.ClanNick);

        return await PersonalForTarget(message, target, ct);
    }

    private async Task<CommandResponse> Personal(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "[PERSONAL] Server data: ClanNick='{ClanNick}' DisplayName='{DisplayName}' Username='{Username}' SenderId='{SenderId}'",
                message.ClanNick,
                message.DisplayName,
                message.Username,
                message.SenderId);

            _logger.LogInformation(
                "[PERSONAL] Channel context: ClanId={ClanId} ChannelId={ChannelId} Mode={Mode} IsPublic={IsPublic}",
                message.ClanId,
                message.ChannelId,
                message.Mode,
                message.IsPublic);

            var target = new ReportTarget
            {
                UserId = message.SenderId,
                Username = message.Username,
                DisplayName = message.DisplayName,
                ClanNick = message.ClanNick
            };

            return await PersonalForTarget(message, target, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personal report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> PersonalForTarget(
        ChannelMessage message,
        ReportTarget target,
        CancellationToken ct)
    {
        var identifiersToTry = BuildIdentifiersToTry(target);
        if (identifiersToTry.Count == 0)
        {
            _logger.LogWarning("[PERSONAL] No valid identifier found for target");
            return new CommandResponse("Cannot identify user - no valid identifier in message");
        }

        PersonalReportDto dto = new() { TotalTasks = 0 };

        foreach (var (identifier, source) in identifiersToTry)
        {
            _logger.LogInformation(
                "[PERSONAL] Attempting lookup with {Source}='{Identifier}' for target display '{DisplayName}'",
                source,
                identifier,
                target.DisplayLabel);

            var result = await _reportService.GetPersonalReportAsync(
                identifier,
                message.ClanId,
                message.ChannelId);

            if (result.TotalTasks > 0)
            {
                dto = result;
                _logger.LogInformation("[PERSONAL] Found {TotalTasks} tasks using {Source}", result.TotalTasks, source);
                break;
            }

            _logger.LogInformation("[PERSONAL] No tasks found with {Source}, trying next...", source);
        }

        if (dto.TotalTasks == 0)
        {
            _logger.LogWarning(
                "[PERSONAL] No tasks found for target UserId='{UserId}' in ClanId='{ClanId}' ChannelId='{ChannelId}'",
                target.UserId,
                message.ClanId,
                message.ChannelId);
        }

        _logger.LogInformation(
            "[PERSONAL] Final result for '{DisplayName}': Total={Total} Completed={Completed} Doing={Doing} Todo={Todo} Late={Late} Rate={Rate:F2}%",
            target.DisplayLabel,
            dto.TotalTasks,
            dto.CompletedTasks,
            dto.DoingTasks,
            dto.ToDoTasks,
            dto.LateTasks,
            dto.CompletionRate);

        var form = ReportFormBuilder.BuildPersonalReportForm(dto, target.DisplayLabel);
        return new CommandResponse(form);
    }

    private async Task<TargetResolutionResult> ResolveMentionTargetAsync(
        ChannelMessage message,
        string rawToken,
        CancellationToken ct)
    {
        var userMentions = (message.Mentions ?? Enumerable.Empty<ApiMessageMention>())
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .ToList();

        _logger.LogInformation(
            "[REPORT_TAG] Resolving token='{Token}' from mentions payload: {Payload}",
            rawToken,
            JsonSerializer.Serialize(userMentions));

        if (userMentions.Count > 1)
        {
            return new TargetResolutionResult(
                null,
                "Hay tag dung 1 nguoi trong lenh !report @tag.");
        }

        if (userMentions.Count == 1)
        {
            var mention = userMentions[0];
            var target = new ReportTarget
            {
                UserId = mention.UserId,
                Username = mention.Username,
                RawToken = rawToken
            };

            await EnrichTargetAsync(message.ClanId, target, ct);
            return new TargetResolutionResult(target, null);
        }

        var extractedUserId = ExtractUserId(rawToken);
        if (!string.IsNullOrWhiteSpace(extractedUserId))
        {
            var target = new ReportTarget
            {
                UserId = extractedUserId,
                RawToken = rawToken
            };

            await EnrichTargetAsync(message.ClanId, target, ct);
            return new TargetResolutionResult(target, null);
        }

        if (string.IsNullOrWhiteSpace(message.ClanId))
        {
            return new TargetResolutionResult(
                null,
                "Khong tim thay clan hien tai de resolve user duoc tag.");
        }

        var clan = _client.Clans.Get(message.ClanId) ?? await _client.GetClanAsync(message.ClanId, ct);
        if (clan == null)
        {
            return new TargetResolutionResult(
                null,
                $"Khong load duoc clan {message.ClanId} de tim user duoc tag.");
        }

        var alias = NormalizeAlias(rawToken);
        if (string.IsNullOrWhiteSpace(alias))
        {
            return new TargetResolutionResult(
                null,
                $"Token '{rawToken}' khong phai mention hop le.");
        }

        await clan.Users.FetchAllAsync(ct);
        var matches = clan.Users.GetAll()
            .Where(user => MatchesAlias(user, alias))
            .DistinctBy(user => user.Id)
            .ToList();

        _logger.LogInformation(
            "[REPORT_TAG] Alias lookup for '{Alias}' returned {Count} user(s): [{Users}]",
            alias,
            matches.Count,
            string.Join(", ", matches.Select(x => $"{x.Id}:{x.ClanNick ?? x.DisplayName ?? x.Username}")));

        if (matches.Count == 0)
        {
            return new TargetResolutionResult(
                null,
                $"Khong tim thay user nao khop voi tag '{rawToken}'.");
        }

        if (matches.Count > 1)
        {
            return new TargetResolutionResult(
                null,
                $"Tag '{rawToken}' dang trung nhieu nguoi. Hay mention truc tiep dung 1 user.");
        }

        var matchedUser = matches[0];
        return new TargetResolutionResult(
            new ReportTarget
            {
                UserId = matchedUser.Id,
                Username = matchedUser.Username,
                DisplayName = matchedUser.DisplayName,
                ClanNick = matchedUser.ClanNick,
                RawToken = rawToken
            },
            null);
    }

    private async Task EnrichTargetAsync(string? clanId, ReportTarget target, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clanId) || string.IsNullOrWhiteSpace(target.UserId))
        {
            return;
        }

        try
        {
            var clan = _client.Clans.Get(clanId) ?? await _client.GetClanAsync(clanId, ct);
            if (clan == null)
            {
                return;
            }

            var user = clan.Users.Get(target.UserId) ?? await clan.Users.FetchAsync(target.UserId, ct);
            if (user == null)
            {
                return;
            }

            target.Username ??= user.Username;
            target.DisplayName ??= user.DisplayName;
            target.ClanNick ??= user.ClanNick;

            _logger.LogInformation(
                "[REPORT_TAG] Enriched target UserId='{UserId}' -> Username='{Username}' DisplayName='{DisplayName}' ClanNick='{ClanNick}'",
                target.UserId,
                target.Username,
                target.DisplayName,
                target.ClanNick);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[REPORT_TAG] Failed to enrich target info for user {UserId}", target.UserId);
        }
    }

    private static List<(string value, string source)> BuildIdentifiersToTry(ReportTarget target)
    {
        var candidates = new (string? value, string source)[]
        {
            (target.UserId, "UserId"),
            (target.ClanNick, "ClanNick"),
            (target.DisplayName, "DisplayName"),
            (target.Username, "Username"),
            (target.RawToken, "RawToken"),
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<(string value, string source)>();

        foreach (var (value, source) in candidates)
        {
            var normalized = NormalizeIdentifier(value);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            result.Add((normalized, source));
        }

        return result;
    }

    private static string? ExtractUserId(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var trimmed = rawToken.Trim();

        if (trimmed.StartsWith("<@", StringComparison.Ordinal) && trimmed.EndsWith(">", StringComparison.Ordinal))
        {
            var candidate = trimmed[2..^1];
            return candidate.All(char.IsDigit) ? candidate : null;
        }

        return trimmed.All(char.IsDigit) ? trimmed : null;
    }

    private static string NormalizeAlias(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim()
            .TrimStart('@')
            .Trim()
            .ToLowerInvariant();
    }

    private static string? NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith("<@", StringComparison.Ordinal) && trimmed.EndsWith(">", StringComparison.Ordinal))
        {
            return trimmed[2..^1];
        }

        if (trimmed.StartsWith("@", StringComparison.Ordinal))
        {
            return trimmed[1..].Trim();
        }

        return trimmed;
    }

    private static bool MatchesAlias(Mezon.Sdk.Structures.User user, string alias)
    {
        return string.Equals(NormalizeAlias(user.Username), alias, StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizeAlias(user.DisplayName), alias, StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizeAlias(user.ClanNick), alias, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUsageText()
    {
        return """
Usage:
!report @tag - Personal report for tagged user in this channel
!report me - Personal report
!report team - Team report
!report today/week/month - Statistics
!report task <id> - Task details
!report enhanced - Enhanced personal
!report health <teamId> - Team health
!report analytics [today/week/month] - Analytics
""";
    }

    private async Task<CommandResponse> Team(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[TEAM] Getting team report for Clan: {ClanId} Channel: {ChannelId}", message.ClanId, message.ChannelId);

            var dto = await _reportService.GetTeamReportAsync(
                message.ClanId!,
                message.ChannelId!);

            _logger.LogInformation(
                "[TEAM] Query result: Members={Members}, Total={Total}, Completed={Completed}, Rate={Rate:F2}%",
                dto.TotalMembers,
                dto.TotalTasks,
                dto.CompletedTasks,
                dto.TeamCompletionRate);

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
            _logger.LogInformation(
                "[STATS] Getting statistics for {TimeRange} in Clan: {ClanId} Channel: {ChannelId}",
                timeRange,
                message.ClanId,
                message.ChannelId);

            var dto = await _reportService.GetStatisticsReportAsync(
                timeRange,
                message.ClanId!,
                message.ChannelId!);

            _logger.LogInformation(
                "[STATS] Query result: Created={Created}, Completed={Completed}, Rate={Rate:F2}%",
                dto.TaskCreated,
                dto.TaskCompleted,
                dto.CompletionRate);

            var form = ReportFormBuilder.BuildStatisticsReportForm(dto);
            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> ComprehensiveTask(
        ChannelMessage message,
        string[] parts,
        CancellationToken ct)
    {
        try
        {
            if (parts.Length < 3)
            {
                return new CommandResponse("Usage: !report task <taskId>");
            }

            if (!int.TryParse(parts[2], out var taskId))
            {
                return new CommandResponse($"Invalid task ID: {parts[2]}");
            }

            _logger.LogInformation("[COMPREHENSIVE] Getting comprehensive report for task {TaskId}", taskId);

            var report = await _reportService.GetComprehensiveTaskReportAsync(taskId);
            var form = ReportFormBuilder.BuildComprehensiveTaskReport(report);

            _logger.LogInformation("[COMPREHENSIVE] Retrieved task: {Title}, Status={Status}", report.Title, report.Status);

            return new CommandResponse(form);
        }
        catch (KeyNotFoundException)
        {
            return new CommandResponse("Task not found. Please check the task ID.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comprehensive task report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> EnhancedPersonal(
        ChannelMessage message,
        CancellationToken ct)
    {
        try
        {
            var identifiersToTry = BuildIdentifiersToTry(new ReportTarget
            {
                UserId = message.SenderId,
                Username = message.Username,
                DisplayName = message.DisplayName,
                ClanNick = message.ClanNick
            });

            if (!identifiersToTry.Any())
            {
                return new CommandResponse("Cannot identify user - no valid identifier in message");
            }

            EnhancedPersonalTaskReportDto? report = null;
            string? usedIdentifier = null;

            foreach (var (identifier, source) in identifiersToTry)
            {
                _logger.LogInformation("[ENHANCED] Attempting lookup with {Source}='{Identifier}'", source, identifier);
                var result = await _reportService.GetEnhancedPersonalReportAsync(identifier, message.ClanId, message.ChannelId);

                if (result.TotalTasks > 0)
                {
                    report = result;
                    usedIdentifier = identifier;
                    _logger.LogInformation("[ENHANCED] Found {TotalTasks} tasks", result.TotalTasks);
                    break;
                }
            }

            if (report == null || report.TotalTasks == 0)
            {
                report = new EnhancedPersonalTaskReportDto { UserId = usedIdentifier ?? message.SenderId ?? "Unknown" };
            }

            var displayName = message.DisplayName ?? message.Username ?? message.SenderId ?? "User";
            var form = ReportFormBuilder.BuildEnhancedPersonalTaskReport(report, displayName);
            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced personal report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> TeamHealth(
        ChannelMessage message,
        string[] parts,
        CancellationToken ct)
    {
        try
        {
            if (parts.Length < 3)
            {
                return new CommandResponse("Usage: !report health <teamId>");
            }

            if (!int.TryParse(parts[2], out var teamId))
            {
                return new CommandResponse($"Invalid team ID: {parts[2]}");
            }

            _logger.LogInformation("[HEALTH] Getting team health report for team {TeamId}", teamId);

            var report = await _reportService.GetTeamHealthReportAsync(teamId);
            var form = ReportFormBuilder.BuildTeamHealthReport(report);

            _logger.LogInformation(
                "[HEALTH] Team health: {HealthStatus}, Rate={Rate:F2}%",
                report.TeamHealthStatus,
                report.TeamCompletionRate);

            return new CommandResponse(form);
        }
        catch (KeyNotFoundException)
        {
            return new CommandResponse("Team not found. Please check the team ID.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team health report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private async Task<CommandResponse> Analytics(
        ChannelMessage message,
        string[] parts,
        CancellationToken ct)
    {
        try
        {
            var timeRange = ETimeRange.Today;

            if (parts.Length >= 3)
            {
                var period = parts[2].ToLowerInvariant();
                timeRange = period switch
                {
                    "today" => ETimeRange.Today,
                    "week" => ETimeRange.Week,
                    "month" => ETimeRange.Month,
                    _ => ETimeRange.Today
                };
            }

            _logger.LogInformation("[ANALYTICS] Getting analytics for {TimeRange}", timeRange);

            var report = await _reportService.GetTaskAnalyticsReportAsync(timeRange, message.ClanId, message.ChannelId);
            var form = ReportFormBuilder.BuildTaskAnalyticsReport(report);

            _logger.LogInformation(
                "[ANALYTICS] Created={Created}, Completed={Completed}, Rate={Rate:F2}%",
                report.TasksCreated,
                report.TasksCompleted,
                report.DeliveryRate);

            return new CommandResponse(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics report");
            return new CommandResponse($"Error: {ex.Message}");
        }
    }

    private sealed record ReportTarget
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? ClanNick { get; set; }
        public string? RawToken { get; set; }

        public string DisplayLabel => ClanNick ?? DisplayName ?? Username ?? UserId ?? "Unknown User";
    }

    private sealed record TargetResolutionResult(ReportTarget? Target, string? Error);
}
