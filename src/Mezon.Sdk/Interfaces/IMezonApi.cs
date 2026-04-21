namespace Mezon.Sdk.Interfaces;

/// <summary>
/// Contract for the Mezon server REST API client.
/// </summary>
public interface IMezonApi
{
    string ApiKey { get; }
    string BasePath { get; }
    int TimeoutMs { get; }

    /// <summary>Authenticate with app credentials and get a session token.</summary>
    Task<Domain.ApiSession> MezAuthenticateAsync(
        string basicAuthUsername,
        string basicAuthPassword,
        Domain.ApiAuthenticateRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>Create a new channel.</summary>
    Task<Domain.ApiChannelDescription> CreateChannelDescAsync(
        string bearerToken,
        Domain.ApiCreateChannelDescRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>List clans accessible to the current user.</summary>
    Task<Domain.ApiClanDescList> ListClanDescsAsync(
        string bearerToken,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>Get detailed information about a specific channel.</summary>
    Task<Domain.ApiChannelDescription> ListChannelDetailAsync(
        string bearerToken,
        string channelId,
        CancellationToken cancellationToken = default);

    /// <summary>List channels the current user has access to.</summary>
    Task<Domain.ApiChannelDescList> ListChannelDescsAsync(
        string bearerToken,
        int? channelType = null,
        string? clanId = null,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        bool? isMobile = null,
        CancellationToken cancellationToken = default);

    /// <summary>List users currently in a voice channel.</summary>
    Task<Domain.ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string bearerToken,
        string? clanId = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>Update role fields.</summary>
    Task UpdateRoleAsync(
        string bearerToken,
        string roleId,
        Domain.MezonUpdateRoleBody body,
        CancellationToken cancellationToken = default);

    /// <summary>List roles in a clan.</summary>
    Task<Domain.ApiRoleListEventResponse> ListRolesAsync(
        string bearerToken,
        string? clanId = null,
        int? limit = null,
        int? state = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>Add a quick menu entry.</summary>
    Task AddQuickMenuAccessAsync(
        string bearerToken,
        Domain.ApiQuickMenuAccessRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>Delete a quick menu entry.</summary>
    Task DeleteQuickMenuAccessAsync(
        string bearerToken,
        string? id = null,
        string? clanId = null,
        string? botId = null,
        string? menuName = null,
        string? background = null,
        string? actionMsg = null,
        CancellationToken cancellationToken = default);

    /// <summary>List quick menu entries.</summary>
    Task<Domain.ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
        string bearerToken,
        string? botId = null,
        string? channelId = null,
        int? menuType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Play media in a voice channel.</summary>
    Task PlayMediaAsync(
        string bearerToken,
        Domain.PlayMediaRequest body,
        CancellationToken cancellationToken = default);
}
