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
               customId == "selected_project" ||
               customId == "selected_team";
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

            if (customId == "selected_project")
            {
                return await HandleProjectDropdownAsync(context);
            }

            if (customId == "selected_team")
            {
                return await HandleTeamDropdownAsync(context);
            }

            var parts = customId.Split('|');
            var action = parts[0];

            return action switch
            {
                "REPORT_SELECT_PROJECT" => await HandleSelectProjectAsync(context, parts),
                "REPORT_SELECT_TEAM" => await HandleSelectTeamAsync(context, parts),
                "REPORT_BACK_PROJECT" => await HandleBackToProjectAsync(context, parts),
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

    private async Task<ComponentResponse> HandleProjectDropdownAsync(ComponentContext context)
    {
        var selectedValue = ExtractDropdownValue(context.Payload);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Project dropdown selected: {Value} by user {UserId}",
            selectedValue,
            context.CurrentUserId);

        if (!string.IsNullOrEmpty(selectedValue) && int.TryParse(selectedValue, out var projectId))
        {
            var projects = await _reportService.GetPMProjectsAsync(context.CurrentUserId ?? "");
            var project = projects.Projects.FirstOrDefault(p => p.ProjectId == projectId);

            if (project != null)
            {
                _stateService.SetSelectedProject(context.CurrentUserId ?? "", projectId, project.ProjectName);
                
                _logger.LogInformation(
                    "[REPORT_COMPONENT] Saved project selection: ProjectId={ProjectId}, ProjectName={ProjectName}",
                    projectId,
                    project.ProjectName);
            }
        }

        return new ComponentResponse();
    }

    private Task<ComponentResponse> HandleTeamDropdownAsync(ComponentContext context)
    {
        var selectedValue = ExtractDropdownValue(context.Payload);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Team dropdown selected: {Value} by user {UserId}",
            selectedValue,
            context.CurrentUserId);

        if (!string.IsNullOrEmpty(selectedValue) && int.TryParse(selectedValue, out var teamId))
        {
            var state = _stateService.GetState(context.CurrentUserId ?? "");
            if (state != null)
            {
                state.TeamId = teamId;
                
                _logger.LogInformation(
                    "[REPORT_COMPONENT] Updated state with TeamId={TeamId}",
                    teamId);
            }
        }

        return Task.FromResult(new ComponentResponse());
    }

    private async Task<ComponentResponse> HandleSelectProjectAsync(ComponentContext context, string[] parts)
    {
        var clanId = parts.Length > 1 ? parts[1] : context.ClanId ?? "";
        var userId = context.CurrentUserId ?? "";

        var state = _stateService.GetState(userId);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Button 'Tiếp tục' clicked by user {UserId}. State: {HasState}",
            userId,
            state != null);

        if (state == null || state.ProjectId == 0)
        {
            _logger.LogWarning(
                "[REPORT_COMPONENT] No project selected in state for user {UserId}",
                userId);
            
            return ComponentResponse.FromText(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Project từ dropdown trước khi nhấn 'Tiếp tục'",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        var projectId = state.ProjectId;
        var projectName = state.ProjectName;

        _logger.LogInformation(
            "[REPORT_COMPONENT] Selected project from state: ProjectId={ProjectId}, ProjectName={ProjectName}",
            projectId,
            projectName);

        var teams = await _reportService.GetTeamsByProjectAsync(projectId);
        var form = ReportFormBuilder.BuildTeamSelectionForm(projectId, projectName, teams, clanId);

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
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private async Task<ComponentResponse> HandleSelectTeamAsync(ComponentContext context, string[] parts)
    {
        var userId = context.CurrentUserId ?? "";

        var state = _stateService.GetState(userId);
        
        _logger.LogInformation(
            "[REPORT_COMPONENT] Button 'Xem báo cáo' clicked by user {UserId}. State: {HasState}, TeamId: {TeamId}",
            userId,
            state != null,
            state?.TeamId);

        if (state == null || !state.TeamId.HasValue || state.TeamId.Value == 0)
        {
            _logger.LogWarning(
                "[REPORT_COMPONENT] No team selected in state for user {UserId}",
                userId);
            
            return ComponentResponse.FromText(
                context.ClanId ?? "",
                context.ChannelId ?? "",
                "❌ Vui lòng chọn Team từ dropdown trước khi nhấn 'Xem báo cáo'",
                context.Mode,
                context.IsPublic,
                context.MessageId ?? "");
        }

        var teamId = state.TeamId.Value;

        _logger.LogInformation(
            "[REPORT_COMPONENT] Selected team from state: TeamId={TeamId}",
            teamId);

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
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private async Task<ComponentResponse> HandleBackToProjectAsync(ComponentContext context, string[] parts)
    {
        var clanId = parts.Length > 1 ? parts[1] : context.ClanId ?? "";

        var report = await _reportService.GetPMProjectsAsync(context.CurrentUserId ?? "");
        var form = ReportFormBuilder.BuildPMProjectSelectionForm(report, clanId);

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
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private ComponentResponse HandleCancel(ComponentContext context)
    {
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
            ReplyToMessageId = context.MessageId
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
    
    private string? ExtractFormValue(JsonElement payload, string fieldId)
    {
        try
        {
            if (payload.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content) &&
                content.TryGetProperty("embed", out var embed) &&
                embed.ValueKind == JsonValueKind.Array)
            {
                var embedArray = embed.EnumerateArray().ToList();
                if (embedArray.Count > 0 &&
                    embedArray[0].TryGetProperty("fields", out var fields) &&
                    fields.ValueKind == JsonValueKind.Array)
                {
                    foreach (var field in fields.EnumerateArray())
                    {
                        if (field.TryGetProperty("inputs", out var inputs) &&
                            inputs.TryGetProperty("id", out var id) &&
                            id.GetString() == fieldId &&
                            inputs.TryGetProperty("value", out var value))
                        {
                            return value.GetString();
                        }
                    }
                }
            }

            if (payload.TryGetProperty("values", out var values) &&
                values.ValueKind == JsonValueKind.Array)
            {
                var valuesArray = values.EnumerateArray().ToList();
                if (valuesArray.Count > 0)
                {
                    return valuesArray[0].GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[REPORT_COMPONENT] Failed to extract form value for {FieldId}", fieldId);
        }

        return null;
    }
}
