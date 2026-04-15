using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;
using Google.Protobuf;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Services;
using MezonProto = Mezon.Sdk.Proto;
using ProtoChannelMessage = Mezon.Sdk.Proto.ChannelMessage;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public class TeamComponentHandler : IComponentHandler
{
    private const string DefaultApiBasePath = "https://gw.mezon.ai:443";
    private const int ChannelScanLimit = 20;
    private const int MessageScanLimit = 50;

    private readonly ILogger<TeamComponentHandler> _logger;
    private readonly MezonClient _client;
    private readonly ITeamWorkflowService _teamWorkflowService;

    private sealed record MemberResolutionResult(
        IReadOnlyList<TeamRequestMember> Members,
        bool DirectoryLookupUnavailable);

    public TeamComponentHandler(
        ILogger<TeamComponentHandler> logger,
        MezonClient client,
        ITeamWorkflowService teamWorkflowService)
    {
        _logger = logger;
        _client = client;
        _teamWorkflowService = teamWorkflowService;
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

        var parts = context.CustomId.Split('|', StringSplitOptions.RemoveEmptyEntries);
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
            "Da huy tao team",
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

        var (isValid, message) = TeamFormBuilder.ValidateForm(projectName, teamName, "PM", membersRaw);
        if (!isValid)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, message, context.Mode, context.IsPublic);
        }

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "Khong xac dinh duoc nguoi tao team", context.Mode, context.IsPublic);
        }

        var resolution = await ResolveMembersAsync(context.ClanId!, membersRaw, cancellationToken);
        var resolvedMembers = resolution.Members
            .Where(x => !string.Equals(x.UserId, context.CurrentUserId, StringComparison.Ordinal))
            .ToList();

        var unresolvedHandles = ExtractHandles(membersRaw)
            .Where(handle => resolvedMembers.All(x => !string.Equals(x.Handle, handle, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (unresolvedHandles.Count > 0)
        {
            var messageText = resolution.DirectoryLookupUnavailable
                ? $"Khong the tra cuu username trong clan cho: {string.Join(", ", unresolvedHandles)}. Vui long nhap member bang mention <@userId> hoac user id."
                : $"Khong tim thay thanh vien trong clan: {string.Join(", ", unresolvedHandles)}";

            return ComponentResponse.FromText(
                context.ClanId!,
                context.ChannelId!,
                messageText,
                context.Mode,
                context.IsPublic);
        }

        var result = await _teamWorkflowService.CreateRequestAsync(
            new CreateTeamRequestInput
            {
                ProjectName = projectName,
                TeamName = teamName,
                PMUserId = context.CurrentUserId,
                Members = resolvedMembers
            },
            cancellationToken);

        var response = ComponentResponse.FromText(context.ClanId!, context.ChannelId!, result.Message, context.Mode, context.IsPublic);
        if (!result.Success || string.IsNullOrWhiteSpace(result.RequestId))
        {
            return response;
        }

        foreach (var member in result.Members)
        {
            response.Messages.Add(new ComponentMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                Content = TeamFormBuilder.BuildConfirmForm(result.RequestId, result.TeamName!, result.ProjectName!, member.UserId),
                Mode = context.Mode,
                IsPublic = context.IsPublic
            });
        }

        return response;
    }

    private async Task<ComponentResponse> HandleAcceptAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "Yeu cau xac nhan khong hop le", context.Mode, context.IsPublic);
        }

        var result = await _teamWorkflowService.AcceptAsync(parts[1], parts[2], context.CurrentUserId, cancellationToken);
        return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, result.Message, context.Mode, context.IsPublic);
    }

    private async Task<ComponentResponse> HandleRejectAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "Yeu cau tu choi khong hop le", context.Mode, context.IsPublic);
        }

        var result = await _teamWorkflowService.RejectAsync(parts[1], parts[2], context.CurrentUserId, cancellationToken);
        return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, result.Message, context.Mode, context.IsPublic);
    }

    private async Task<MemberResolutionResult> ResolveMembersAsync(string clanId, string membersRaw, CancellationToken cancellationToken)
    {
        var clan = _client.Clans.Get(clanId) ?? await _client.GetClanAsync(clanId, cancellationToken);
        if (clan == null)
        {
            _logger.LogWarning("[TEAM_COMPONENT] Clan {ClanId} is not available in cache", clanId);
            return new MemberResolutionResult([], false);
        }

        var handles = ExtractHandles(membersRaw);
        var resolved = MatchUsersFromCache(clan.Users.GetAll(), handles);
        if (resolved.Count == handles.Count)
        {
            return new MemberResolutionResult(resolved, false);
        }

        _logger.LogInformation(
            "[TEAM_COMPONENT] Cache miss for clan {ClanId}. Hydrating users from API for handles: {Handles}",
            clanId,
            string.Join(", ", handles));

        var apiResolution = await ResolveMembersFromApiAsync(clan, handles, cancellationToken);
        return new MemberResolutionResult(
            resolved
                .Concat(apiResolution.Members)
                .DistinctBy(x => x.UserId)
                .ToList(),
            apiResolution.DirectoryLookupUnavailable);
    }

    private static List<TeamRequestMember> MatchUsersFromCache(IEnumerable<User> users, IEnumerable<string> handles)
    {
        var resolved = new List<TeamRequestMember>();

        foreach (var handle in handles)
        {
            if (IsExplicitUserId(handle))
            {
                resolved.Add(new TeamRequestMember
                {
                    UserId = handle,
                    Handle = handle
                });
                continue;
            }

            var match = users.FirstOrDefault(user =>
                string.Equals(user.Id, handle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.Username, handle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.DisplayName, handle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.ClanNick, handle, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                continue;
            }

            resolved.Add(new TeamRequestMember
            {
                UserId = match.Id,
                Handle = handle
            });
        }

        return resolved
            .DistinctBy(x => x.UserId)
            .ToList();
    }

    private async Task<MemberResolutionResult> ResolveMembersFromApiAsync(
        Clan clan,
        IReadOnlyCollection<string> handles,
        CancellationToken cancellationToken)
    {
        var sessionToken = _client.CurrentSession?.Token;
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            _logger.LogWarning("[TEAM_COMPONENT] Session token is unavailable while resolving clan users");
            return new MemberResolutionResult([], false);
        }

        ApiClanUserList response;
        var directoryLookupUnavailable = false;
        try
        {
            response = await _client.Api.ListClanUsersAsync(
                sessionToken,
                clan.Id,
                ct: cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            directoryLookupUnavailable = true;
            _logger.LogInformation(
                "[TEAM_COMPONENT] ListClanUsers is forbidden for clan {ClanId}. Falling back to cache/history only.",
                clan.Id);
            response = new ApiClanUserList { ClanUsers = [] };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TEAM_COMPONENT] Failed to call ListClanUsers for clan {ClanId}", clan.Id);
            return new MemberResolutionResult([], false);
        }

        var clanUsers = response.ClanUsers?.ToList() ?? [];
        _logger.LogInformation("[TEAM_COMPONENT] ListClanUsers returned {Count} users for clan {ClanId}", clanUsers.Count, clan.Id);

        var resolved = MatchUsersFromClanUsers(clanUsers, handles);
        if (resolved.Count == handles.Count)
        {
            return new MemberResolutionResult(resolved, directoryLookupUnavailable);
        }

        var unresolvedHandles = handles
            .Where(handle => resolved.All(x => !string.Equals(x.Handle, handle, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (unresolvedHandles.Count == 0)
        {
            return new MemberResolutionResult(resolved, directoryLookupUnavailable);
        }

        var historyResolved = await ResolveMembersFromRecentMessagesAsync(
            clan,
            sessionToken,
            unresolvedHandles,
            cancellationToken);

        return new MemberResolutionResult(
            resolved
                .Concat(historyResolved)
                .DistinctBy(x => x.UserId)
                .ToList(),
            directoryLookupUnavailable);
    }

    private async Task<List<TeamRequestMember>> ResolveMembersFromRecentMessagesAsync(
        Clan clan,
        string sessionToken,
        IReadOnlyCollection<string> handles,
        CancellationToken cancellationToken)
    {
        List<ApiChannelDescription> channels = clan.Channels.GetAll();
        if (channels.Count == 0)
        {
            try
            {
                await clan.LoadChannelsAsync(cancellationToken);
                channels = clan.Channels.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[TEAM_COMPONENT] Failed to load cached channels for clan {ClanId}", clan.Id);
            }
        }

        if (channels.Count == 0)
        {
            try
            {
                var response = await _client.ListChannelsAsync(clanId: clan.Id, limit: 100, cancellationToken: cancellationToken);
                channels = response.ChannelDescs?
                    .Where(x => !string.IsNullOrWhiteSpace(x.ChannelId))
                    .ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[TEAM_COMPONENT] Failed to list channels for clan {ClanId}", clan.Id);
                return [];
            }
        }

        if (channels.Count == 0)
        {
            return [];
        }

        var resolved = new List<TeamRequestMember>();

        foreach (var channel in channels
                     .Where(x => !string.IsNullOrWhiteSpace(x.ChannelId))
                     .Take(ChannelScanLimit))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var messages = await ListChannelMessagesAsync(
                sessionToken,
                clan.Id,
                channel.ChannelId!,
                MessageScanLimit,
                cancellationToken);

            foreach (var message in messages)
            {
                CacheUserFromHistory(clan, message);
            }

            var unresolvedHandles = handles
                .Where(handle => resolved.All(x => !string.Equals(x.Handle, handle, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (unresolvedHandles.Count == 0)
            {
                break;
            }

            resolved.AddRange(MatchUsersFromMessages(messages, unresolvedHandles));

            if (resolved.Count == handles.Count)
            {
                break;
            }
        }

        if (resolved.Count > 0)
        {
            _logger.LogInformation(
                "[TEAM_COMPONENT] Resolved {Count} members from recent messages in clan {ClanId}",
                resolved.Count,
                clan.Id);
        }

        return resolved
            .DistinctBy(x => x.UserId)
            .ToList();
    }

    private async Task<IReadOnlyList<ProtoChannelMessage>> ListChannelMessagesAsync(
        string sessionToken,
        string clanId,
        string channelId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(clanId, out var parsedClanId) || !long.TryParse(channelId, out var parsedChannelId))
        {
            return [];
        }

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(7)
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetApiBasePath()}/mezon.api.Mezon/ListChannelMessages");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/proto"));

        var body = new MezonProto.ListChannelMessagesRequest
        {
            ClanId = parsedClanId,
            ChannelId = parsedChannelId,
            Limit = limit
        };

        request.Content = new ByteArrayContent(body.ToByteArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/proto");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "[TEAM_COMPONENT] ListChannelMessages returned status {StatusCode} for clan {ClanId} channel {ChannelId}",
                    (int)response.StatusCode,
                    clanId,
                    channelId);
                return [];
            }

            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (payload.Length == 0)
            {
                return [];
            }

            return MezonProto.ChannelMessageList.Parser.ParseFrom(payload).Messages.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[TEAM_COMPONENT] Failed to scan messages for clan {ClanId} channel {ChannelId}", clanId, channelId);
            return [];
        }
    }

    private void CacheUserFromHistory(Clan clan, ProtoChannelMessage message)
    {
        var userId = message.SenderId.ToString();
        if (string.IsNullOrWhiteSpace(userId) || userId == _client.ClientId)
        {
            return;
        }

        clan.Users.Set(
            userId,
            new User(
                userId: userId,
                client: _client,
                api: _client.Api,
                getSessionToken: () => _client.CurrentSession?.Token ?? "",
                username: message.Username,
                displayName: message.DisplayName,
                clanNick: message.ClanNick,
                clanAvatar: message.ClanAvatar,
                avatarUrl: message.Avatar));
    }

    private static List<TeamRequestMember> MatchUsersFromMessages(
        IEnumerable<ProtoChannelMessage> messages,
        IEnumerable<string> handles)
    {
        var resolved = new List<TeamRequestMember>();

        foreach (var handle in handles)
        {
            var match = messages.FirstOrDefault(message => IsMatch(message, handle));
            var userId = match?.SenderId.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                continue;
            }

            resolved.Add(new TeamRequestMember
            {
                UserId = userId,
                Handle = handle
            });
        }

        return resolved
            .DistinctBy(x => x.UserId)
            .ToList();
    }

    private static List<TeamRequestMember> MatchUsersFromClanUsers(
        IEnumerable<ApiClanUser> clanUsers,
        IEnumerable<string> handles)
    {
        var resolved = new List<TeamRequestMember>();

        foreach (var handle in handles)
        {
            var match = clanUsers.FirstOrDefault(clanUser => IsMatch(clanUser, handle));
            if (string.IsNullOrWhiteSpace(match?.User?.Id))
            {
                continue;
            }

            resolved.Add(new TeamRequestMember
            {
                UserId = match.User.Id,
                Handle = handle
            });
        }

        return resolved
            .DistinctBy(x => x.UserId)
            .ToList();
    }

    private static bool IsMatch(ApiClanUser clanUser, string handle)
    {
        var user = clanUser.User;
        if (user == null)
        {
            return false;
        }

        return string.Equals(user.Id, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Username, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.DisplayName, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clanUser.ClanNick, handle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatch(ProtoChannelMessage message, string handle)
    {
        var senderId = message.SenderId.ToString();

        return string.Equals(senderId, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.Username, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.DisplayName, handle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.ClanNick, handle, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ExtractHandles(string membersRaw)
    {
        return membersRaw
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeHandle)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeHandle(string rawHandle)
    {
        var handle = rawHandle.Trim();
        if (handle.StartsWith("<@", StringComparison.Ordinal) && handle.EndsWith(">", StringComparison.Ordinal))
        {
            return handle[2..^1].Trim();
        }

        return handle.TrimStart('@');
    }

    private static bool IsExplicitUserId(string handle) =>
        handle.All(char.IsDigit);

    private string GetApiBasePath()
    {
        var apiUrl = _client.CurrentSession?.ApiUrl;
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return DefaultApiBasePath;
        }

        try
        {
            var uri = new Uri(
                apiUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? apiUrl
                    : $"https://{apiUrl}");

            var port = uri.IsDefaultPort
                ? uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80
                : uri.Port;

            return $"{uri.Scheme}://{uri.Host}:{port}";
        }
        catch
        {
            return DefaultApiBasePath;
        }
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
