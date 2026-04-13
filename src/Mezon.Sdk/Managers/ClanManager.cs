using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Socket;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

/// <summary>
/// Manages clan cache and operations, similar to TypeScript SDK's client.clans
/// </summary>
public sealed class ClanManager
{
    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly MezonSocket _socket;
    private readonly Func<string> _getSessionToken;

    /// <summary>
    /// Cache of all clans the bot is a member of
    /// </summary>
    public ConcurrentDictionary<string, Clan> Cache { get; } = new();

    public ClanManager(MezonClient client, MezonRestApi api, MezonSocket socket, Func<string> getSessionToken)
    {
        _client = client;
        _api = api;
        _socket = socket;
        _getSessionToken = getSessionToken;
    }

    /// <summary>
    /// Get a clan from cache by ID
    /// </summary>
    public Clan? Get(string clanId)
    {
        return Cache.TryGetValue(clanId, out var clan) ? clan : null;
    }

    /// <summary>
    /// Fetch a clan from API and add to cache
    /// </summary>
    public async Task<Clan?> FetchAsync(string clanId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionToken = _getSessionToken();
            var clanList = await _api.ListClansAsync(sessionToken, cancellationToken);
            var clanData = clanList.FirstOrDefault(c => c.ClanId.ToString() == clanId);

            if (clanData == null)
            {
                return null;
            }

            var clan = new Clan(
                clanId: clanId,
                client: _client,
                api: _api,
                socket: _socket,
                getSessionToken: _getSessionToken,
                name: clanData.ClanName ?? "",
                welcomeChannelId: ""
            );

            Cache[clanId] = clan;
            return clan;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Load all clans the bot is a member of into cache
    /// </summary>
    public async Task<List<Clan>> FetchAllAsync(CancellationToken cancellationToken = default)
    {
        var clans = new List<Clan>();
        
        try
        {
            var sessionToken = _getSessionToken();
            var clanList = await _api.ListClansAsync(sessionToken, cancellationToken);

            foreach (var clanData in clanList)
            {
                var clanId = clanData.ClanId.ToString();
                var clan = new Clan(
                    clanId: clanId,
                    client: _client,
                    api: _api,
                    socket: _socket,
                    getSessionToken: _getSessionToken,
                    name: clanData.ClanName ?? "",
                    welcomeChannelId: ""
                );

                Cache[clanId] = clan;
                clans.Add(clan);
            }
        }
        catch
        {
            // Ignore errors
        }

        return clans;
    }

    /// <summary>
    /// Remove a clan from cache
    /// </summary>
    public bool Remove(string clanId)
    {
        return Cache.TryRemove(clanId, out _);
    }

    /// <summary>
    /// Clear all clans from cache
    /// </summary>
    public void Clear()
    {
        Cache.Clear();
    }

    /// <summary>
    /// Get all clans as a list
    /// </summary>
    public List<Clan> GetAll()
    {
        return Cache.Values.ToList();
    }
}
