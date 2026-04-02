namespace Mezon.Sdk.Managers;

using Mezon.Sdk.Api;
using Mezon.Sdk.Proto;

public class ChannelManager(MezonRestApi api)
{
    public Task<ChannelDescription?> CreateDmChannelAsync(string token, string userId, CancellationToken ct = default)
        => api.CreateDmChannelAsync(token, userId, ct);
}
