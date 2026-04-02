namespace Mezon.Sdk.Api;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Mezon.Sdk.Proto;
using MezonSession = Mezon.Sdk.Session.Session;

/// <summary>
/// Mezon API client. All non-auth calls use POST with protobuf binary body/response
/// to /mezon.api.Mezon/MethodName. Auth uses /v2/apps/authenticate/token with JSON
/// body but protobuf binary response.
/// </summary>
public class MezonRestApi
{
    private readonly HttpClient _http;
    private readonly string _basePath;

    public MezonRestApi(string apiKey, string basePath, int timeoutMs = 7000)
    {
        _basePath = basePath.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
    }

    // -------------------------------------------------------------------------
    // Auth — POST /v2/apps/authenticate/token
    // Body: JSON  |  Response: protobuf binary (Proto.Session)
    // -------------------------------------------------------------------------
    public async Task<MezonSession> AuthenticateAsync(string botId, string apiKey, CancellationToken ct = default)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:"));
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/v2/apps/authenticate/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
        var body = new { account = new { appid = botId, token = apiKey } };
        // TS SDK sends JSON body with Content-Type: application/proto
        var jsonBody = JsonSerializer.Serialize(body);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/proto");

        var bytes = await SendAndReadAsync(request, ct);
        var proto = Session.Parser.ParseFrom(bytes);
        return new MezonSession(proto.Token, proto.RefreshToken, proto.UserId.ToString(), proto.ApiUrl, proto.WsUrl);
    }

    // -------------------------------------------------------------------------
    // List clans — POST /mezon.api.Mezon/ListClanDescs
    // -------------------------------------------------------------------------
    public async Task<IReadOnlyList<ClanDesc>> ListClansAsync(string token, CancellationToken ct = default)
    {
        var body = new ListClanDescRequest { Limit = 100 }.ToByteArray();
        var resp = await CallAsync(token, "ListClanDescs", body, ct);
        return ClanDescList.Parser.ParseFrom(resp).Clandesc;
    }

    // -------------------------------------------------------------------------
    // List channels — POST /mezon.api.Mezon/ListChannelDescs
    // -------------------------------------------------------------------------
    public async Task<IReadOnlyList<ChannelDescription>> ListChannelsAsync(
        string token, string? clanId = null, int? channelType = null, int limit = 100, CancellationToken ct = default)
    {
        var req = new ListChannelDescsRequest { Limit = limit };
        if (long.TryParse(clanId, out var cid)) req.ClanId = cid;
        if (channelType.HasValue) req.ChannelType = channelType.Value;
        var resp = await CallAsync(token, "ListChannelDescs", req.ToByteArray(), ct);
        return ChannelDescList.Parser.ParseFrom(resp).Channeldesc;
    }

    // -------------------------------------------------------------------------
    // Create DM channel — POST /mezon.api.Mezon/CreateChannelDesc
    // -------------------------------------------------------------------------
    public async Task<ChannelDescription?> CreateDmChannelAsync(string token, string userId, CancellationToken ct = default)
    {
        if (!long.TryParse(userId, out var uid)) return null;
        var req = new CreateChannelDescRequest { ClanId = 0, ChannelId = 0, CategoryId = 0, Type = 3, ChannelPrivate = 1 };
        req.UserIds.Add(uid);
        try
        {
            var resp = await CallAsync(token, "CreateChannelDesc", req.ToByteArray(), ct);
            return ChannelDescription.Parser.ParseFrom(resp);
        }
        catch { return null; }
    }

    // -------------------------------------------------------------------------
    // Get channel detail — POST /mezon.api.Mezon/ListChannelDetail
    // -------------------------------------------------------------------------
    public async Task<ChannelDescription?> GetChannelAsync(string token, string channelId, CancellationToken ct = default)
    {
        if (!long.TryParse(channelId, out var cid)) return null;
        var req = new ListChannelDetailRequest { ChannelId = cid };
        try
        {
            var resp = await CallAsync(token, "ListChannelDetail", req.ToByteArray(), ct);
            return ChannelDescription.Parser.ParseFrom(resp);
        }
        catch { return null; }
    }

    // -------------------------------------------------------------------------
    private async Task<byte[]> CallAsync(string bearerToken, string method, byte[] body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/mezon.api.Mezon/{method}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/proto"));
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/proto");
        // Mirror TS SDK behavior: on non-success, return empty bytes so proto parsers
        // return empty/default messages (e.g., empty channel list) instead of throwing.
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<byte[]> SendAndReadAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} — {request.RequestUri}\nBody: {body}");
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
