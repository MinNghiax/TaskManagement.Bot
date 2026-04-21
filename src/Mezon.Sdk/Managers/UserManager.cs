using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

public sealed class UserManager
{
    private readonly string _clanId;
    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly Func<string> _getSessionToken;

    public ConcurrentDictionary<string, User> Cache { get; } = new();

    public UserManager(string clanId, MezonClient client, MezonRestApi api, Func<string> getSessionToken)
    {
        _clanId = clanId;
        _client = client;
        _api = api;
        _getSessionToken = getSessionToken;
    }

    public User? Get(string userId)
    {
        return Cache.TryGetValue(userId, out var user) ? user : null;
    }

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
        }

        return users.Count > 0 ? users : GetAll();
    }

    public void Set(string userId, User user)
    {
        Cache[userId] = user;
    }

    public bool Remove(string userId)
    {
        return Cache.TryRemove(userId, out _);
    }

    public void Clear()
    {
        Cache.Clear();
    }

    public List<User> GetAll()
    {
        return Cache.Values.ToList();
    }
}
