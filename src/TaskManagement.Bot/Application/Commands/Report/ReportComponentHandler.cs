using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands.Report;

public class ReportComponentHandler : IComponentHandler
{
    private readonly ILogger<ReportComponentHandler> _logger;
    private readonly IReportService _reportService;
    private readonly ReportStateService _stateService;

    public ReportComponentHandler(
        ILogger<ReportComponentHandler> logger,
        IReportService reportService,
        ReportStateService stateService)
    {
        _logger = logger;
        _reportService = reportService;
        _stateService = stateService;
    }

    public bool CanHandle(string customId)
    {
        return customId.StartsWith("REPORT_", StringComparison.OrdinalIgnoreCase) ||
               customId == "report_project_select" ||
               customId == "report_team_select";
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken ct)
    {
        try
        {
            var customId = context.CustomId;

            _logger.LogInformation(
                "[REPORT_COMPONENT] CustomId: {CustomId} | User: {UserId} | Clan: {ClanId}",
                customId,
                context.CurrentUserId,
                context.ClanId);

            if (customId == "report_project_select")
            {
                return await HandleProjectSelectAsync(context);
            }

            if (customId == "report_team_select")
            {
                return await HandleTeamSelectAsync(context);
            }

            var parts = customId.Split('|');
            var action = parts[0];

            return action switch
            {
                "REPORT_VIEW" => await HandleViewReportAsync(context, parts),
                "REPORT_CANCEL" => HandleCancel(context),
                _ => ComponentResponse.FromText(
                    context.ClanId ?? "",
                    context.ChannelId ?? "",
                    "❌ Unknown action",
                    context.Mode,
                    context.IsPublic,
                    context.MessageId ?? "")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPORT_COMPONENT] Error handling component");
            return ComponentResponse.FromText(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                $"❌ Lỗi: {ex.Message}",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }
    }

    private async Task<ComponentResponse> HandleProjectSelectAsync(ComponentContext context)
    {
        var selectedValue = ExtractDropdownValue(context.Payload);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Project selected: {Value} by user {UserId}",
            selectedValue,
            context.CurrentUserId);

        if (string.IsNullOrEmpty(selectedValue) || !int.TryParse(selectedValue, out var projectId))
        {
            return new ComponentResponse();
        }

        var userId = context.CurrentUserId ?? "";
        var clanId = context.ClanId ?? "";

        var state = _stateService.GetState(userId);
        var originalMessageId = state?.OriginalMessageId ?? context.MessageId ?? "";
        var originalMessage = state?.OriginalMessage;
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] OriginalMessageId from state: {OriginalMessageId}, context.MessageId: {ContextMessageId}",
            state?.OriginalMessageId,
            context.MessageId);

        var projects = await _reportService.GetPMProjectsAsync(userId);
        var project = projects.Projects.FirstOrDefault(p => p.ProjectId == projectId);

        if (project == null)
        {
            return new ComponentResponse();
        }

        var projectChanged = state?.ProjectId != projectId;
        
        _stateService.SetSelectedProject(userId, projectId, project.ProjectName, originalMessageId, originalMessage);
        
        if (projectChanged)
        {
            var currentState = _stateService.GetState(userId);
            if (currentState != null)
            {
                currentState.TeamId = null;
            }
        }

        var teams = await _reportService.GetTeamsByProjectAsync(projectId);
        var form = ReportFormBuilder.BuildReportFilterForm(projects, clanId, projectId, teams);

        var response = new ComponentResponse();

        if (!string.IsNullOrEmpty(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        _logger.LogInformation(
            "[REPORT_COMPONENT] Sending updated form with ReplyToMessageId: {ReplyToMessageId}",
            originalMessageId);

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId ?? "",
            ChannelId = context.ChannelId ?? "",
            Content = form,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = originalMessage
        });

        return response;
    }

    private Task<ComponentResponse> HandleTeamSelectAsync(ComponentContext context)
    {
        var selectedValue = ExtractDropdownValue(context.Payload);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Team selected: {Value} by user {UserId}",
            selectedValue,
            context.CurrentUserId);

        if (!string.IsNullOrEmpty(selectedValue) && int.TryParse(selectedValue, out var teamId))
        {
            var state = _stateService.GetState(context.CurrentUserId ?? "");
            if (state != null)
            {
                state.TeamId = teamId;
                
                _logger.LogInformation(
                    "[REPORT_COMPONENT] Saved team selection: TeamId={TeamId}",
                    teamId);
            }
        }

        return Task.FromResult(new ComponentResponse());
    }

    private async Task<ComponentResponse> HandleViewReportAsync(ComponentContext context, string[] parts)
    {
        var userId = context.CurrentUserId ?? "";
        var state = _stateService.GetState(userId);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] View report clicked by user {UserId}. ProjectId: {ProjectId}, TeamId: {TeamId}",
            userId,
            state?.ProjectId,
            state?.TeamId);

        if (state == null || state.ProjectId == 0)
        {
            return ComponentResponse.FromText(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Project trước",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        if (!state.TeamId.HasValue || state.TeamId.Value == 0)
        {
            return ComponentResponse.FromText(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Team trước",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        var originalMessageId = state.OriginalMessageId;
        var originalMessage = state.OriginalMessage;
        var teamId = state.TeamId.Value;
        var report = await _reportService.GetTeamDetailReportAsync(teamId);
        var form = ReportFormBuilder.BuildTeamDetailReportForm(report);

        _stateService.ClearState(userId);

        var response = new ComponentResponse();

        if (!string.IsNullOrEmpty(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId ?? "",
            ChannelId = context.ChannelId ?? "",
            Content = form,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = originalMessage
        });

        return response;
    }

    private ComponentResponse HandleCancel(ComponentContext context)
    {
        var userId = context.CurrentUserId ?? "";
        var state = _stateService.GetState(userId);
        var originalMessageId = state?.OriginalMessageId ?? context.MessageId ?? "";
        var originalMessage = state?.OriginalMessage;

        _stateService.ClearState(userId);

        var response = new ComponentResponse();

        if (!string.IsNullOrEmpty(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId ?? "",
            ChannelId = context.ChannelId ?? "",
            Text = "✅ Đã hủy báo cáo",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = originalMessage
        });

        return response;
    }

    private string? ExtractDropdownValue(JsonElement payload)
    {
        try
        {
            var rawPayload = payload.GetRawText();
            _logger.LogInformation("[REPORT_COMPONENT] Raw payload length: {Length} chars", rawPayload.Length);

            if (payload.TryGetProperty("MessageButtonClicked", out var buttonClicked))
            {
                if (buttonClicked.TryGetProperty("ExtraData", out var extraData))
                {
                    var value = extraData.GetString();
                    _logger.LogInformation("[REPORT_COMPONENT] Extracted value from 'MessageButtonClicked.ExtraData': {Value}", value);
                    return value;
                }
            }

            if (payload.TryGetProperty("values", out var values) &&
                values.ValueKind == JsonValueKind.Array)
            {
                var valuesArray = values.EnumerateArray().ToList();
                if (valuesArray.Count > 0)
                {
                    var value = valuesArray[0].GetString();
                    _logger.LogInformation("[REPORT_COMPONENT] Extracted value from 'values' array: {Value}", value);
                    return value;
                }
            }

            if (payload.TryGetProperty("data", out var data) &&
                data.TryGetProperty("values", out var dataValues) &&
                dataValues.ValueKind == JsonValueKind.Array)
            {
                var valuesArray = dataValues.EnumerateArray().ToList();
                if (valuesArray.Count > 0)
                {
                    var value = valuesArray[0].GetString();
                    _logger.LogInformation("[REPORT_COMPONENT] Extracted value from 'data.values' array: {Value}", value);
                    return value;
                }
            }

            if (payload.TryGetProperty("component_id", out var componentId) &&
                payload.TryGetProperty("value", out var valueElement))
            {
                var value = valueElement.GetString();
                _logger.LogInformation("[REPORT_COMPONENT] Extracted value from 'value' property: {Value}", value);
                return value;
            }

            _logger.LogWarning("[REPORT_COMPONENT] Could not extract dropdown value from payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPORT_COMPONENT] Error extracting dropdown value");
        }

        return null;
    }
}
