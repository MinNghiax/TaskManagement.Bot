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
    private readonly ITeamWorkflowService _teamWorkflowService;
    private readonly MezonClient _client;
    private readonly Dictionary<string, PendingTeamRequest> _pendingRequests;

    public TeamComponentHandler(
        ILogger<TeamComponentHandler> logger,
        ITeamWorkflowService teamWorkflowService,
        MezonClient client)
    {
        _logger = logger;
        _teamWorkflowService = teamWorkflowService;
        _client = client;
        _pendingRequests = new Dictionary<string, PendingTeamRequest>();
    }

    public bool CanHandle(string customId)
    {
        return customId.StartsWith("CREATE_TEAM", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("CANCEL_TEAM", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("ACCEPT", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("ADD_MEMBER_FIELD", StringComparison.OrdinalIgnoreCase);
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
            "ADD_MEMBER_FIELD" => await HandleAddMemberFieldAsync(context, parts, cancellationToken),
            "ACCEPT" => await HandleAcceptAsync(context, parts, cancellationToken),
            "REJECT" => await HandleRejectAsync(context, parts, cancellationToken),
            _ => new ComponentResponse()
        };
    }

    private async Task<ComponentResponse> HandleAddMemberFieldAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        // Lấy số lượng member hiện tại
        var currentCount = parts.Length > 2 ? int.Parse(parts[2]) : 3;

        // Lấy các giá trị đã nhập từ form
        var projectName = ReadValue(context.Payload, "project_name");
        var teamName = ReadValue(context.Payload, "team_name");

        // Lấy danh sách member đã nhập
        var existingMembers = new List<string>();
        for (int i = 1; i <= currentCount; i++)
        {
            var memberValue = ReadValue(context.Payload, $"member_{i}");
            if (!string.IsNullOrWhiteSpace(memberValue))
            {
                existingMembers.Add(memberValue);
            }
            else
            {
                existingMembers.Add("");
            }
        }

        // Tạo form mới với thêm 1 trường member
        var updatedForm = TeamFormBuilder.BuildAddMemberFieldForm(context.ClanId!, currentCount, projectName, teamName, existingMembers);

        var response = new ComponentResponse();

        // Xóa form cũ
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId!,
                context.ChannelId!,
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        // Gửi form mới
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = updatedForm,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private static ComponentResponse BuildCancelResponse(ComponentContext context)
    {
        var response = new ComponentResponse();

        // Xóa form
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId!,
                context.ChannelId!,
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        // Gửi tin nhắn thông báo (reply vào form)
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = "❌ Đã hủy tạo team. Form đã được đóng.",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private async Task<ComponentResponse> HandleCreateAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        var projectName = ReadValue(context.Payload, "project_name");
        var teamName = ReadValue(context.Payload, "team_name");
        var membersRaw = ReadValue(context.Payload, "members");

        // Lấy tất cả các member từ member_1 đến member_6
        var formValues = new Dictionary<string, string>();
        for (int i = 1; i <= 6; i++)
        {
            var value = ReadValue(context.Payload, $"member_{i}");
            if (!string.IsNullOrWhiteSpace(value))
            {
                formValues[$"member_{i}"] = value;
            }
        }

        // Validate và lấy danh sách members
        var (isValid, message, memberList) = TeamFormBuilder.ValidateFormWithMembers(projectName, teamName, formValues);
        if (!isValid)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, message, context.Mode, context.IsPublic, context.MessageId, null);
        }

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Không xác định được người tạo team", context.Mode, context.IsPublic, context.MessageId, null);
        }

        var memberTokens = TeamFormBuilder.ExtractMemberIds(membersRaw);
        var resolvedMembers = await ResolveMembersAsync(context.ClanId!, memberTokens, cancellationToken);

        if (resolvedMembers.InvalidTokens.Count > 0)
        {
            return ComponentResponse.FromText(
                context.ClanId!,
                context.ChannelId!,
                $"❌ Không tìm thấy user hợp lệ cho: {string.Join(", ", resolvedMembers.InvalidTokens)}",
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        var createResult = await _teamWorkflowService.CreateRequestAsync(
            new CreateTeamRequestInput
            {
                ProjectName = projectName,
                TeamName = teamName,
                PMUserId = context.CurrentUserId,
                Members = resolvedMembers.Members
            },
            cancellationToken);

        if (!createResult.Success || string.IsNullOrWhiteSpace(createResult.RequestId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, createResult.Message, context.Mode, context.IsPublic, context.MessageId, null);
        }

        var failedInvites = new List<string>();

        foreach (var member in createResult.Members)
        {
            try
            {
                await _client.SendEphemeralMessageAsync(
                    receiverId: member.UserId,
                    clanId: context.ClanId!,
                    channelId: context.ChannelId!,
                    mode: context.Mode,
                    isPublic: context.IsPublic,
                    content: TeamFormBuilder.BuildConfirmForm(
                        createResult.RequestId,
                        createResult.TeamName ?? teamName,
                        createResult.ProjectName ?? projectName,
                        member.UserId,
                        context.ClanId!),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                failedInvites.Add(member.Handle);
                _logger.LogError(
                    ex,
                    "[TEAM_REQUEST] Failed to send ephemeral invite to {UserId} for request {RequestId}",
                    member.UserId,
                    createResult.RequestId);
            }
        }

        var responseMessage = failedInvites.Count == 0
            ? createResult.Message
            : $"{createResult.Message}\n⚠️ Không gửi được lời mời cho: {string.Join(", ", failedInvites)}";

        return ComponentResponse.FromText(
            context.ClanId!,
            context.ChannelId!,
            responseMessage,
            context.Mode,
            context.IsPublic,
            context.MessageId,
            null);
    }

    private async Task<ComponentResponse> HandleAcceptAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu xác nhận không hợp lệ", context.Mode, context.IsPublic, context.MessageId, null);
        }

        var result = await _teamWorkflowService.AcceptAsync(parts[1], parts[2], context.CurrentUserId, cancellationToken);
        return await BuildDecisionResponseAsync(context, result, cancellationToken);
    }

    private async Task<ComponentResponse> HandleRejectAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Yêu cầu từ chối không hợp lệ", context.Mode, context.IsPublic, context.MessageId, null);
        }

        var result = await _teamWorkflowService.RejectAsync(parts[1], parts[2], context.CurrentUserId, cancellationToken);
        return await BuildDecisionResponseAsync(context, result, cancellationToken);
    }

    private async Task<ComponentResponse> BuildDecisionResponseAsync(
        ComponentContext context,
        TeamRequestActionResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _client.SendEphemeralMessageAsync(
                receiverId: context.CurrentUserId!,
                clanId: context.ClanId!,
                channelId: context.ChannelId!,
                mode: context.Mode,
                isPublic: context.IsPublic,
                content: new ChannelMessageContent { Text = result.Message },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TEAM_REQUEST] Failed to send ephemeral status to {UserId}", context.CurrentUserId);
        }

        if (result.TeamCreated || result.Success && result.Message.Contains("bi huy", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, result.Message, context.Mode, context.IsPublic, context.MessageId, null);
        }

        return new ComponentResponse();
    }

    private async Task<ResolvedTeamMembers> ResolveMembersAsync(
        string clanId,
        IReadOnlyCollection<string> memberTokens,
        CancellationToken cancellationToken)
    {
        var resolvedMembers = new List<TeamRequestMember>();
        var invalidTokens = new List<string>();
        var aliases = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (memberTokens.Any(x => x.StartsWith("@", StringComparison.Ordinal)))
        {
            PopulateAliasesFromCache(clanId, aliases);
            await PopulateAliasesFromApiAsync(clanId, aliases, cancellationToken);
        }

        foreach (var token in memberTokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (long.TryParse(token, out _))
            {
                resolvedMembers.Add(new TeamRequestMember
                {
                    UserId = token,
                    Handle = $"<@{token}>"
                });
                continue;
            }

            if (!token.StartsWith("@", StringComparison.Ordinal))
            {
                invalidTokens.Add(token);
                continue;
            }

            var lookupKey = token[1..].Trim();
            if (!aliases.TryGetValue(lookupKey, out var matchedUserIds) || matchedUserIds.Count != 1)
            {
                invalidTokens.Add(token);
                continue;
            }

            resolvedMembers.Add(new TeamRequestMember
            {
                UserId = matchedUserIds.Single(),
                Handle = token
            });
        }

        return new ResolvedTeamMembers(
            resolvedMembers
                .DistinctBy(x => x.UserId)
                .ToList(),
            invalidTokens
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private void PopulateAliasesFromCache(string clanId, Dictionary<string, HashSet<string>> aliases)
    {
        var clan = _client.Clans.Get(clanId);
        if (clan == null)
        {
            return;
        }

        foreach (var user in clan.Users.GetAll())
        {
            AddAlias(aliases, user.Username, user.Id);
            AddAlias(aliases, user.ClanNick, user.Id);
            AddAlias(aliases, user.DisplayName, user.Id);
        }
    }

    private async Task PopulateAliasesFromApiAsync(
        string clanId,
        Dictionary<string, HashSet<string>> aliases,
        CancellationToken cancellationToken)
    {
        var sessionToken = _client.CurrentSession?.Token;
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        try
        {
            var clanUsers = await _client.Api.ListClanUsersAsync(sessionToken, clanId, ct: cancellationToken);
            foreach (var clanUser in clanUsers.ClanUsers ?? [])
            {
                if (string.IsNullOrWhiteSpace(clanUser.User?.Id))
                {
                    continue;
                }

                AddAlias(aliases, clanUser.User.Username, clanUser.User.Id);
                AddAlias(aliases, clanUser.ClanNick, clanUser.User.Id);
                AddAlias(aliases, clanUser.User.DisplayName, clanUser.User.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TEAM_REQUEST] Failed to load clan users for clan {ClanId}", clanId);
        }
    }

    private static void AddAlias(Dictionary<string, HashSet<string>> aliases, string? alias, string userId)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var key = alias.Trim().TrimStart('@');
        if (key.Length == 0)
        {
            return;
        }

        if (!aliases.TryGetValue(key, out var userIds))
        {
            userIds = new HashSet<string>(StringComparer.Ordinal);
            aliases[key] = userIds;
        }

        userIds.Add(userId);
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

    private sealed record ResolvedTeamMembers(
        IReadOnlyList<TeamRequestMember> Members,
        IReadOnlyList<string> InvalidTokens);
}
