using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Interfaces;

public interface IMezonClient : IDisposable
{
    Session? CurrentSession { get; }

    Task<Session> LoginAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task RefreshSessionAsync(CancellationToken cancellationToken = default);


    void On(string eventType, EventHandler<MezonEventArgs> handler);

    void Remove(string eventType, EventHandler<MezonEventArgs> handler);


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

    Task<ChannelMessageAck> SendDMAsync(
        string channelDmId,
        string message,
        Dictionary<string, object>? messageOptions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? refs = null,
        CancellationToken cancellationToken = default);


    Task<SendTokenResult> SendTokenAsync(SendTokenData data, CancellationToken cancellationToken = default);

    Task<EphemeralKeyPair> GetEphemeralKeyPairAsync(CancellationToken cancellationToken = default);

    Task<string> GetAddressAsync(string senderId, CancellationToken cancellationToken = default);

    Task<long> GetCurrentNonceAsync(string senderId, string state = "pending", CancellationToken cancellationToken = default);

    Task<ZkProofResponse> GetZkProofsAsync(ZkProofRequest request, CancellationToken cancellationToken = default);


    Task<ApiChannelDescription> CreateChannelAsync(ApiCreateChannelDescRequest request, CancellationToken cancellationToken = default);

    Task<ApiChannelDescription?> CreateDMChannelAsync(string userId, CancellationToken cancellationToken = default);

    Task<ApiChannelDescList> ListChannelsAsync(
        int? channelType = null, string? clanId = null,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default);

    Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string clanId, string channelId, int channelType,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default);


    Task<Clan> GetClanAsync(string clanId, CancellationToken cancellationToken = default);

    Task<TextChannel> GetChannelAsync(string clanId, string channelId, CancellationToken cancellationToken = default);

    Task<User> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}

public class MezonEventArgs : EventArgs
{
    public required object Data { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
}
