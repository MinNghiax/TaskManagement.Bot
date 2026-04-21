using TaskManagement.Bot.Domain;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// Service for resolving full user information from userId
/// Provides caching and preloading capabilities
/// </summary>
public interface IMezonUserService
{
    /// <summary>
    /// Get full user information by userId and clanId
    /// </summary>
    /// <param name="userId">User ID to resolve</param>
    /// <param name="clanId">Clan ID where to search for the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full user information or null if not found</returns>
    Task<MezonUser?> GetUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh full user information from the Mezon API and update the cache.
    /// </summary>
    /// <param name="userId">User ID to refresh</param>
    /// <param name="clanId">Clan ID where to search for the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full user information or null if not found</returns>
    Task<MezonUser?> RefreshUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get display name for a user (convenience method)
    /// </summary>
    /// <param name="userId">User ID to resolve</param>
    /// <param name="clanId">Clan ID where to search for the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Display name following priority: ClanNick → DisplayName → Username → Partial UserId</returns>
    Task<string> GetDisplayNameAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all members of a clan and cache them
    /// </summary>
    /// <param name="clanId">Clan ID to fetch members from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all clan members</returns>
    Task<List<MezonUser>> GetClanMembersAsync(string clanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preload all members from all clans the bot has joined
    /// Call this on bot startup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of users cached</returns>
    Task<int> PreloadAllClanMembersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache a user from message data (for real-time updates)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="clanNick">Clan nickname</param>
    /// <param name="displayName">Display name</param>
    /// <param name="username">Username</param>
    /// <param name="avatarUrl">Avatar URL</param>
    /// <param name="clanId">Clan ID</param>
    void CacheUserFromMessage(string userId, string? clanNick, string? displayName, string? username, string? avatarUrl, string? clanId);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Number of users in cache</returns>
    int GetCacheSize();
}
