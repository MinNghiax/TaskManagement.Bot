namespace Mezon.Sdk.Interfaces;

public interface IMezonSocket : IDisposable
{
    bool IsOpen { get; }

    Task<Domain.Session> ConnectAsync(
        Domain.Session session,
        bool createStatus = false,
        int? connectTimeoutMs = null,
        CancellationToken cancellationToken = default);

    void Disconnect(bool fireDisconnectEvent = true);


    Task<Domain.Channel> JoinClanChatAsync(string clanId, CancellationToken cancellationToken = default);

    Task<Domain.Channel> JoinClanAsync(string clanId, CancellationToken cancellationToken = default);

    Task<Domain.Channel> JoinChatAsync(
        string clanId, string channelId, int channelType, bool isPublic,
        CancellationToken cancellationToken = default);

    Task LeaveChatAsync(
        string clanId, string channelId, int channelType, bool isPublic,
        CancellationToken cancellationToken = default);

    Task<Domain.ChannelMessageAck> WriteChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        object content,
        Domain.ApiMessageMention[]? mentions = null,
        Domain.ApiMessageAttachment[]? attachments = null,
        Domain.ApiMessageRef[]? references = null,
        bool anonymousMessage = false,
        bool mentionEveryone = false,
        string? avatar = null,
        int? code = null,
        string? topicId = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ChannelMessageAck> WriteEphemeralMessageAsync(
        string receiverId, string clanId, string channelId, int mode, bool isPublic,
        object content,
        Domain.ApiMessageMention[]? mentions = null,
        Domain.ApiMessageAttachment[]? attachments = null,
        Domain.ApiMessageRef[]? references = null,
        bool anonymousMessage = false,
        bool mentionEveryone = false,
        string? avatar = null,
        int? code = null,
        string? topicId = null,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ChannelMessageAck> UpdateChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, object content,
        Domain.ApiMessageMention[]? mentions = null,
        Domain.ApiMessageAttachment[]? attachments = null,
        bool? hideEditted = null,
        string? topicId = null,
        CancellationToken cancellationToken = default);

    Task<Domain.ChannelMessageAck> RemoveChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, string? topicId = null,
        CancellationToken cancellationToken = default);


    Task<Domain.ApiMessageReaction> WriteMessageReactionAsync(
        string id, string clanId, string channelId, int mode, bool isPublic,
        string messageId, string emojiId, string emoji, int count,
        string messageSenderId, bool actionDelete,
        CancellationToken cancellationToken = default);

    Task WriteMessageTypingAsync(
        string clanId, string channelId, int mode, bool isPublic,
        CancellationToken cancellationToken = default);

    Task WriteLastSeenMessageAsync(
        string clanId, string channelId, int mode,
        string messageId, long timestampSeconds,
        CancellationToken cancellationToken = default);

    Task WriteLastPinMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, long timestampSeconds, int operation,
        CancellationToken cancellationToken = default);


    Task WriteCustomStatusAsync(string clanId, string status, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string? status = null, CancellationToken cancellationToken = default);


    Task<Domain.TokenSentEvent> SendTokenAsync(
        string receiverId, int amount,
        CancellationToken cancellationToken = default);


    Task<Domain.ClanEmoji[]> ListClanEmojiAsync(string clanId, CancellationToken cancellationToken = default);

    Task<Domain.ClanSticker[]> ListClanStickersAsync(string clanId, CancellationToken cancellationToken = default);

    Task<Domain.ChannelDescriptionEvent[]> ListChannelsByUserIdAsync(CancellationToken cancellationToken = default);

    Task<Domain.HashtagDm[]> HashtagDMListAsync(string[] userIds, int limit, CancellationToken cancellationToken = default);

    Task<bool> CheckDuplicateClanNameAsync(string clanName, CancellationToken cancellationToken = default);


    Task<Domain.NotificationChannelSettingEvent?> GetNotificationChannelSettingAsync(
        string channelId, CancellationToken cancellationToken = default);

    Task<Domain.NotificationCategorySettingEvent?> GetNotificationCategorySettingAsync(
        string categoryId, CancellationToken cancellationToken = default);

    Task<Domain.NotificationClanSettingEvent?> GetNotificationClanSettingAsync(
        string clanId, CancellationToken cancellationToken = default);
}
