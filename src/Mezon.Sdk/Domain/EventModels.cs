namespace Mezon.Sdk.Domain;

/// <summary>
/// A "user added to channel" socket event.
/// </summary>
public sealed record UserChannelAddedEvent
{
    public ChannelDescriptionEvent? ChannelDesc { get; init; }
    public UserProfileRedis[]? Users { get; init; }
    public string? Status { get; init; }
    public string? ClanId { get; init; }
    public UserProfileRedis? Caller { get; init; }
    public long? CreateTimeSeconds { get; init; }
    public int? Active { get; init; }
}

/// <summary>
/// A "user removed from channel" socket event.
/// </summary>
public sealed record UserChannelRemovedEvent
{
    public string? ChannelId { get; init; }
    public string[]? UserIds { get; init; }
    public int? ChannelType { get; init; }
    public string? ClanId { get; init; }
    public int[]? BadgeCounts { get; init; }
}

/// <summary>
/// A "user removed from clan" socket event.
/// </summary>
public sealed record UserClanRemovedEvent
{
    public string? ClanId { get; init; }
    public string[]? UserIds { get; init; }
}

/// <summary>
/// Channel description embedded in socket events.
/// </summary>
public sealed record ChannelDescriptionEvent
{
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public int? Type { get; init; }
    public string? ChannelLabel { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? AppUrl { get; init; }
    public int? ChannelPrivate { get; init; }
    public string? MeetingCode { get; init; }
    public string? ClanName { get; init; }
    public string? ParentId { get; init; }
    public ApiChannelMessageHeader? LastSentMessage { get; init; }
}

/// <summary>
/// User profile stored in Redis, used in socket events.
/// </summary>
public sealed record UserProfileRedis
{
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? Avatar { get; init; }
    public string? DisplayName { get; init; }
    public string? AboutMe { get; init; }
    public string? CustomStatus { get; init; }
    public long? CreateTimeSecond { get; init; }
    public FcmToken[]? FcmTokens { get; init; }
    public bool? Online { get; init; }
    public string? Metadata { get; init; }
    public bool? IsDisabled { get; init; }
    public string[]? JoinedClans { get; init; }
    public string? PubKey { get; init; }
    public string? MezonId { get; init; }
    public string? AppToken { get; init; }
}

/// <summary>
/// FCM push notification token entry.
/// </summary>
public sealed record FcmToken
{
    public required string DeviceId { get; init; }
    public required string TokenId { get; init; }
    public required string Platform { get; init; }
}

/// <summary>
/// Channel created event.
/// </summary>
public sealed record ChannelCreatedEvent
{
    public string? ClanId { get; init; }
    public string? CategoryId { get; init; }
    public string? CreatorId { get; init; }
    public string? ParentId { get; init; }
    public string? ChannelId { get; init; }
    public string? ChannelLabel { get; init; }
    public int? ChannelPrivate { get; init; }
    public int? ChannelType { get; init; }
    public int? Status { get; init; }
    public string? AppId { get; init; }
    public string? ClanName { get; init; }
    public string? ChannelAvatar { get; init; }
}

/// <summary>
/// Channel updated event.
/// </summary>
public sealed record ChannelUpdatedEvent
{
    public string? ClanId { get; init; }
    public string? CategoryId { get; init; }
    public string? CreatorId { get; init; }
    public string? ParentId { get; init; }
    public string? ChannelId { get; init; }
    public string? ChannelLabel { get; init; }
    public int? ChannelType { get; init; }
    public int? Status { get; init; }
    public string? MeetingCode { get; init; }
    public bool? IsError { get; init; }
    public bool? ChannelPrivate { get; init; }
    public string? AppId { get; init; }
    public int? E2ee { get; init; }
    public string? Topic { get; init; }
    public int? AgeRestricted { get; init; }
    public int? Active { get; init; }
    public int? CountMessUnread { get; init; }
    public string[]? UserIds { get; init; }
    public string[]? RoleIds { get; init; }
    public string? ChannelAvatar { get; init; }
}

/// <summary>
/// Channel deleted event.
/// </summary>
public sealed record ChannelDeletedEvent
{
    public string? ClanId { get; init; }
    public string? CategoryId { get; init; }
    public string? ParentId { get; init; }
    public string? ChannelId { get; init; }
    public string? Deletor { get; init; }
}

/// <summary>
/// Clan updated event.
/// </summary>
public sealed record ClanUpdatedEvent
{
    public string? ClanId { get; init; }
    public string? ClanName { get; init; }
    public string? ClanLogo { get; init; }
    public string? Banner { get; init; }
    public int? Status { get; init; }
    public bool? IsOnboarding { get; init; }
    public string? WelcomeChannelId { get; init; }
    public string? OnboardingBanner { get; init; }
    public string? CommunityBanner { get; init; }
    public bool? IsCommunity { get; init; }
    public string? About { get; init; }
    public string? Description { get; init; }
    public bool? PreventAnonymous { get; init; }
}

/// <summary>
/// Clan profile updated event (user's clan nick/avatar changed).
/// </summary>
public sealed record ClanProfileUpdatedEvent
{
    public string? UserId { get; init; }
    public string? ClanNick { get; init; }
    public string? ClanAvatar { get; init; }
    public string? ClanId { get; init; }
}

/// <summary>
/// User profile updated event.
/// </summary>
public sealed record UserProfileUpdatedEvent
{
    public string? UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? Avatar { get; init; }
    public string? AboutMe { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
    public string? EncryptPrivateKey { get; init; }
}

/// <summary>
/// Role assigned event.
/// </summary>
public sealed record RoleAssignedEvent
{
    public string? ClanId { get; init; }
    public string? RoleId { get; init; }
    public string[]? UserIdsAssigned { get; init; }
    public string[]? UserIdsRemoved { get; init; }
}

/// <summary>
/// "Give coffee" (token gift) event.
/// </summary>
public sealed record GiveCoffeeEvent
{
    public string? SenderId { get; init; }
    public string? ReceiverId { get; init; }
    public int? TokenCount { get; init; }
    public string? MessageRefId { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
}

/// <summary>
/// Voice joined event.
/// </summary>
public sealed record VoiceJoinedEvent
{
    public string? ClanId { get; init; }
    public string? ClanName { get; init; }
    public string? Id { get; init; }
    public string? Participant { get; init; }
    public string? UserId { get; init; }
    public string? VoiceChannelLabel { get; init; }
    public string? VoiceChannelId { get; init; }
    public string? LastScreenshot { get; init; }
}

/// <summary>
/// Voice left event.
/// </summary>
public sealed record VoiceLeavedEvent
{
    public string? Id { get; init; }
    public string? ClanId { get; init; }
    public string? VoiceChannelId { get; init; }
    public string? VoiceUserId { get; init; }
}

/// <summary>
/// Streaming joined event.
/// </summary>
public sealed record StreamingJoinedEvent
{
    public string? ClanId { get; init; }
    public string? ClanName { get; init; }
    public string? Id { get; init; }
    public string? Participant { get; init; }
    public string? UserId { get; init; }
    public string? StreamingChannelLabel { get; init; }
    public string? StreamingChannelId { get; init; }
}

/// <summary>
/// Streaming left event.
/// </summary>
public sealed record StreamingLeavedEvent
{
    public string? Id { get; init; }
    public string? ClanId { get; init; }
    public string? StreamingChannelId { get; init; }
    public string? StreamingUserId { get; init; }
}

/// <summary>
/// Message typing event.
/// </summary>
public sealed record MessageTypingEvent
{
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? SenderId { get; init; }
    public bool? IsPublic { get; init; }
    public string? SenderUsername { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? TopicId { get; init; }
}

/// <summary>
/// Last seen message event.
/// </summary>
public sealed record LastSeenMessageEvent
{
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? MessageId { get; init; }
    public long? TimestampSeconds { get; init; }
    public int? BadgeCount { get; init; }
}

/// <summary>
/// Last pin message event.
/// </summary>
public sealed record LastPinMessageEvent
{
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? MessageId { get; init; }
    public string? UserId { get; init; }
    public int? Operation { get; init; }
    public bool? IsPublic { get; init; }
    public string? MessageSenderAvatar { get; init; }
    public string? MessageSenderId { get; init; }
    public string? MessageSenderUsername { get; init; }
    public string? MessageContent { get; init; }
    public string? MessageAttachment { get; init; }
    public string? MessageCreatedTime { get; init; }
    public long? TimestampSeconds { get; init; }
}

/// <summary>
/// Custom status event.
/// </summary>
public sealed record CustomStatusEvent
{
    public string? ClanId { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? Status { get; init; }
    public long? TimeReset { get; init; }
    public bool? NoClear { get; init; }
}

/// <summary>
/// Interactive button click event.
/// </summary>
public sealed record MessageButtonClickedEvent
{
    public string? MessageId { get; init; }
    public string? ChannelId { get; init; }
    public string? ButtonId { get; init; }
    public string? SenderId { get; init; }
    public string? UserId { get; init; }
    public string? ExtraData { get; init; }
}

/// <summary>
/// Dropdown select box event.
/// </summary>
public sealed record DropdownBoxSelectedEvent
{
    public string? MessageId { get; init; }
    public string? ChannelId { get; init; }
    public string? SelectboxId { get; init; }
    public string? SenderId { get; init; }
    public string? UserId { get; init; }
    public string[]? Values { get; init; }
}

/// <summary>
/// WebRTC signaling forward event.
/// </summary>
public sealed record WebrtcSignalingFwdEvent
{
    public string? ReceiverId { get; init; }
    public int? DataType { get; init; }
    public string? JsonData { get; init; }
    public string? ChannelId { get; init; }
    public string? CallerId { get; init; }
}

/// <summary>
/// Quick menu event payload.
/// </summary>
public sealed record QuickMenuDataEvent
{
    public string? MenuName { get; init; }
    public QuickMenuMessage? Message { get; init; }
}

/// <summary>
/// Message payload embedded in a quick menu event.
/// </summary>
public sealed record QuickMenuMessage
{
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public object? Content { get; init; }
    public ApiMessageMention[]? Mentions { get; init; }
    public ApiMessageAttachment[]? Attachments { get; init; }
    public bool? AnonymousMessage { get; init; }
    public bool? MentionEveryone { get; init; }
    public string? Avatar { get; init; }
    public bool? IsPublic { get; init; }
    public int? Code { get; init; }
    public string? TopicId { get; init; }
}

/// <summary>
/// Clan sticker for the sticker list.
/// </summary>
public sealed record ClanSticker
{
    public string? Category { get; init; }
    public string? ClanId { get; init; }
    public string? CreateTime { get; init; }
    public string? CreatorId { get; init; }
    public string? Id { get; init; }
    public string? Shortname { get; init; }
    public string? Source { get; init; }
}

/// <summary>
/// Clan emoji for the emoji list.
/// </summary>
public sealed record ClanEmoji
{
    public string? Category { get; init; }
    public string? CreatorId { get; init; }
    public string? Id { get; init; }
    public string? Shortname { get; init; }
    public string? Src { get; init; }
}

/// <summary>
/// Hashtag DM channel entry.
/// </summary>
public sealed record HashtagDm
{
    public string? ChannelId { get; init; }
    public string? ChannelLabel { get; init; }
    public string? ClanId { get; init; }
    public string? ClanName { get; init; }
    public string? MeetingCode { get; init; }
    public int? Type { get; init; }
    public int? ChannelPrivate { get; init; }
    public string? ParentId { get; init; }
}

/// <summary>
/// Notification channel setting event.
/// </summary>
public sealed record NotificationChannelSettingEvent
{
    public string? ChannelId { get; init; }
    public NotificationUserChannel? NotificationUserChannel { get; init; }
}

/// <summary>
/// Notification category setting event.
/// </summary>
public sealed record NotificationCategorySettingEvent
{
    public string? CategoryId { get; init; }
    public NotificationUserChannel? NotificationUserChannel { get; init; }
}

/// <summary>
/// Notification clan setting event.
/// </summary>
public sealed record NotificationClanSettingEvent
{
    public string? ClanId { get; init; }
    public NotificationSetting? NotificationSetting { get; init; }
}

/// <summary>
/// Notification setting for a user in a channel.
/// </summary>
public sealed record NotificationUserChannel
{
    public int? Active { get; init; }
    public string? Id { get; init; }
    public int? NotificationSettingType { get; init; }
    public string? TimeMute { get; init; }
}

/// <summary>
/// Global notification setting.
/// </summary>
public sealed record NotificationSetting
{
    public string? Id { get; init; }
    public int? NotificationSettingType { get; init; }
}

/// <summary>
/// Reaction notification event.
/// </summary>
public sealed record NotifiReactMessageEvent
{
    public string? ChannelId { get; init; }
    public NotifiReactMessage? NotifiReactMessage { get; init; }
}

/// <summary>
/// Reaction notification data.
/// </summary>
public sealed record NotifiReactMessage
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? ChannelIdReq { get; init; }
}
