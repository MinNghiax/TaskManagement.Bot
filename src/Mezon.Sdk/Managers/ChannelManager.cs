using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

public sealed class ChannelManager
{
    private readonly string _clanId;
    private readonly MezonRestApi _api;
    private readonly Func<string> _getSessionToken;

    public ConcurrentDictionary<string, ApiChannelDescription> Cache { get; } = new();

    public ChannelManager(string clanId, MezonRestApi api, Func<string> getSessionToken)
    {
        _clanId = clanId;
        _api = api;
        _getSessionToken = getSessionToken;
    }

    public ApiChannelDescription? Get(string channelId)
    {
        return Cache.TryGetValue(channelId, out var channel) ? channel : null;
    }

    public async Task<List<ApiChannelDescription>> FetchAllAsync(CancellationToken cancellationToken = default)
    {
        var channels = new List<ApiChannelDescription>();

        try
        {
            var sessionToken = _getSessionToken();
            var channelList = await _api.ListChannelDescsAsync(
                sessionToken, 
                clanId: _clanId, 
                ct: cancellationToken
            );

            if (channelList.ChannelDescs != null)
            {
                foreach (var channel in channelList.ChannelDescs)
                {
                    var channelId = channel.ChannelId ?? "";
                    if (!string.IsNullOrEmpty(channelId))
                    {
                        Cache[channelId] = channel;
                        channels.Add(channel);
                    }
                }
            }
        }
        catch
        {
        }

        return channels;
    }

    public void Set(string channelId, ApiChannelDescription channel)
    {
        Cache[channelId] = channel;
    }

    public bool Remove(string channelId)
    {
        return Cache.TryRemove(channelId, out _);
    }

    public void Clear()
    {
        Cache.Clear();
    }

    public List<ApiChannelDescription> GetAll()
    {
        return Cache.Values.ToList();
    }

    public List<ApiChannelDescription> GetByType(int channelType)
    {
        return Cache.Values.Where(c => c.Type == channelType).ToList();
    }
}
