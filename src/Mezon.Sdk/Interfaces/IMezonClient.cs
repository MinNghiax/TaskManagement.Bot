using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Interfaces;

/// <summary>
/// Contract for the main Mezon client entry point.
/// </summary>
public interface IMezonClient : IDisposable
{
    /// <summary>The current authenticated session.</summary>
    Session? CurrentSession { get; }

    /// <summary>Log in to the Mezon server and establish a realtime connection.</summary>
    Task<Session> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>Log out and close the connection.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Refresh the session token.</summary>
    Task RefreshSessionAsync(CancellationToken cancellationToken = default);


    // ─── Event Subscription ─────────────────────────────────────────────────
    /// <summary>Subscribe to a realtime event.</summary>
    void On(string eventType, EventHandler<MezonEventArgs> handler);

    /// <summary>Unsubscribe from a realtime event.</summary>
    void Remove(string eventType, EventHandler<MezonEventArgs> handler);


    // ─── Messaging ───────────────────────────────────────────────────────────
    /// <summary>Send a message to a channel.</summary>
    Task<ChannelMessageAck> SendMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        ChannelMessageContent content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? references = null,
        bool anonymousMessage = false,
        bool mentionEveryone = false,
        string? avatar = null,
        int? code = null,
        string? topicId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Send a direct message to a user.</summary>
    Task<ChannelMessageAck> SendDMAsync(
        string channelDmId,
        string message,
        Dictionary<string, object>? messageOptions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? refs = null,
        CancellationToken cancellationToken = default);


    // ─── MMN Token Transfers ─────────────────────────────────────────────────
    /// <summary>Send tokens to another user via MMN.</summary>
    Task<SendTokenResult> SendTokenAsync(SendTokenData data, CancellationToken cancellationToken = default);

    /// <summary>Get an ephemeral key pair for MMN transfers.</summary>
    Task<EphemeralKeyPair> GetEphemeralKeyPairAsync(CancellationToken cancellationToken = default);

    /// <summary>Get a user's MMN wallet address.</summary>
    Task<string> GetAddressAsync(string senderId, CancellationToken cancellationToken = default);

    /// <summary>Get the current nonce for MMN transfers.</summary>
    Task<long> GetCurrentNonceAsync(string senderId, string state = "pending", CancellationToken cancellationToken = default);

    /// <summary>Generate zero-knowledge proofs for MMN transfers.</summary>
    Task<ZkProofResponse> GetZkProofsAsync(ZkProofRequest request, CancellationToken cancellationToken = default);


    // ─── Channels ────────────────────────────────────────────────────────────
    /// <summary>Create a new channel description.</summary>
    Task<ApiChannelDescription> CreateChannelAsync(ApiCreateChannelDescRequest request, CancellationToken cancellationToken = default);

    /// <summary>Create a DM channel with a user.</summary>
    Task<ApiChannelDescription?> CreateDMChannelAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>List channels.</summary>
    Task<ApiChannelDescList> ListChannelsAsync(
        int? channelType = null, string? clanId = null,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>List users in a voice channel.</summary>
    Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string clanId, string channelId, int channelType,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default);


    // ─── High-level Structures ───────────────────────────────────────────────
    /// <summary>Get a high-level Clan object by ID.</summary>
    Task<Clan> GetClanAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>Get a high-level TextChannel object.</summary>
    Task<TextChannel> GetChannelAsync(string clanId, string channelId, CancellationToken cancellationToken = default);

    /// <summary>Get a high-level User object.</summary>
    Task<User> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for all Mezon realtime event arguments.
/// </summary>
public class MezonEventArgs : EventArgs
{
    public required object Data { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
}
