using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

/// <summary>
/// Manages user cache for a clan, similar to TypeScript SDK's clan.users
/// </summary>
public sealed class UserManager
{
    private readonly string _clanId;
    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly Func<string> _getSessionToken;

    /// <summary>
    /// Cache of all users in this clan
    /// </summary>
    public ConcurrentDictionary<string, User> Cache { get; } = new();

    public UserManager(string clanId, MezonClient client, MezonRestApi api, Func<string> getSessionToken)
    {
        _clanId = clanId;
        _client = client;
        _api = api;
        _getSessionToken = getSessionToken;
    }

    /// <summary>
    /// Get a user from cache by ID
    /// </summary>
    public User? Get(string userId)
    {
        return Cache.TryGetValue(userId, out var user) ? user : null;
    }

    /// <summary>
    /// Fetch a specific user and add to cache
    /// </summary>
    public async Task<User?> FetchAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (Cache.TryGetValue(userId, out var cachedUser))
        {
            return cachedUser;
        }

        var users = await FetchAllAsync(cancellationToken);
        return users.FirstOrDefault(x => string.Equals(x.Id, userId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Fetch all users in the clan (if API supports it)
    /// </summary>
    public async Task<List<User>> FetchAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<User>();

        try
        {
            var sessionToken = _getSessionToken();
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return GetAll();
            }

            var clanUsers = await _api.ListClanUsersAsync(
                sessionToken,
                _clanId,
                ct: cancellationToken);

            foreach (var clanUser in clanUsers.ClanUsers ?? [])
            {
                if (string.IsNullOrWhiteSpace(clanUser.User?.Id))
                {
                    continue;
                }

                var user = new User(
                    userId: clanUser.User.Id,
                    client: _client,
                    api: _api,
                    getSessionToken: _getSessionToken,
                    username: clanUser.User.Username,
                    displayName: clanUser.User.DisplayName,
                    clanNick: clanUser.ClanNick,
                    clanAvatar: clanUser.ClanAvatar,
                    avatarUrl: clanUser.User.AvatarUrl);

                Cache[user.Id] = user;
                users.Add(user);
            }
        }
        catch
        {
            // Ignore errors
        }

        return users.Count > 0 ? users : GetAll();
    }

    /// <summary>
    /// Add or update a user in cache
    /// </summary>
    public void Set(string userId, User user)
    {
        Cache[userId] = user;
    }

    /// <summary>
    /// Remove a user from cache
    /// </summary>
    public bool Remove(string userId)
    {
        return Cache.TryRemove(userId, out _);
    }

    /// <summary>
    /// Clear all users from cache
    /// </summary>
    public void Clear()
    {
        Cache.Clear();
    }

    /// <summary>
    /// Get all users as a list
    /// </summary>
    public List<User> GetAll()
    {
        return Cache.Values.ToList();
    }
}
