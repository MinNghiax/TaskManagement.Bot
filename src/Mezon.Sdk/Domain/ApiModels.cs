using System.Text.Json.Serialization;

namespace Mezon.Sdk.Domain;

/// <summary>
/// Response from the authenticate endpoint.
/// </summary>
public sealed record ApiSession
{
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string? ApiUrl { get; init; }
    public string? WsUrl { get; init; }
    public string? IdToken { get; init; }
}

/// <summary>
/// Request body sent to the authenticate endpoint.
/// </summary>
public sealed record ApiAuthenticateRequest
{
    public ApiAccountApp? Account { get; init; }
}

/// <summary>
/// App account credentials used for bot authentication.
/// </summary>
public sealed record ApiAccountApp
{
    [JsonPropertyName("appid")]
    public string? AppId { get; init; }
    
    [JsonPropertyName("app_name")]
    public string? AppName { get; init; }
    
    [JsonPropertyName("token")]
    public string? Token { get; init; }
    
    [JsonPropertyName("vars")]
    public Dictionary<string, string>? Vars { get; init; }
}

/// <summary>
/// Response when listing clans.
/// </summary>
public sealed record ApiClanDescList
{
    public IEnumerable<ApiClanDesc>? ClanDescs { get; init; }
}

/// <summary>
/// A clan's summary description.
/// </summary>
public sealed record ApiClanDesc
{
    public string? Banner { get; init; }
    public string? ClanId { get; init; }
    public string? ClanName { get; init; }
    public string? CreatorId { get; init; }
    public string? Logo { get; init; }
    public int? Status { get; init; }
    public int? BadgeCount { get; init; }
    public bool? IsOnboarding { get; init; }
    public string? WelcomeChannelId { get; init; }
    public string? OnboardingBanner { get; init; }
}

/// <summary>
/// Full channel description returned by channel detail/list endpoints.
/// </summary>
public sealed record ApiChannelDescription
{
    public required string ClanId { get; init; }
    public required string ChannelId { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int? Type { get; init; }
    public string? CreatorId { get; init; }
    public string? ChannelLabel { get; init; }
    public int? ChannelPrivate { get; init; }
    public string[]? Avatars { get; init; }
    public string[]? UserIds { get; init; }
    public ApiChannelMessageHeader? LastSentMessage { get; init; }
    public ApiChannelMessageHeader? LastSeenMessage { get; init; }
    public bool[]? Onlines { get; init; }
    public string? MeetingCode { get; init; }
    public int? CountMessUnread { get; init; }
    public int? Active { get; init; }
    public string? LastPinMessage { get; init; }
    public string[]? Usernames { get; init; }
    public string? CreatorName { get; init; }
    public long? CreateTimeSeconds { get; init; }
    public long? UpdateTimeSeconds { get; init; }
    public string[]? DisplayNames { get; init; }
    public string? ChannelAvatar { get; init; }
    public string? ClanName { get; init; }
    public string? AppId { get; init; }
    public bool? IsMute { get; init; }
    public int? AgeRestricted { get; init; }
    public string? Topic { get; init; }
    public int? E2ee { get; init; }
    public int? MemberCount { get; init; }
    public string? ParentId { get; init; }
}

/// <summary>
/// Paginated list of channel descriptions.
/// </summary>
public sealed record ApiChannelDescList
{
    public string? CacheableCursor { get; init; }
    public IEnumerable<ApiChannelDescription>? ChannelDescs { get; init; }
    public string? NextCursor { get; init; }
    public int? Page { get; init; }
    public string? PrevCursor { get; init; }
}

/// <summary>
/// Minimal channel message header for last-seen/last-sent tracking.
/// </summary>
public sealed record ApiChannelMessageHeader
{
    public string? Attachment { get; init; }
    public string? Content { get; init; }
    public string? Id { get; init; }
    public string? Mention { get; init; }
    public string? Reaction { get; init; }
    public string? Referece { get; init; }
    public string? SenderId { get; init; }
    public long? TimestampSeconds { get; init; }
}

/// <summary>
/// Request to create a new channel.
/// </summary>
public sealed record ApiCreateChannelDescRequest
{
    public string? CategoryId { get; init; }
    public string? ChannelId { get; init; }
    public string? ChannelLabel { get; init; }
    public int? ChannelPrivate { get; init; }
    public string? ClanId { get; init; }
    public string? ParentId { get; init; }
    public int? Type { get; init; }
    public IEnumerable<string>? UserIds { get; init; }
}

/// <summary>
/// Users currently in a voice channel.
/// </summary>
public sealed record ApiVoiceChannelUserList
{
    public IEnumerable<ApiVoiceChannelUser>? VoiceChannelUsers { get; init; }
}

/// <summary>
/// A user in a voice channel session.
/// </summary>
public sealed record ApiVoiceChannelUser
{
    public string? Id { get; init; }
    public string? ChannelId { get; init; }
    public string? Participant { get; init; }
    public IEnumerable<string>? UserIds { get; init; }
}

/// <summary>
/// Response when listing roles.
/// </summary>
public sealed record ApiRoleListEventResponse
{
    public string? ClanId { get; init; }
    public string? Cursor { get; init; }
    public int? Limit { get; init; }
    public ApiRoleList? Roles { get; init; }
    public int? State { get; init; }
}

/// <summary>
/// A list of roles with optional cursor.
/// </summary>
public sealed record ApiRoleList
{
    public string? CacheableCursor { get; init; }
    public string? NextCursor { get; init; }
    public string? PrevCursor { get; init; }
    public IEnumerable<ApiRole>? Roles { get; init; }
}

/// <summary>
/// A role description within a clan.
/// </summary>
public sealed record ApiRole
{
    public string? Id { get; init; }
    public string? RoleId { get; init; }
    public int? Active { get; init; }
    public int? AllowMention { get; init; }
    public IEnumerable<string>? ChannelIds { get; init; }
    public string? ClanId { get; init; }
    public string? Color { get; init; }
    public string? CreatorId { get; init; }
    public string? Description { get; init; }
    public int? DisplayOnline { get; init; }
    public int? MaxLevelPermission { get; init; }
    public ApiPermissionList? PermissionList { get; init; }
    public int? RoleChannelActive { get; init; }
    public string? RoleIcon { get; init; }
    public ApiRoleUserList? RoleUserList { get; init; }
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public int? OrderRole { get; init; }
}

/// <summary>
/// Permission list within a role.
/// </summary>
public sealed record ApiPermissionList
{
    public int? MaxLevelPermission { get; init; }
    public IEnumerable<ApiPermission>? Permissions { get; init; }
}

/// <summary>
/// A single permission entry.
/// </summary>
public sealed record ApiPermission
{
    public int? Active { get; init; }
    public string? Description { get; init; }
    public string? Id { get; init; }
    public int? Level { get; init; }
    public int? Scope { get; init; }
    public string? Slug { get; init; }
    public string? Title { get; init; }
}

/// <summary>
/// User list within a role.
/// </summary>
public sealed record ApiRoleUserList
{
    public string? Cursor { get; init; }
    public IEnumerable<RoleUserListRoleUser>? RoleUsers { get; init; }
}

/// <summary>
/// A user entry in a role's user list.
/// </summary>
public sealed record RoleUserListRoleUser
{
    public string? AvatarUrl { get; init; }
    public string? DisplayName { get; init; }
    public string? Id { get; init; }
    public string? LangTag { get; init; }
    public string? Location { get; init; }
    public bool? Online { get; init; }
    public string? Username { get; init; }
}

/// <summary>
/// Body sent when updating a role's fields.
/// </summary>
public sealed record MezonUpdateRoleBody
{
    public IEnumerable<string>? ActivePermissionIds { get; init; }
    public IEnumerable<string>? AddUserIds { get; init; }
    public int? AllowMention { get; init; }
    public string? ClanId { get; init; }
    public string? Color { get; init; }
    public string? Description { get; init; }
    public int? DisplayOnline { get; init; }
    public required string MaxPermissionId { get; init; }
    public IEnumerable<string>? RemovePermissionIds { get; init; }
    public IEnumerable<string>? RemoveUserIds { get; init; }
    public string? RoleIcon { get; init; }
    public string? Title { get; init; }
}

/// <summary>
/// Quick menu access item.
/// </summary>
public sealed record ApiQuickMenuAccess
{
    public string? ActionMsg { get; init; }
    public string? Background { get; init; }
    public string? BotId { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
    public string? Id { get; init; }
    public string? MenuName { get; init; }
    public int? MenuType { get; init; }
}

/// <summary>
/// Paginated list of quick menu items.
/// </summary>
public sealed record ApiQuickMenuAccessList
{
    public IEnumerable<ApiQuickMenuAccess>? QuickMenus { get; init; }
    public IEnumerable<ApiQuickMenuAccess>? ListMenus { get; init; }
}

/// <summary>
/// Request to add a quick menu entry.
/// </summary>
public sealed record ApiQuickMenuAccessRequest
{
    public string? ActionMsg { get; init; }
    public string? Background { get; init; }
    public string? BotId { get; init; }
    public string? ChannelId { get; init; }
    public string? ClanId { get; init; }
    public string? Id { get; init; }
    public string? MenuName { get; init; }
    public int? MenuType { get; init; }
}

/// <summary>
/// Request to play media in a voice channel.
/// </summary>
public sealed record PlayMediaRequest
{
    public required string RoomName { get; init; }
    public required string ParticipantIdentity { get; init; }
    public required string ParticipantName { get; init; }
    public required string Url { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// A user in the system.
/// </summary>
public sealed record ApiUser
{
    public string? Id { get; init; }
    public string? Username { get; init; }
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? LangTag { get; init; }
    public string? Location { get; init; }
    public string? Timezone { get; init; }
    public string? Metadata { get; init; }
    public string? FacebookId { get; init; }
    public string? GoogleId { get; init; }
    public string? GamecenterId { get; init; }
    public string? SteamId { get; init; }
    public bool? Online { get; init; }
    public int? EdgeCount { get; init; }
    public long? CreateTime { get; init; }
    public long? UpdateTime { get; init; }
}

/// <summary>
/// A user within a clan with clan-specific properties.
/// </summary>
public sealed record ApiClanUser
{
    public ApiUser? User { get; init; }
    public string? ClanNick { get; init; }
    public string? ClanAvatar { get; init; }
}

/// <summary>
/// List of users in a clan.
/// </summary>
public sealed record ApiClanUserList
{
    public IEnumerable<ApiClanUser>? ClanUsers { get; init; }
    public string? Cursor { get; init; }
}
