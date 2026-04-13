using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;
using Mezon.Sdk.Managers;
using Mezon.Sdk.Socket;

namespace Mezon.Sdk.Structures;

/// <summary>
/// High-level Clan structure, providing clan-level operations.
/// Similar to TypeScript SDK's Clan class with cache support.
/// </summary>
public sealed class Clan
{
    public string Id { get; }
    public string Name { get; }
    public string WelcomeChannelId { get; }

    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly MezonSocket _socket;
    private readonly Func<string> _getSessionToken;
    private bool _channelsLoaded;
    private Task? _loadingTask;

    /// <summary>
    /// Channel manager with cache, similar to TypeScript SDK's clan.channels
    /// </summary>
    public ChannelManager Channels { get; }

    /// <summary>
    /// User manager with cache, similar to TypeScript SDK's clan.users
    /// </summary>
    public UserManager Users { get; }

    public Clan(
        string clanId,
        MezonClient client,
        MezonRestApi api,
        MezonSocket socket,
        Func<string> getSessionToken,
        string name = "",
        string welcomeChannelId = "")
    {
        Id = clanId;
        Name = name;
        WelcomeChannelId = welcomeChannelId;
        _client = client;
        _api = api;
        _socket = socket;
        _getSessionToken = getSessionToken;

        // Initialize managers
        Channels = new ChannelManager(clanId, api, getSessionToken);
        Users = new UserManager(clanId, client, api, getSessionToken);
    }

    /// <summary>Get the client instance</summary>
    public MezonClient GetClient() => _client;

    /// <summary>Get the bot's client ID</summary>
    public string GetClientId() => _client.ClientId;

    /// <summary>
    /// Load all channels in this clan and populate cache.
    /// Similar to TypeScript SDK's clan.loadChannels()
    /// </summary>
    public async Task LoadChannelsAsync(CancellationToken cancellationToken = default)
    {
        if (_channelsLoaded) return;
        _loadingTask ??= DoLoadChannelsAsync(cancellationToken);
        await _loadingTask;
    }

    private async Task DoLoadChannelsAsync(CancellationToken ct)
    {
        try
        {
            await Channels.FetchAllAsync(ct);
            _channelsLoaded = true;
        }
        catch { /* ignore */ }
    }

    /// <summary>List voice channel users.</summary>
    public async Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var sessionToken = _getSessionToken();
        return await _api.ListChannelVoiceUsersAsync(sessionToken, Id, limit, cancellationToken);
    }
}
