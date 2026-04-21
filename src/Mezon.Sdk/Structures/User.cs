using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;

namespace Mezon.Sdk.Structures;

public sealed class User
{
    public string Id { get; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? ClanNick { get; set; }
    public string? ClanAvatar { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DmChannelId { get; set; }

    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly Func<string> _getSessionToken;

    public User(
        string userId,
        MezonClient client,
        MezonRestApi api,
        Func<string> getSessionToken,
        string? username = null,
        string? displayName = null,
        string? clanNick = null,
        string? clanAvatar = null,
        string? avatarUrl = null,
        string? dmChannelId = null)
    {
        Id = userId;
        Username = username;
        DisplayName = displayName;
        ClanNick = clanNick;
        ClanAvatar = clanAvatar;
        AvatarUrl = avatarUrl;
        DmChannelId = dmChannelId;
        _client = client;
        _api = api;
        _getSessionToken = getSessionToken;
    }

    public async Task<ApiChannelDescription?> CreateDmChannelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionToken = _getSessionToken();
            var dmChannel = await _client.CreateDMChannelAsync(Id, cancellationToken);
            
            if (dmChannel != null && !string.IsNullOrEmpty(dmChannel.ChannelId))
            {
                DmChannelId = dmChannel.ChannelId;
            }

            return dmChannel;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ChannelMessageAck> SendDMAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        return await _client.SendDMAsync(
            channelDmId: await EnsureDmChannelAsync(cancellationToken),
            message: message,
            cancellationToken: cancellationToken
        );
    }

    public async Task<ChannelMessageAck> SendDMAsync(
        ChannelMessageContent content,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? references = null,
        CancellationToken cancellationToken = default)
    {
        var dmChannelId = await EnsureDmChannelAsync(cancellationToken);

        return await _client.SendMessageAsync(
            clanId: "",
            channelId: dmChannelId,
            mode: ChannelStreamMode.Dm,
            isPublic: false,
            content: content,
            attachments: attachments,
            references: references,
            cancellationToken: cancellationToken);
    }

    private async Task<string> EnsureDmChannelAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(DmChannelId))
        {
            var dmChannel = await CreateDmChannelAsync(cancellationToken);
            if (dmChannel == null || string.IsNullOrEmpty(dmChannel.ChannelId))
            {
                throw new InvalidOperationException($"Cannot create DM channel for user {Id}");
            }
            DmChannelId = dmChannel.ChannelId;
        }

        return DmChannelId;
    }
}
