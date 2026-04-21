using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Socket;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

public sealed class ClanManager
{
    private readonly MezonClient _client;
    private readonly MezonRestApi _api;
    private readonly MezonSocket _socket;
    private readonly Func<string> _getSessionToken;

    public ConcurrentDictionary<string, Clan> Cache { get; } = new();

    public ClanManager(MezonClient client, MezonRestApi api, MezonSocket socket, Func<string> getSessionToken)
    {
        _client = client;
        _api = api;
        _socket = socket;
        _getSessionToken = getSessionToken;
    }

    public Clan? Get(string clanId)
    {
        return Cache.TryGetValue(clanId, out var clan) ? clan : null;
    }

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
        }

        return clans;
    }

    public bool Remove(string clanId)
    {
        return Cache.TryRemove(clanId, out _);
    }

    public void Clear()
    {
        Cache.Clear();
    }

    public List<Clan> GetAll()
    {
        return Cache.Values.ToList();
    }
}
