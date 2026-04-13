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
    /// Note: MezonRestApi doesn't have GetUserAsync yet, so this is a placeholder
    /// </summary>
    public Task<User?> FetchAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement when GetUserAsync is added to MezonRestApi
        // For now, users are cached as they interact via messages
        return Task.FromResult<User?>(null);
    }

    /// <summary>
    /// Fetch all users in the clan (if API supports it)
    /// </summary>
    public Task<List<User>> FetchAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<User>();

        try
        {
            var sessionToken = _getSessionToken();
            // Note: This would require a ListClanUsers API endpoint
            // For now, this is a placeholder
            // In practice, users are typically cached as they interact
        }
        catch
        {
            // Ignore errors
        }

        return Task.FromResult(users);
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
