namespace Mezon.Sdk.Managers;

using Mezon.Sdk.Api;
using MezonSession = Mezon.Sdk.Session.Session;

public class SessionManager
{
    private readonly MezonRestApi _api;
    private MezonSession? _session;

    public SessionManager(MezonRestApi api, MezonSession? session = null)
    {
        _api = api;
        _session = session;
    }

    public MezonSession? GetSession() => _session;

    public async Task<MezonSession> AuthenticateAsync(string botId, string apiKey, CancellationToken ct = default)
    {
        var apiSession = await _api.AuthenticateAsync(botId, apiKey, ct);
        _session = new MezonSession(apiSession.Token, apiSession.RefreshToken, apiSession.UserId, apiSession.ApiUrl, apiSession.WsUrl);
        return _session;
    }
}
