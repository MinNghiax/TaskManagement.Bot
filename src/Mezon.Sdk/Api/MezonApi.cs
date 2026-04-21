using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;

namespace Mezon.Sdk.Api;

/// <summary>
/// REST API client for the Mezon server, built on <see cref="HttpClient"/>.
/// </summary>
public sealed class MezonApi : IMezonApi
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public string ApiKey { get; }
    public string BasePath { get; }
    public int TimeoutMs { get; }

    private int _retryCount = 3;
    private int _retryDelayMs = 500;

    public MezonApi(string apiKey, string basePath, int timeoutMs = 10_000, bool allowInvalidCertificates = false)
    {
        ApiKey = apiKey;
        BasePath = basePath;
        TimeoutMs = timeoutMs;

        // Create HttpClientHandler with optional SSL bypass
        var handler = new HttpClientHandler();
        if (allowInvalidCertificates)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(basePath.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };
        _http.DefaultRequestHeaders.Add("XApiKey", apiKey);

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };
    }

    /// <summary>Authenticate with app credentials.</summary>
    public async Task<ApiSession> MezAuthenticateAsync(
        string basicAuthUsername,
        string basicAuthPassword,
        ApiAuthenticateRequest body,
        CancellationToken cancellationToken = default)
    {
        // Authorization: Basic {base64(apiKey:)} - note the colon at the end
        // Body: JSON with Content-Type: application/proto
        // Response: Protobuf binary
        
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{basicAuthPassword}:"));
        
        using var req = new HttpRequestMessage(HttpMethod.Post, "v2/apps/authenticate/token");
        req.Headers.Add("Authorization", $"Basic {credentials}");
        req.Headers.Add("Accept", "application/x-protobuf");
        
        // Send JSON body with Content-Type: application/proto (matches TypeScript SDK)
        var jsonBody = JsonSerializer.Serialize(body, _json);
        req.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/proto");

        var resp = await RateLimitFetchAsync(req, cancellationToken);
        
        // Check if response is successful
        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[DEBUG] Error Response: {errorContent}");
            throw new HttpRequestException(
                $"Authentication failed with status {resp.StatusCode}: {errorContent}");
        }
        
        // Read response as binary (protobuf)
        var responseBytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
        Console.WriteLine($"[DEBUG] Response bytes length: {responseBytes.Length}");
        Console.WriteLine($"[DEBUG] ✅ Authentication successful!");
        
        // Decode protobuf Session message
        return DecodeSessionProtobuf(responseBytes);
    }
    
    /// <summary>
    /// Decode a protobuf Session message.
    /// Based on TypeScript SDK's Session interface:
    /// - created (bool, field 1)
    /// - token (string, field 2)
    /// - refresh_token (string, field 3)
    /// - user_id (string, field 4)
    /// - is_remember (bool, field 5)
    /// - api_url (string, field 6)
    /// - id_token (string, field 7)
    /// - ws_url (string, field 8)
    /// </summary>
    private static ApiSession DecodeSessionProtobuf(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, System.Text.Encoding.UTF8);
        
        string? token = null;
        string? refreshToken = null;
        string? userId = null;
        string? apiUrl = null;
        string? idToken = null;
        string? wsUrl = null;
        
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var fieldNumber = tag >> 3;
            var wireType = tag & 0x7;
            
            switch (fieldNumber)
            {
                case 1: // created (bool) - skip
                    if (wireType == 0) ReadVarint32(reader);
                    break;
                case 2: // token (string)
                    if (wireType == 2) token = ReadLengthDelimitedString(reader);
                    break;
                case 3: // refresh_token (string)
                    if (wireType == 2) refreshToken = ReadLengthDelimitedString(reader);
                    break;
                case 4: // user_id (string)
                    if (wireType == 2) userId = ReadLengthDelimitedString(reader);
                    break;
                case 5: // is_remember (bool) - skip
                    if (wireType == 0) ReadVarint32(reader);
                    break;
                case 6: // api_url (string)
                    if (wireType == 2) apiUrl = ReadLengthDelimitedString(reader);
                    break;
                case 7: // id_token (string)
                    if (wireType == 2) idToken = ReadLengthDelimitedString(reader);
                    break;
                case 8: // ws_url (string)
                    if (wireType == 2) wsUrl = ReadLengthDelimitedString(reader);
                    break;
                default:
                    // Skip unknown fields
                    SkipField(reader, wireType);
                    break;
            }
        }
        
        return new ApiSession
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = userId,
            ApiUrl = apiUrl,
            IdToken = idToken,
            WsUrl = wsUrl
        };
    }
    
    private static int ReadVarint32(BinaryReader reader)
    {
        int result = 0;
        int shift = 0;
        while (true)
        {
            var b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift >= 32) throw new InvalidDataException("Varint too long");
        }
        return result;
    }
    
    private static string ReadLengthDelimitedString(BinaryReader reader)
    {
        var length = ReadVarint32(reader);
        var bytes = reader.ReadBytes(length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
    
    private static void SkipField(BinaryReader reader, int wireType)
    {
        switch (wireType)
        {
            case 0: // Varint
                ReadVarint32(reader);
                break;
            case 1: // 64-bit
                reader.BaseStream.Position += 8;
                break;
            case 2: // Length-delimited
                var length = ReadVarint32(reader);
                reader.BaseStream.Position += length;
                break;
            case 5: // 32-bit
                reader.BaseStream.Position += 4;
                break;
            default:
                throw new InvalidDataException($"Unknown wire type: {wireType}");
        }
    }

    /// <summary>Create a new channel.</summary>
    public async Task<ApiChannelDescription> CreateChannelDescAsync(
        string bearerToken, ApiCreateChannelDescRequest body,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "v1/api/channel", bearerToken,
            JsonContent.Create(body, options: _json), cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiChannelDescription>(_json, cancellationToken)
            ?? throw new InvalidOperationException("Empty channel response");
    }

    /// <summary>List clans.</summary>
    public async Task<ApiClanDescList> ListClanDescsAsync(
        string bearerToken, int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("limit", limit), ("state", state), ("cursor", cursor));
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/clan?{query}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiClanDescList>(_json, cancellationToken)
            ?? new ApiClanDescList();
    }

    /// <summary>Get channel detail.</summary>
    public async Task<ApiChannelDescription> ListChannelDetailAsync(
        string bearerToken, string channelId,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/channel/{channelId}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiChannelDescription>(_json, cancellationToken)
            ?? throw new InvalidOperationException("Empty channel detail response");
    }

    /// <summary>List channels.</summary>
    public async Task<ApiChannelDescList> ListChannelDescsAsync(
        string bearerToken, int? channelType = null, string? clanId = null,
        int? limit = null, int? state = null, string? cursor = null, bool? isMobile = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("channel_type", channelType), ("clan_id", clanId),
            ("limit", limit), ("state", state), ("cursor", cursor), ("is_mobile", isMobile));
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/channel?{query}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiChannelDescList>(_json, cancellationToken)
            ?? new ApiChannelDescList();
    }

    /// <summary>List users in a voice channel.</summary>
    public async Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string bearerToken, string? clanId = null, int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("clan_id", clanId), ("limit", limit));
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/voice/channel/users?{query}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiVoiceChannelUserList>(_json, cancellationToken)
            ?? new ApiVoiceChannelUserList();
    }

    /// <summary>Update role fields.</summary>
    public async Task UpdateRoleAsync(
        string bearerToken, string roleId, MezonUpdateRoleBody body,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(HttpMethod.Put, $"v1/api/role/{roleId}", bearerToken,
            JsonContent.Create(body, options: _json), cancellationToken);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>List roles in a clan.</summary>
    public async Task<ApiRoleListEventResponse> ListRolesAsync(
        string bearerToken, string? clanId = null, int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("clan_id", clanId), ("limit", limit), ("state", state), ("cursor", cursor));
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/role?{query}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiRoleListEventResponse>(_json, cancellationToken)
            ?? new ApiRoleListEventResponse();
    }

    /// <summary>Add quick menu access.</summary>
    public async Task AddQuickMenuAccessAsync(
        string bearerToken, ApiQuickMenuAccessRequest body,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "v1/api/quickmenu/access", bearerToken,
            JsonContent.Create(body, options: _json), cancellationToken);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>Delete quick menu access.</summary>
    public async Task DeleteQuickMenuAccessAsync(
        string bearerToken, string? id = null, string? clanId = null,
        string? botId = null, string? menuName = null, string? background = null, string? actionMsg = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("id", id), ("clan_id", clanId), ("bot_id", botId),
            ("menu_name", menuName), ("background", background), ("action_msg", actionMsg));
        var req = new HttpRequestMessage(HttpMethod.Delete, $"v1/api/quickmenu/access?{query}");
        await SendAsync(req, bearerToken, cancellationToken);
    }

    /// <summary>List quick menu access entries.</summary>
    public async Task<ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
        string bearerToken, string? botId = null, string? channelId = null, int? menuType = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("bot_id", botId), ("channel_id", channelId), ("menu_type", menuType));
        var resp = await SendAsync(HttpMethod.Get, $"v1/api/quickmenu/access?{query}", bearerToken, cancellationToken: cancellationToken);
        return await resp.Content.ReadFromJsonAsync<ApiQuickMenuAccessList>(_json, cancellationToken)
            ?? new ApiQuickMenuAccessList();
    }

    /// <summary>Play media in a voice channel.</summary>
    public async Task PlayMediaAsync(
        string bearerToken, PlayMediaRequest body,
        CancellationToken cancellationToken = default)
    {
        var resp = await SendAsync(HttpMethod.Post, "v1/api/voice/media", bearerToken,
            JsonContent.Create(body, options: _json), cancellationToken);
        resp.EnsureSuccessStatusCode();
    }

    // ─── Internal Helpers ─────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, string bearerToken,
        HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(method, path) { Content = content };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        return await RateLimitFetchAsync(req, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage req, string bearerToken, CancellationToken cancellationToken)
    {
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        return await RateLimitFetchAsync(req, cancellationToken);
    }

    /// <summary>
    /// Retry on 429 with exponential backoff.
    /// </summary>
    private async Task<HttpResponseMessage> RateLimitFetchAsync(
        HttpRequestMessage req, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= _retryCount; attempt++)
        {
            var resp = await _http.SendAsync(req, cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                resp.Dispose();
                var delay = _retryDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            return resp;
        }
        throw new HttpRequestException("Rate limit exceeded after retries");
    }
    private static string BuildQuery(params (string name, object? value)[] args)
    {
        var parts = args
            .Where(a => a.value != null)
            .Select(a => $"{a.name}={Uri.EscapeDataString(a.value?.ToString() ?? "")}");
        return string.Join("&", parts);
    }
}
