namespace TaskManagement.Bot.Domain;

public class MezonUser
{
    public string Id { get; set; } = string.Empty;

    public string? ClanNick { get; set; }

    public string? DisplayName { get; set; }

    public string? Username { get; set; }

    public string? ClanAvatar { get; set; }

    public string? AvatarUrl { get; set; }

    public string? ClanId { get; set; }

    public bool? IsOnline { get; set; }

    public string? GetBestAvatar() => ClanAvatar ?? AvatarUrl;

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(ClanNick))
            return ClanNick;

        if (!string.IsNullOrWhiteSpace(DisplayName))
            return DisplayName;

        if (!string.IsNullOrWhiteSpace(Username))
            return Username;

        if (!string.IsNullOrWhiteSpace(Id) && Id.Length >= 8)
            return $"User_{Id.Substring(0, 8)}";

        return "Unknown User";
    }

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
