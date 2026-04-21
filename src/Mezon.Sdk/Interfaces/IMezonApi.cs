namespace Mezon.Sdk.Interfaces;

public interface IMezonApi
{
    string ApiKey { get; }
    string BasePath { get; }
    int TimeoutMs { get; }

    Task<Domain.ApiSession> MezAuthenticateAsync(
        string basicAuthUsername,
        string basicAuthPassword,
        Domain.ApiAuthenticateRequest body,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiChannelDescription> CreateChannelDescAsync(
        string bearerToken,
        Domain.ApiCreateChannelDescRequest body,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiClanDescList> ListClanDescsAsync(
        string bearerToken,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiChannelDescription> ListChannelDetailAsync(
        string bearerToken,
        string channelId,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiChannelDescList> ListChannelDescsAsync(
        string bearerToken,
        int? channelType = null,
        string? clanId = null,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        bool? isMobile = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string bearerToken,
        string? clanId = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task UpdateRoleAsync(
        string bearerToken,
        string roleId,
        Domain.MezonUpdateRoleBody body,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiRoleListEventResponse> ListRolesAsync(
        string bearerToken,
        string? clanId = null,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    Task AddQuickMenuAccessAsync(
        string bearerToken,
        Domain.ApiQuickMenuAccessRequest body,
        CancellationToken cancellationToken = default);

    Task DeleteQuickMenuAccessAsync(
        string bearerToken,
        string? id = null,
        string? clanId = null,
        string? botId = null,
        string? menuName = null,
        string? background = null,
        string? actionMsg = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
        string bearerToken,
        string? botId = null,
        string? channelId = null,
        int? menuType = null,
        CancellationToken cancellationToken = default);

    Task PlayMediaAsync(
        string bearerToken,
        Domain.PlayMediaRequest body,
        CancellationToken cancellationToken = default);
}
