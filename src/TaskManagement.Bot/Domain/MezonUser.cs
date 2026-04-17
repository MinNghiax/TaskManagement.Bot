namespace TaskManagement.Bot.Domain;

/// <summary>
/// Represents a complete Mezon user with all available information
/// </summary>
public class MezonUser
{
    /// <summary>
    /// User ID (unique identifier)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Clan-specific nickname (highest priority for display)
    /// </summary>
    public string? ClanNick { get; set; }

    /// <summary>
    /// Global display name (second priority)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Global username (third priority)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Clan-specific avatar URL
    /// </summary>
    public string? ClanAvatar { get; set; }

    /// <summary>
    /// Global avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Clan ID where this user info was fetched from
    /// </summary>
    public string? ClanId { get; set; }

    /// <summary>
    /// Whether user is online
    /// </summary>
    public bool? IsOnline { get; set; }

    /// <summary>
    /// Get the best available avatar (ClanAvatar → AvatarUrl)
    /// </summary>
    public string? GetBestAvatar() => ClanAvatar ?? AvatarUrl;

    /// <summary>
    /// Get display name with priority: ClanNick → DisplayName → Username → Partial UserId
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(ClanNick))
            return ClanNick;

        if (!string.IsNullOrWhiteSpace(DisplayName))
            return DisplayName;

        if (!string.IsNullOrWhiteSpace(Username))
            return Username;

        // Fallback to partial userId
        if (!string.IsNullOrWhiteSpace(Id) && Id.Length >= 8)
            return $"User_{Id.Substring(0, 8)}";

        return "Unknown User";
    }

    /// <summary>
    /// Create from Mezon SDK User
    /// </summary>
    public static MezonUser FromSdkUser(Mezon.Sdk.Structures.User sdkUser, string? clanId = null)
    {
        return new MezonUser
        {
            Id = sdkUser.Id,
            ClanNick = sdkUser.ClanNick,
            DisplayName = sdkUser.DisplayName,
            Username = sdkUser.Username,
            ClanAvatar = sdkUser.ClanAvatar,
            AvatarUrl = sdkUser.AvatarUrl,
            ClanId = clanId
        };
    }

    /// <summary>
    /// Create from message data
    /// </summary>
    public static MezonUser FromMessage(string userId, string? clanNick, string? displayName, string? username, string? avatarUrl, string? clanId)
    {
        return new MezonUser
        {
            Id = userId,
            ClanNick = clanNick,
            DisplayName = displayName,
            Username = username,
            ClanAvatar = avatarUrl,
            AvatarUrl = avatarUrl,
            ClanId = clanId
        };
    }
}
