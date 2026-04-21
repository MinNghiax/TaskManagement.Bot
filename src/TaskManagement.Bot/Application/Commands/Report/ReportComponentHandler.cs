using System.Text.Json;
using Mezon.Sdk.Domain;
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
                "[REPORT_COMPONENT] CustomId: {CustomId} | User: {UserId} | Clan: {ClanId} | MessageId: {MessageId}",
                customId,
                context.CurrentUserId,
                context.ClanId,
                context.MessageId);

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
            
            _logger.LogInformation(
                "[REPORT_COMPONENT] Button action: {Action} | Parts: {Parts}",
                action,
                string.Join(", ", parts));

            if (action == "REPORT_VIEW" || action == "REPORT_CANCEL")
            {
                var userId = context.CurrentUserId ?? "";
                var state = _stateService.GetState(userId);

                if (state == null || !state.OwnerUserId.Equals(userId))
                {
                    _logger.LogWarning(
                        "[REPORT_COMPONENT] User {UserId} tried to interact with unauthorized form. State exists: {StateExists}, Owner: {OwnerId}",
                        userId,
                        state != null,
                        state?.OwnerUserId ?? "N/A");

                    return ComponentResponse.FromText(
                        context.ClanId ?? "",
                        context.ChannelId ?? "",
                        "❌ Bạn không có quyền thao tác với form này",
                        context.Mode,
                        context.IsPublic,
                        context.MessageId ?? "");
                }
            }

            return action switch
            {
                "REPORT_VIEW" => await HandleViewReportAsync(context, parts),
                "REPORT_CANCEL" => HandleCancel(context, parts),
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
        var userId = context.CurrentUserId ?? "";
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Project selected: {Value} by user {UserId}",
            selectedValue,
            userId);

        var state = _stateService.GetState(userId);
        if (state == null)
        {
            _logger.LogWarning("[REPORT_COMPONENT] No state found for user {UserId}", userId);
            return new ComponentResponse();
        }

        if (!state.OwnerUserId.Equals(userId))
        {
            _logger.LogWarning(
                "[REPORT_COMPONENT] User {UserId} tried to select project in form owned by {OwnerId}",
                userId,
                state.OwnerUserId);
            return new ComponentResponse();
        }

        if (string.IsNullOrEmpty(selectedValue) || !int.TryParse(selectedValue, out var projectId))
        {
            return new ComponentResponse();
        }

        var clanId = context.ClanId ?? "";
        var channelId = context.ChannelId ?? "";
        var formMessageId = context.MessageId ?? "";

        var originalMessageId = state.OriginalMessageId ?? "";
        var originalMessage = state.OriginalMessage;
        
        _stateService.SetFormMessageId(userId, formMessageId);

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
        response.UpdateMessage(
            clanId,
            channelId,
            formMessageId,
            form,
            context.Mode,
            context.IsPublic);

        _logger.LogInformation(
            "[REPORT_COMPONENT] ✅ Queued update for form message {MessageId} with team options",
            formMessageId);

        return response;
    }

    private Task<ComponentResponse> HandleTeamSelectAsync(ComponentContext context)
    {
        var selectedValue = ExtractDropdownValue(context.Payload);
        var userId = context.CurrentUserId ?? "";
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Team selected: {Value} by user {UserId}",
            selectedValue,
            userId);

        // ⭐ VALIDATE OWNERSHIP
        var state = _stateService.GetState(userId);
        if (state == null)
        {
            _logger.LogWarning("[REPORT_COMPONENT] No state found for user {UserId}", userId);
            return Task.FromResult(new ComponentResponse());
        }

        if (!state.OwnerUserId.Equals(userId))
        {
            _logger.LogWarning(
                "[REPORT_COMPONENT] User {UserId} tried to select team in form owned by {OwnerId}",
                userId,
                state.OwnerUserId);
            return Task.FromResult(new ComponentResponse());
        }

        if (!string.IsNullOrEmpty(selectedValue) && int.TryParse(selectedValue, out var teamId))
        {
            state.TeamId = teamId;
            
            var formMessageId = context.MessageId ?? "";
            _stateService.SetFormMessageId(userId, formMessageId);
            
            _logger.LogInformation(
                "[REPORT_COMPONENT] Saved team selection: TeamId={TeamId}, FormMessageId={FormMessageId}",
                teamId,
                formMessageId);
        }

        return Task.FromResult(new ComponentResponse());
    }

    private async Task<ComponentResponse> HandleViewReportAsync(ComponentContext context, string[] parts)
    {
        var userId = context.CurrentUserId ?? "";
        var state = _stateService.GetState(userId);
        
        var clanId = (!string.IsNullOrEmpty(context.ClanId) && context.ClanId != "0") 
            ? context.ClanId 
            : (parts.Length > 1 ? parts[1] : "");
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] View report clicked by user {UserId}. ProjectId: {ProjectId}, TeamId: {TeamId}, FormMessageId: {FormMessageId}, ClanId: {ClanId}",
            userId,
            state?.ProjectId,
            state?.TeamId,
            state?.FormMessageId,
            clanId);

        if (state == null || state.ProjectId == 0)
        {
            _logger.LogWarning("[REPORT_COMPONENT] State is null or ProjectId is 0");
            return ComponentResponse.FromText(
                clanId,
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Project trước",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        if (!state.TeamId.HasValue || state.TeamId.Value == 0)
        {
            _logger.LogWarning("[REPORT_COMPONENT] TeamId is null or 0");
            return ComponentResponse.FromText(
                clanId,
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Team trước",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        var formMessageId = !string.IsNullOrEmpty(state.FormMessageId) 
            ? state.FormMessageId 
            : context.MessageId ?? "";
        
        if (string.IsNullOrEmpty(formMessageId))
        {
            _logger.LogError("[REPORT_COMPONENT] FormMessageId is empty!");
            return ComponentResponse.FromText(
                clanId,
                context.ChannelId ?? "",
                "❌ Lỗi: Không tìm thấy form message",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        var teamId = state.TeamId.Value;
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Fetching report for team {TeamId}, will update message {MessageId}",
            teamId,
            formMessageId);
        var report = await _reportService.GetTeamDetailReportAsync(teamId);
        
        var form = ReportFormBuilder.BuildTeamDetailReportForm(report);

        var response = new ComponentResponse();
        response.UpdateMessage(
            clanId,  // ⭐ Use fixed clanId
            context.ChannelId ?? "",
            formMessageId,
            form,
            context.Mode,
            context.IsPublic,
            onSuccess: () =>
            {
                // ⭐ Clear state SAU KHI update thành công
                _stateService.ClearState(userId);
                _logger.LogInformation(
                    "[REPORT_COMPONENT] ✅ Cleared state for user {UserId} after successful report display",
                    userId);
            });

        _logger.LogInformation(
            "[REPORT_COMPONENT] ✅ Queued update for form message {MessageId} with report result",
            formMessageId);

        return response;
    }

    private ComponentResponse HandleCancel(ComponentContext context, string[] parts)
    {
        var userId = context.CurrentUserId ?? "";
        var state = _stateService.GetState(userId);

        var clanId = (!string.IsNullOrEmpty(context.ClanId) && context.ClanId != "0") 
            ? context.ClanId 
            : (parts.Length > 1 ? parts[1] : "");

        var formMessageId = !string.IsNullOrEmpty(state?.FormMessageId) 
            ? state.FormMessageId 
            : context.MessageId ?? "";

        _logger.LogInformation(
            "[REPORT_COMPONENT] Cancel clicked by user {UserId}. ClanId: {ClanId}, FormMessageId: {FormMessageId}",
            userId,
            clanId,
            formMessageId);

        var content = new ChannelMessageContent
        {
            Text = "✅ Đã hủy báo cáo"
        };

        var response = new ComponentResponse();

        if (!string.IsNullOrEmpty(formMessageId))
        {
            response.UpdateMessage(
                clanId,
                context.ChannelId ?? "",
                formMessageId,
                content,
                context.Mode,
                context.IsPublic,
                onSuccess: () =>
                {
                    _stateService.ClearState(userId);
                    _logger.LogInformation(
                        "[REPORT_COMPONENT] ✅ Cleared state for user {UserId} after cancel",
                        userId);
                });
        }

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
