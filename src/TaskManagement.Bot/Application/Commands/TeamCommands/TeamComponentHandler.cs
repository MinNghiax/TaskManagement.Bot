using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public class TeamComponentHandler : IComponentHandler
{
    private readonly ILogger<TeamComponentHandler> _logger;
    private readonly ITeamService _teamService;
    private readonly MezonClient _client;
    private readonly Dictionary<string, PendingTeamRequest> _pendingRequests;

    public TeamComponentHandler(
        ILogger<TeamComponentHandler> logger,
        ITeamService teamService,
        MezonClient client)
    {
        _logger = logger;
        _teamService = teamService;
        _client = client;
        _pendingRequests = new Dictionary<string, PendingTeamRequest>();
    }

    public bool CanHandle(string customId)
    {
        return customId.StartsWith("CREATE_TEAM", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("CANCEL_TEAM", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("ACCEPT", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.ClanId) || string.IsNullOrWhiteSpace(context.ChannelId))
        {
            return new ComponentResponse();
        }

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length == 0)
        {
            return new ComponentResponse();
        }

        return parts[0].ToUpperInvariant() switch
        {
            "CREATE_TEAM" => await HandleCreateAsync(context, cancellationToken),
            "CANCEL_TEAM" => BuildCancelResponse(context),
            "ACCEPT" => await HandleAcceptAsync(context, parts, cancellationToken),
            "REJECT" => await HandleRejectAsync(context, parts, cancellationToken),
            _ => new ComponentResponse()
        };
    }

    private static ComponentResponse BuildCancelResponse(ComponentContext context)
    {
        var response = ComponentResponse.FromText(
            context.ClanId!,
            context.ChannelId!,
            "❌ Đã hủy tạo team",
            context.Mode,
            context.IsPublic);

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId!,
                context.ChannelId!,
                context.MessageId,
                context.Mode,
                context.IsPublic);
        }

        return response;
    }

    private async Task<ComponentResponse> HandleCreateAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        var projectName = ReadValue(context.Payload, "project_name");
        var teamName = ReadValue(context.Payload, "team_name");
        var membersRaw = ReadValue(context.Payload, "members");

        // Validate với số lượng thành viên 3-6
        var (isValid, message) = TeamFormBuilder.ValidateForm(projectName, teamName, "PM", membersRaw);
        if (!isValid)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, message, context.Mode, context.IsPublic);
        }

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Không xác định được người tạo team", context.Mode, context.IsPublic);
        }

        // Extract member IDs
        var memberIds = TeamFormBuilder.ExtractMemberIds(membersRaw);

        // Thêm PM nếu chưa có
        if (!memberIds.Contains(context.CurrentUserId))
        {
            memberIds.Add(context.CurrentUserId);
        }

        if (memberIds.Count < 1)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                $"❌ Team phải có ít nhất 3 thành viên (hiện tại: {memberIds.Count})", context.Mode, context.IsPublic);
        }

        if (memberIds.Count > 6)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                $"❌ Team tối đa 6 thành viên (hiện tại: {memberIds.Count})", context.Mode, context.IsPublic);
        }

        // Tạo pending request
        var requestId = Guid.NewGuid().ToString();
        var pendingRequest = new PendingTeamRequest
        {
            ProjectName = projectName,
            TeamName = teamName,
            PMUserId = context.CurrentUserId,
            MemberUserIds = memberIds,
            AcceptedUserIds = new List<string> { context.CurrentUserId },
            CreatedAt = DateTime.UtcNow,
            MessageId = context.MessageId ?? "",
            SenderId = context.CurrentUserId
        };

        _pendingRequests[requestId] = pendingRequest;

        var response = ComponentResponse.FromText(
            context.ClanId!,
            context.ChannelId!,
            $"✅ Đã gửi lời mời đến {memberIds.Count - 1} thành viên.\n⏰ Các thành viên cần xác nhận trong vòng 30 phút.",
            context.Mode,
            context.IsPublic);

        // Gửi lời mời cho từng member (trừ PM) - KHÔNG dùng ComponentMessage, gửi trực tiếp
        foreach (var memberId in memberIds)
        {
            if (memberId == context.CurrentUserId) continue;

            var confirmForm = TeamFormBuilder.BuildConfirmForm(requestId, teamName, projectName, memberId);

            // Gửi trực tiếp vào channel của member (không cần response.Messages)
            try
            {
                // Thử gửi vào channel của member (channel ID chính là user ID đối với DM)
                await SendDirectMessageAsync(context.ClanId!, memberId, confirmForm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send DM to {memberId}");
            }
        }

        return response;
    }

    // Thêm method helper để gửi DM
    private async Task SendDirectMessageAsync(string clanId, string userId, ChannelMessageContent content)
    {
        // Cách 1: Gửi trực tiếp vào channel ID là user ID
        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: userId,  // DM channel ID thường là user ID
            mode: 2,
            isPublic: false,
            content: content
        );
    }

    private async Task<ComponentResponse> HandleAcceptAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu xác nhận không hợp lệ", context.Mode, context.IsPublic);
        }

        var requestId = parts[1];
        var targetUser = parts[2];

        if (!_pendingRequests.ContainsKey(requestId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu không tồn tại hoặc đã hết hạn.", context.Mode, context.IsPublic);
        }

        var request = _pendingRequests[requestId];

        // Check timeout 30 phút
        if ((DateTime.UtcNow - request.CreatedAt).TotalMinutes > 30)
        {
            _pendingRequests.Remove(requestId);
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "⏰ Yêu cầu đã hết hạn (30 phút).", context.Mode, context.IsPublic);
        }

        // Check đúng user
        if (context.CurrentUserId != targetUser)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Lời mời này không dành cho bạn.", context.Mode, context.IsPublic);
        }

        // Thêm vào danh sách đã accept
        if (!request.AcceptedUserIds.Contains(context.CurrentUserId))
        {
            request.AcceptedUserIds.Add(context.CurrentUserId);
        }

        int acceptedCount = request.AcceptedUserIds.Count;
        int totalCount = request.MemberUserIds.Count;

        if (acceptedCount == totalCount)
        {
            // Tất cả đã accept, tạo team
            var cleanMemberList = request.MemberUserIds
                .Select(m => m.StartsWith("@") ? m.Substring(1) : m)
                .ToList();

            var team = await _teamService.CreateTeamWithProjectAsync(
                request.ProjectName,
                request.TeamName,
                request.PMUserId,
                cleanMemberList
            );

            _pendingRequests.Remove(requestId);

            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                $"🎉 **Team `{request.TeamName}` đã được tạo thành công!**\n" +
                $"📁 Project: {request.ProjectName}\n" +
                $"👥 Số thành viên: {totalCount}", context.Mode, context.IsPublic);
        }
        else
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                $"✅ {context.CurrentUserId} đã chấp nhận tham gia.\n📊 Đã xác nhận: {acceptedCount}/{totalCount}", context.Mode, context.IsPublic);
        }
    }

    private async Task<ComponentResponse> HandleRejectAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu từ chối không hợp lệ", context.Mode, context.IsPublic);
        }

        var requestId = parts[1];
        var targetUser = parts[2];

        if (!_pendingRequests.ContainsKey(requestId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu không tồn tại.", context.Mode, context.IsPublic);
        }

        var request = _pendingRequests[requestId];

        if (context.CurrentUserId != targetUser)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Lời mời này không dành cho bạn.", context.Mode, context.IsPublic);
        }

        _pendingRequests.Remove(requestId);

        return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
            $"❌ {context.CurrentUserId} đã từ chối tham gia.\nTeam `{request.TeamName}` đã bị hủy.", context.Mode, context.IsPublic);
    }

    private static string ReadValue(JsonElement payload, string key)
    {
        var valuesNode = ComponentPayloadHelper.GetValues(payload);
        var fromValues = ComponentPayloadHelper.GetPropertyIgnoreCase(valuesNode, key)?.GetString();
        if (!string.IsNullOrWhiteSpace(fromValues))
        {
            return fromValues;
        }

        var extraData = ComponentPayloadHelper.GetExtraData(payload);
        if (string.IsNullOrWhiteSpace(extraData) || !extraData.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        try
        {
            using var json = JsonDocument.Parse(extraData);
            return ComponentPayloadHelper.GetPropertyIgnoreCase(json.RootElement, key)?.GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}