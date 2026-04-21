namespace Mezon.Sdk.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// The content payload of a channel message, containing text, embeds, components, and rich content markers.
/// </summary>
public sealed record ChannelMessageContent
{
    [JsonPropertyName("t")]
    public string? Text { get; init; }
    
    [JsonPropertyName("ct")]
    public string? ContentThread { get; init; }
    
    [JsonPropertyName("hg")]
    public HashtagOnMessage[]? Hashtags { get; init; }
    
    [JsonPropertyName("ej")]
    public EmojiOnMessage[]? Emojis { get; init; }
    
    [JsonPropertyName("lk")]
    public LinkOnMessage[]? Links { get; init; }
    
    [JsonPropertyName("mk")]
    public MarkdownOnMessage[]? Markdowns { get; init; }
    
    [JsonPropertyName("vc")]
    public VoiceLinkOnMessage[]? VoiceLinks { get; init; }
    
    [JsonPropertyName("embed")]
    public object[]? Embed { get; init; }
    
    [JsonPropertyName("components")]
    public object[]? Components { get; init; }
}

/// <summary>
/// A hashtag detected in message content.
/// </summary>
public sealed record HashtagOnMessage
{
    public required int S { get; init; }
    public required int E { get; init; }
    public string? ChannelId { get; init; }
}

/// <summary>
/// An emoji detected in message content.
/// </summary>
public sealed record EmojiOnMessage
{
    public required int S { get; init; }
    public required int E { get; init; }
    public string? EmojiId { get; init; }
}

/// <summary>
/// A plain link detected in message content.
/// </summary>
public sealed record LinkOnMessage
{
    public required int S { get; init; }
    public required int E { get; init; }
}

/// <summary>
/// A markdown segment in message content.
/// </summary>
public sealed record MarkdownOnMessage
{
    public required int S { get; init; }
    public required int E { get; init; }
    public string? Type { get; init; }
}

/// <summary>
/// A voice room link detected in message content.
/// </summary>
public sealed record VoiceLinkOnMessage
{
    public required int S { get; init; }
    public required int E { get; init; }
}

/// <summary>
/// A mention of a user or role in a message.
/// </summary>
public sealed record ApiMessageMention
{
    public string? CreateTime { get; init; }
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? RoleId { get; init; }
    public string? RoleName { get; init; }
    public int? S { get; init; }
    public int? E { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? MessageId { get; init; }
    public string? SenderId { get; init; }
}

/// <summary>
/// A file attachment on a message.
/// </summary>
public sealed record ApiMessageAttachment
{
    public string? Filename { get; init; }
    public string? FileType { get; init; }
    public int? Height { get; init; }
    public int? Size { get; init; }
    public string? Url { get; init; }
    public int? Width { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? MessageId { get; init; }
    public string? SenderId { get; init; }
}

/// <summary>
/// A reference (reply/quote) to another message.
/// </summary>
public sealed record ApiMessageRef
{
    public string? MessageId { get; init; }
    public required string MessageRefId { get; init; }
    public int? RefType { get; init; }
    public required string MessageSenderId { get; init; }
    public string? MessageSenderUsername { get; init; }
    public string? MesagesSenderAvatar { get; init; }
    public string? MessageSenderClanNick { get; init; }
    public string? MessageSenderDisplayName { get; init; }
    public string? Content { get; init; }
    public bool? HasAttachment { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
}

/// <summary>
/// A reaction (emoji) on a message.
/// </summary>
public sealed record ApiMessageReaction
{
    public bool? Action { get; init; }
    public string? EmojiId { get; init; }
    public string? Emoji { get; init; }
    public string? Id { get; init; }
    public string? SenderId { get; init; }
    public string? SenderName { get; init; }
    public string? SenderAvatar { get; init; }
    public int? Count { get; init; }
    public string? ChannelId { get; init; }
    public int? Mode { get; init; }
    public string? ChannelLabel { get; init; }
    public string? MessageId { get; init; }
}

/// <summary>
/// Acknowledgement returned when a message is sent.
/// </summary>
public sealed record ChannelMessageAck
{
    public required string ChannelId { get; init; }
    public required string MessageId { get; init; }
    public int? Mode { get; init; }
    public int? Code { get; init; }
    public string? Username { get; init; }
    public string? CreateTime { get; init; }
    public string? UpdateTime { get; init; }
    public bool? Persistence { get; init; }
}

/// <summary>
/// A realtime channel presence entry.
/// </summary>
public sealed record Presence
{
    public required string UserId { get; init; }
    public required string SessionId { get; init; }
    public string? Username { get; init; }
    public string? Node { get; init; }
    public string? Status { get; init; }
}

/// <summary>
/// A channel joined via the realtime socket.
/// </summary>
public sealed record Channel
{
    public required string Id { get; init; }
    public string? ChannelLabel { get; init; }
    public Presence[]? Presences { get; init; }
    public Presence? Self { get; init; }
    public string? ClanLogo { get; init; }
    public string? CategoryName { get; init; }
}

/// <summary>
/// A full channel message received from the realtime socket.
/// </summary>
public sealed record ChannelMessage
{
    public required string Id { get; init; }
    public required string ChannelId { get; init; }
    public required string ChannelLabel { get; init; }
    public int? Code { get; init; }
    public ChannelMessageContent? Content { get; init; }
    public string? CreateTime { get; init; }
    public string? ClanId { get; init; }
    public IEnumerable<ApiMessageReaction>? Reactions { get; init; }
    public IEnumerable<ApiMessageMention>? Mentions { get; init; }
    public IEnumerable<ApiMessageAttachment>? Attachments { get; init; }
    public IEnumerable<ApiMessageRef>? References { get; init; }
    public ChannelMessage? ReferencedMessage { get; init; }
    public bool? Persistent { get; init; }
    public string? SenderId { get; init; }
    public string? UpdateTime { get; init; }
    public string? ClanLogo { get; init; }
    public string? CategoryName { get; init; }
    public string? Username { get; init; }
    public string? ClanNick { get; init; }
    public string? ClanAvatar { get; init; }
    public string? DisplayName { get; init; }
    public long? CreateTimeSeconds { get; init; }
    public long? UpdateTimeSeconds { get; init; }
    public int? Mode { get; init; }
    public string? MessageId { get; init; }
    public bool? HideEditted { get; init; }
    public bool? IsPublic { get; init; }
    public string? TopicId { get; init; }
}
