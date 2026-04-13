using System.Collections.Concurrent;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Managers;

/// <summary>
/// Manages channel cache for a clan, similar to TypeScript SDK's clan.channels
/// </summary>
public sealed class ChannelManager
{
    private readonly string _clanId;
    private readonly MezonRestApi _api;
    private readonly Func<string> _getSessionToken;

    /// <summary>
    /// Cache of all channels in this clan
    /// </summary>
    public ConcurrentDictionary<string, ApiChannelDescription> Cache { get; } = new();

    public ChannelManager(string clanId, MezonRestApi api, Func<string> getSessionToken)
    {
        _clanId = clanId;
        _api = api;
        _getSessionToken = getSessionToken;
    }

    /// <summary>
    /// Get a channel from cache by ID
    /// </summary>
    public ApiChannelDescription? Get(string channelId)
    {
        return Cache.TryGetValue(channelId, out var channel) ? channel : null;
    }

    /// <summary>
    /// Fetch channels from API and populate cache
    /// </summary>
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
            // Ignore errors
        }

        return channels;
    }

    /// <summary>
    /// Add or update a channel in cache
    /// </summary>
    public void Set(string channelId, ApiChannelDescription channel)
    {
        Cache[channelId] = channel;
    }

    /// <summary>
    /// Remove a channel from cache
    /// </summary>
    public bool Remove(string channelId)
    {
        return Cache.TryRemove(channelId, out _);
    }

    /// <summary>
    /// Clear all channels from cache
    /// </summary>
    public void Clear()
    {
        Cache.Clear();
    }

    /// <summary>
    /// Get all channels as a list
    /// </summary>
    public List<ApiChannelDescription> GetAll()
    {
        return Cache.Values.ToList();
    }

    /// <summary>
    /// Filter channels by type
    /// </summary>
    public List<ApiChannelDescription> GetByType(int channelType)
    {
        return Cache.Values.Where(c => c.Type == channelType).ToList();
    }
}
