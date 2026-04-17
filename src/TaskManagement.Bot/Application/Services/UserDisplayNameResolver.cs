using System.Collections.Concurrent;
using Mezon.Sdk;
using Mezon.Sdk.Structures;
using Microsoft.Extensions.Logging;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// Service to resolve user display names with priority: ClanNick → DisplayName → Username → "Unknown User"
/// Uses a local cache to store user information from messages and API calls
/// </summary>
public class UserDisplayNameResolver
{
    private readonly MezonClient _client;
    private readonly ILogger<UserDisplayNameResolver> _logger;
    
    /// <summary>
    /// Local cache of users: Key = userId, Value = User
    /// This cache is populated from message mentions and API calls
    /// </summary>
    private readonly ConcurrentDictionary<string, User> _userCache = new();

    public UserDisplayNameResolver(MezonClient client, ILogger<UserDisplayNameResolver> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    /// <summary>
    /// Cache a user from message mention data
    /// This is called when we receive messages with user mentions
    /// </summary>
    public void CacheUserFromMention(string userId, string? clanNick, string? displayName, string? username, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;
            
        var user = new User(
            userId: userId,
            client: _client,
            api: _client.Api,
            getSessionToken: () => _client.CurrentSession?.Token ?? "",
            username: username,
            displayName: displayName,
            clanNick: clanNick,
            avatarUrl: avatarUrl);
            
        _userCache[userId] = user;
        
        _logger.LogDebug(
            "[USER_CACHE] Cached user {UserId} from mention: ClanNick={ClanNick}, DisplayName={DisplayName}, Username={Username}",
            userId, clanNick ?? "null", displayName ?? "null", username ?? "null");
    }

    /// <summary>
    /// Resolve display name for a user with priority: ClanNick → DisplayName → Username → "Unknown User"
    /// NEVER returns userId - this is a UX requirement
    /// </summary>
    /// <param name="userId">User ID to resolve</param>
    /// <param name="clanId">Optional clan ID to search in specific clan first</param>
    /// <returns>Display name following priority order, NEVER returns userId</returns>
    public async Task<string> ResolveDisplayNameAsync(string userId, string? clanId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Unknown User";
        }

        try
        {
            User? user = null;

            // Strategy 1: Check local cache first (fastest - populated from messages)
            if (_userCache.TryGetValue(userId, out var cachedUser))
            {
                _logger.LogDebug("[USER_RESOLVER] ✅ Found user {UserId} in local cache (fast path)", userId);
                var displayName = ResolveDisplayName(cachedUser);
                
                _logger.LogInformation(
                    "[USER_RESOLVER] ✅ Resolved user {UserId} from cache: ClanNick={ClanNick}, DisplayName={DisplayName}, Username={Username}, Result={Result}",
                    userId, cachedUser.ClanNick ?? "null", cachedUser.DisplayName ?? "null", cachedUser.Username ?? "null", displayName);
                
                return displayName;
            }

            // Strategy 2: If clanId is provided, try that clan
            if (!string.IsNullOrWhiteSpace(clanId))
            {
                _logger.LogDebug("[USER_RESOLVER] Trying to find user {UserId} in clan {ClanId}", userId, clanId);
                user = await TryGetUserFromClanAsync(userId, clanId);
                
                if (user != null)
                {
                    _logger.LogInformation("[USER_RESOLVER] Found user {UserId} in specified clan {ClanId}", userId, clanId);
                    // Cache for future use
                    _userCache[userId] = user;
                }
            }

            // Strategy 3: Search all clans
            if (user == null)
            {
                _logger.LogDebug("[USER_RESOLVER] User {UserId} not found in specified clan, searching all clans", userId);
                user = await TryGetUserFromAllClansAsync(userId);
                
                if (user != null)
                {
                    _logger.LogInformation("[USER_RESOLVER] Found user {UserId} in one of the clans", userId);
                    // Cache for future use
                    _userCache[userId] = user;
                }
            }

            // Apply priority: ClanNick → DisplayName → Username → "Unknown User"
            if (user != null)
            {
                var displayName = ResolveDisplayName(user);
                
                _logger.LogInformation(
                    "[USER_RESOLVER] ✅ Resolved user {UserId}: ClanNick={ClanNick}, DisplayName={DisplayName}, Username={Username}, Result={Result}",
                    userId, user.ClanNick ?? "null", user.DisplayName ?? "null", user.Username ?? "null", displayName);
                
                return displayName;
            }

            // ❌ NEVER return userId - bad UX
            _logger.LogWarning(
                "[USER_RESOLVER] ❌ Could not find user {UserId} in cache or any clan. Returning 'Unknown User' instead of userId. " +
                "This might mean: 1) User hasn't sent any messages yet, 2) User is not in any clan the bot has joined, 3) Invalid userId",
                userId);
            
            return "Unknown User";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_RESOLVER] ❌ Error resolving display name for user {UserId}. Returning 'Unknown User'", userId);
            return "Unknown User";
        }
    }

    /// <summary>
    /// Resolve display name from User object with priority: ClanNick → DisplayName → Username → "Unknown User"
    /// NEVER returns userId - this is a UX requirement
    /// </summary>
    public string ResolveDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.ClanNick))
            return user.ClanNick;

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName;

        if (!string.IsNullOrWhiteSpace(user.Username))
            return user.Username;

        // ❌ NEVER return userId - bad UX
        _logger.LogWarning(
            "[USER_RESOLVER] User {UserId} has no displayable name (ClanNick, DisplayName, Username all null). Returning 'Unknown User'",
            user.Id);
        
        return "Unknown User";
    }

    /// <summary>
    /// Batch resolve display names for multiple users
    /// </summary>
    public async Task<Dictionary<string, string>> ResolveDisplayNamesAsync(IEnumerable<string> userIds, string? clanId = null)
    {
        var result = new Dictionary<string, string>();
        
        foreach (var userId in userIds.Distinct())
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                result[userId] = await ResolveDisplayNameAsync(userId, clanId);
            }
        }

        return result;
    }

    private async Task<User?> TryGetUserFromClanAsync(string userId, string clanId)
    {
        try
        {
            var clan = _client.Clans.Get(clanId);
            if (clan == null)
            {
                _logger.LogDebug("[USER_RESOLVER] Clan {ClanId} not found in client cache", clanId);
                return null;
            }

            // Strategy 1: Check cache first (fastest)
            var cachedUser = clan.Users.Get(userId);
            if (cachedUser != null)
            {
                _logger.LogDebug("[USER_RESOLVER] ✅ Found user {UserId} in clan {ClanId} cache (fast path)", userId, clanId);
                return cachedUser;
            }

            // Strategy 2: Fetch all users to populate cache (if not already done)
            _logger.LogDebug("[USER_RESOLVER] User {UserId} not in cache, fetching all users from clan {ClanId}", userId, clanId);
            try
            {
                var allUsers = await clan.Users.FetchAllAsync();
                _logger.LogDebug("[USER_RESOLVER] Fetched {Count} users from clan {ClanId}", allUsers.Count, clanId);
                
                // Try again after fetching all users
                cachedUser = clan.Users.Get(userId);
                if (cachedUser != null)
                {
                    _logger.LogInformation("[USER_RESOLVER] ✅ Found user {UserId} in clan {ClanId} after fetching all users", userId, clanId);
                    return cachedUser;
                }
                
                _logger.LogDebug("[USER_RESOLVER] User {UserId} not found in clan {ClanId} even after fetching all users", userId, clanId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[USER_RESOLVER] Failed to fetch all users from clan {ClanId}", clanId);
            }

            // Strategy 3: Try direct fetch as last resort (might work if user is in clan but FetchAll didn't get them)
            _logger.LogDebug("[USER_RESOLVER] Trying direct fetch for user {UserId} from clan {ClanId}", userId, clanId);
            try
            {
                var user = await clan.Users.FetchAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation("[USER_RESOLVER] ✅ Fetched user {UserId} directly from clan {ClanId} API", userId, clanId);
                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[USER_RESOLVER] Direct fetch failed for user {UserId} from clan {ClanId}", userId, clanId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[USER_RESOLVER] Failed to get user {UserId} from clan {ClanId}", userId, clanId);
            return null;
        }
    }

    private async Task<User?> TryGetUserFromAllClansAsync(string userId)
    {
        var clans = _client.Clans.GetAll();
        
        _logger.LogDebug("[USER_RESOLVER] Searching for user {UserId} in {ClanCount} clans", userId, clans.Count);

        foreach (var clan in clans)
        {
            _logger.LogDebug("[USER_RESOLVER] Checking clan {ClanId} ({ClanName}) for user {UserId}", clan.Id, clan.Name, userId);
            
            var user = await TryGetUserFromClanAsync(userId, clan.Id);
            if (user != null)
            {
                _logger.LogInformation("[USER_RESOLVER] ✅ Found user {UserId} in clan {ClanId} ({ClanName})", userId, clan.Id, clan.Name);
                return user;
            }
        }

        _logger.LogWarning("[USER_RESOLVER] ❌ User {UserId} not found in any of the {ClanCount} clans", userId, clans.Count);
        return null;
    }
}
