using System.Collections.Concurrent;
using Mezon.Sdk;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Domain;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// Complete user resolution service with caching and preloading
/// Resolves full user information from userId without relying on chat events
/// </summary>
public class MezonUserService : IMezonUserService
{
    private readonly MezonClient _client;
    private readonly ILogger<MezonUserService> _logger;

    /// <summary>
    /// Global user cache: Key = userId, Value = MezonUser
    /// This cache is populated from:
    /// 1. Preload on startup (GetClanMembersAsync)
    /// 2. Real-time from messages (CacheUserFromMessage)
    /// 3. On-demand fetches (GetUserAsync)
    /// </summary>
    private readonly ConcurrentDictionary<string, MezonUser> _userCache = new();

    /// <summary>
    /// Track which clans have been preloaded to avoid duplicate fetches
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _preloadedClans = new();

    public MezonUserService(MezonClient client, ILogger<MezonUserService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Get full user information by userId
    /// Strategy: Cache → Clan API → All Clans → Placeholder
    /// </summary>
    public async Task<MezonUser?> GetUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[USER_SERVICE] GetUserAsync called with empty userId");
            return null;
        }

        try
        {
            // Strategy 1: Check cache first (fastest - O(1))
            if (_userCache.TryGetValue(userId, out var cachedUser))
            {
                _logger.LogDebug("[USER_SERVICE] ✅ Cache HIT for user {UserId} (fast path)", userId);
                return cachedUser;
            }

            _logger.LogDebug("[USER_SERVICE] ❌ Cache MISS for user {UserId}, fetching from API", userId);

            // Strategy 2: If clanId provided, try that clan first
            if (!string.IsNullOrWhiteSpace(clanId))
            {
                var user = await FetchUserFromClanAsync(userId, clanId, cancellationToken);
                if (user != null)
                {
                    _userCache[userId] = user;
                    _logger.LogInformation(
                        "[USER_SERVICE] ✅ Fetched user {UserId} from clan {ClanId}: {DisplayName}",
                        userId, clanId, user.GetDisplayName());
                    return user;
                }
            }

            // Strategy 3: Search all clans
            var allClans = _client.Clans.GetAll();
            _logger.LogDebug("[USER_SERVICE] Searching user {UserId} in {ClanCount} clans", userId, allClans.Count);

            foreach (var clan in allClans)
            {
                var user = await FetchUserFromClanAsync(userId, clan.Id, cancellationToken);
                if (user != null)
                {
                    _userCache[userId] = user;
                    _logger.LogInformation(
                        "[USER_SERVICE] ✅ Found user {UserId} in clan {ClanId} ({ClanName}): {DisplayName}",
                        userId, clan.Id, clan.Name, user.GetDisplayName());
                    return user;
                }
            }

            // Strategy 4: Create placeholder with partial userId
            _logger.LogWarning(
                "[USER_SERVICE] ❌ User {UserId} not found in any clan. Creating placeholder. " +
                "User may not have interacted yet or is not in any clan the bot has joined.",
                userId);

            var placeholder = new MezonUser
            {
                Id = userId,
                ClanId = clanId
            };

            // Cache placeholder to avoid repeated API calls
            _userCache[userId] = placeholder;

            return placeholder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_SERVICE] Error getting user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Refresh full user information by bypassing the local cache.
    /// </summary>
    public async Task<MezonUser?> RefreshUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[USER_SERVICE] RefreshUserAsync called with empty userId");
            return null;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(clanId))
            {
                var clanUser = await FetchUserFromClanAsync(userId, clanId, cancellationToken);
                if (clanUser != null)
                {
                    _userCache[userId] = clanUser;
                    return clanUser;
                }
            }

            var allClans = _client.Clans.GetAll();
            foreach (var clan in allClans)
            {
                var user = await FetchUserFromClanAsync(userId, clan.Id, cancellationToken);
                if (user != null)
                {
                    _userCache[userId] = user;
                    return user;
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_SERVICE] Error refreshing user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get display name for a user (convenience method)
    /// </summary>
    public async Task<string> GetDisplayNameAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Unknown User";
        }

        var user = await GetUserAsync(userId, clanId, cancellationToken);
        return user?.GetDisplayName() ?? "Unknown User";
    }

    /// <summary>
    /// Get all members of a clan and cache them
    /// This is the KEY method for preloading
    /// </summary>
    public async Task<List<MezonUser>> GetClanMembersAsync(string clanId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clanId))
        {
            _logger.LogWarning("[USER_SERVICE] GetClanMembersAsync called with empty clanId");
            return new List<MezonUser>();
        }

        try
        {
            _logger.LogInformation("[USER_SERVICE] Fetching members from clan {ClanId}", clanId);

            var clan = _client.Clans.Get(clanId);
            if (clan == null)
            {
                _logger.LogWarning("[USER_SERVICE] Clan {ClanId} not found in client cache", clanId);
                return new List<MezonUser>();
            }

            var members = new List<MezonUser>();

            // Try to fetch all users from clan
            try
            {
                var sdkUsers = await clan.Users.FetchAllAsync(cancellationToken);
                
                _logger.LogInformation(
                    "[USER_SERVICE] Fetched {Count} users from clan {ClanId} ({ClanName})",
                    sdkUsers.Count, clanId, clan.Name);

                foreach (var sdkUser in sdkUsers)
                {
                    if (string.IsNullOrWhiteSpace(sdkUser.Id))
                        continue;

                    var mezonUser = MezonUser.FromSdkUser(sdkUser, clanId);
                    members.Add(mezonUser);

                    // Cache the user
                    _userCache[sdkUser.Id] = mezonUser;

                    _logger.LogDebug(
                        "[USER_SERVICE] Cached user {UserId}: {DisplayName}",
                        sdkUser.Id, mezonUser.GetDisplayName());
                }

                // Mark clan as preloaded
                _preloadedClans[clanId] = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[USER_SERVICE] Failed to fetch users from clan {ClanId}", clanId);
            }

            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_SERVICE] Error getting clan members for {ClanId}", clanId);
            return new List<MezonUser>();
        }
    }

    /// <summary>
    /// Preload all members from all clans the bot has joined
    /// Call this on bot startup
    /// </summary>
    public async Task<int> PreloadAllClanMembersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var clans = _client.Clans.GetAll();
            _logger.LogInformation("[USER_SERVICE] 🚀 Preloading members from {ClanCount} clans", clans.Count);

            var totalUsers = 0;

            foreach (var clan in clans)
            {
                try
                {
                    var members = await GetClanMembersAsync(clan.Id, cancellationToken);
                    totalUsers += members.Count;

                    _logger.LogInformation(
                        "[USER_SERVICE] ✅ Preloaded {MemberCount} members from clan {ClanId} ({ClanName})",
                        members.Count, clan.Id, clan.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "[USER_SERVICE] ❌ Failed to preload members from clan {ClanId} ({ClanName})",
                        clan.Id, clan.Name);
                }
            }

            _logger.LogInformation(
                "[USER_SERVICE] 🎉 Preload complete: {TotalUsers} users cached from {ClanCount} clans",
                totalUsers, clans.Count);

            return totalUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_SERVICE] Error during preload");
            return 0;
        }
    }

    /// <summary>
    /// Cache a user from message data (for real-time updates)
    /// This ensures cache stays up-to-date as users interact
    /// </summary>
    public void CacheUserFromMessage(string userId, string? clanNick, string? displayName, string? username, string? avatarUrl, string? clanId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var user = MezonUser.FromMessage(userId, clanNick, displayName, username, avatarUrl, clanId);
        _userCache[userId] = user;

        _logger.LogDebug(
            "[USER_SERVICE] 📨 Cached user {UserId} from message: {DisplayName}",
            userId, user.GetDisplayName());
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public int GetCacheSize()
    {
        return _userCache.Count;
    }

    /// <summary>
    /// Fetch a specific user from a specific clan
    /// </summary>
    private async Task<MezonUser?> FetchUserFromClanAsync(string userId, string clanId, CancellationToken cancellationToken)
    {
        try
        {
            var clan = _client.Clans.Get(clanId);
            if (clan == null)
            {
                return null;
            }

            // Check clan's user cache first
            var cachedUser = clan.Users.Get(userId);
            if (cachedUser != null)
            {
                _logger.LogDebug("[USER_SERVICE] Found user {UserId} in clan {ClanId} SDK cache", userId, clanId);
                return MezonUser.FromSdkUser(cachedUser, clanId);
            }

            // Try to fetch the user
            var sdkUser = await clan.Users.FetchAsync(userId, cancellationToken);
            if (sdkUser != null)
            {
                _logger.LogDebug("[USER_SERVICE] Fetched user {UserId} from clan {ClanId} API", userId, clanId);
                return MezonUser.FromSdkUser(sdkUser, clanId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[USER_SERVICE] Failed to fetch user {UserId} from clan {ClanId}", userId, clanId);
            return null;
        }
    }
}
