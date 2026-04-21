namespace Mezon.Sdk.Interfaces;

/// <summary>
/// Contract for the Mezon WebSocket realtime connection.
/// </summary>
public interface IMezonSocket : IDisposable
{
    /// <summary>Whether the socket is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Connect to the Mezon server using the session token.</summary>
    Task<Domain.Session> ConnectAsync(
        Domain.Session session,
        bool createStatus = false,
        int? connectTimeoutMs = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disconnect from the server.</summary>
    void Disconnect(bool fireDisconnectEvent = true);

    // ─── Chat Messaging ─────────────────────────────────────────────────────

    /// <summary>Join a clan chat room.</summary>
    Task<Domain.Channel> JoinClanChatAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>Alias for <see cref="JoinClanChatAsync"/>.</summary>
    Task<Domain.Channel> JoinClanAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>Join a specific channel.</summary>
    Task<Domain.Channel> JoinChatAsync(
        string clanId, string channelId, int channelType, bool isPublic,
        CancellationToken cancellationToken = default);

    /// <summary>Leave a channel.</summary>
    Task LeaveChatAsync(
        string clanId, string channelId, int channelType, bool isPublic,
        CancellationToken cancellationToken = default);

    /// <summary>Send a message to a channel.</summary>
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

    /// <summary>Send an ephemeral (auto-dismissing) message.</summary>
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

    /// <summary>Update a message previously sent.</summary>
    Task<Domain.ChannelMessageAck> UpdateChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, object content,
        Domain.ApiMessageMention[]? mentions = null,
        Domain.ApiMessageAttachment[]? attachments = null,
        bool? hideEditted = null,
        string? topicId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Remove a message from a channel.</summary>
    Task<Domain.ChannelMessageAck> RemoveChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, string? topicId = null,
        CancellationToken cancellationToken = default);

    // ─── Reactions & Typing ─────────────────────────────────────────────────

    /// <summary>Add or remove a reaction on a message.</summary>
    Task<Domain.ApiMessageReaction> WriteMessageReactionAsync(
        string id, string clanId, string channelId, int mode, bool isPublic,
        string messageId, string emojiId, string emoji, int count,
        string messageSenderId, bool actionDelete,
        CancellationToken cancellationToken = default);

    /// <summary>Send a typing indicator.</summary>
    Task WriteMessageTypingAsync(
        string clanId, string channelId, int mode, bool isPublic,
        CancellationToken cancellationToken = default);

    /// <summary>Mark a message as seen.</summary>
    Task WriteLastSeenMessageAsync(
        string clanId, string channelId, int mode,
        string messageId, long timestampSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>Pin or unpin a message.</summary>
    Task WriteLastPinMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, long timestampSeconds, int operation,
        CancellationToken cancellationToken = default);

    // ─── Status & Presence ───────────────────────────────────────────────────

    /// <summary>Update the current user's custom status.</summary>
    Task WriteCustomStatusAsync(string clanId, string status, CancellationToken cancellationToken = default);

    /// <summary>Set the current user's presence status.</summary>
    Task UpdateStatusAsync(string? status = null, CancellationToken cancellationToken = default);

    // ─── Token Transfers (MMN) ───────────────────────────────────────────────

    /// <summary>Send tokens to another user.</summary>
    Task<Domain.TokenSentEvent> SendTokenAsync(
        string receiverId, int amount,
        CancellationToken cancellationToken = default);

    // ─── Utility ─────────────────────────────────────────────────────────────

    /// <summary>List clan emoji.</summary>
    Task<Domain.ClanEmoji[]> ListClanEmojiAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>List clan stickers.</summary>
    Task<Domain.ClanSticker[]> ListClanStickersAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>List channels a user is part of.</summary>
    Task<Domain.ChannelDescriptionEvent[]> ListChannelsByUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>List hashtag DMs.</summary>
    Task<Domain.HashtagDm[]> HashtagDMListAsync(string[] userIds, int limit, CancellationToken cancellationToken = default);

    /// <summary>Check if a clan name already exists.</summary>
    Task<bool> CheckDuplicateClanNameAsync(string clanName, CancellationToken cancellationToken = default);

    // ─── Notifications ───────────────────────────────────────────────────────

    /// <summary>Get notification settings for a channel.</summary>
    Task<Domain.NotificationChannelSettingEvent?> GetNotificationChannelSettingAsync(
        string channelId, CancellationToken cancellationToken = default);

    /// <summary>Get notification settings for a category.</summary>
    Task<Domain.NotificationCategorySettingEvent?> GetNotificationCategorySettingAsync(
        string categoryId, CancellationToken cancellationToken = default);

    /// <summary>Get notification settings for a clan.</summary>
    Task<Domain.NotificationClanSettingEvent?> GetNotificationClanSettingAsync(
        string clanId, CancellationToken cancellationToken = default);
}
