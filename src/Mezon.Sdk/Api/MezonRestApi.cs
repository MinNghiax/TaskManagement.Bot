namespace Mezon.Sdk.Api;

using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Mezon.Sdk.Proto;
using Mezon.Sdk.Domain;
using MezonSession = Mezon.Sdk.Domain.Session;

/// <summary>
/// Mezon API client. All non-auth calls use POST with protobuf binary body/response
/// to /mezon.api.Mezon/MethodName. Auth uses /v2/apps/authenticate/token with JSON
/// body but protobuf binary response.
/// </summary>
public class MezonRestApi
{
    private readonly HttpClient _http;
    private readonly string _basePath;

    public MezonRestApi(string apiKey, string basePath, int timeoutMs = 7000, bool allowInvalidCertificates = false)
    {
        _basePath = basePath.TrimEnd('/');
        
        var handler = new HttpClientHandler();
        if (allowInvalidCertificates)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
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
        var proto = Proto.Session.Parser.ParseFrom(bytes);
        
        return new MezonSession(new Domain.ApiSession
        {
            Token = proto.Token,
            RefreshToken = proto.RefreshToken,
            UserId = proto.UserId.ToString(),
            ApiUrl = proto.ApiUrl,
            WsUrl = proto.WsUrl,
            IdToken = proto.IdToken
        });
    }

    // -------------------------------------------------------------------------
    // List clans — POST /mezon.api.Mezon/ListClanDescs
    // -------------------------------------------------------------------------
    public async Task<IReadOnlyList<ClanDesc>> ListClansAsync(string token, CancellationToken ct = default)
    {
        var body = new ListClanDescRequest { Limit = 100 }.ToByteArray();
        var resp = await CallAsync(token, "ListClanDescs", body, ct);
        var result = ClanDescList.Parser.ParseFrom(resp);
        return result.Clandesc;
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
    // Create channel — POST /mezon.api.Mezon/CreateChannelDesc
    // -------------------------------------------------------------------------
    public async Task<Domain.ApiChannelDescription> CreateChannelDescAsync(
        string token, Domain.ApiCreateChannelDescRequest body, CancellationToken ct = default)
    {
        var req = new CreateChannelDescRequest
        {
            ClanId = long.TryParse(body.ClanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(body.ChannelId, out var chid) ? chid : 0,
            CategoryId = long.TryParse(body.CategoryId, out var catid) ? catid : 0,
            Type = body.Type ?? 0,
            ChannelPrivate = body.ChannelPrivate ?? 0,
        };
        if (body.UserIds != null)
            foreach (var uid in body.UserIds)
                if (long.TryParse(uid, out var u)) req.UserIds.Add(u);

        var resp = await CallAsync(token, "CreateChannelDesc", req.ToByteArray(), ct);
        var proto = ChannelDescription.Parser.ParseFrom(resp);
        return new Domain.ApiChannelDescription
        {
            ClanId = proto.ClanId.ToString(),
            ChannelId = proto.ChannelId.ToString(),
            Type = (int)proto.Type,
            ChannelLabel = proto.ChannelLabel,
        };
    }

    // -------------------------------------------------------------------------
    // List channels — POST /mezon.api.Mezon/ListChannelDescs (returns domain model)
    // -------------------------------------------------------------------------
    public async Task<Domain.ApiChannelDescList> ListChannelDescsAsync(
        string token, int? channelType = null, string? clanId = null,
        int? limit = null, int? state = null, string? cursor = null,
        bool? isMobile = null, CancellationToken ct = default)
    {
        var channels = await ListChannelsAsync(token, clanId, channelType, limit ?? 100, ct);
        return new Domain.ApiChannelDescList
        {
            ChannelDescs = channels.Select(c => new Domain.ApiChannelDescription
            {
                ClanId = c.ClanId.ToString(),
                ChannelId = c.ChannelId.ToString(),
                Type = (int)c.Type,
                ChannelLabel = c.ChannelLabel,
            })
        };
    }

    // -------------------------------------------------------------------------
    // List voice channel users — POST /mezon.api.Mezon/ListChannelVoiceUsers
    // -------------------------------------------------------------------------
    public Task<Domain.ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string token, string? clanId = null, int? limit = null, CancellationToken ct = default)
    {
        // Voice users endpoint not in proto — return empty for now
        return Task.FromResult(new Domain.ApiVoiceChannelUserList
        {
            VoiceChannelUsers = Array.Empty<Domain.ApiVoiceChannelUser>()
        });
    }

    // -------------------------------------------------------------------------
    // List clan users — POST /mezon.api.Mezon/ListClanUsers
    // -------------------------------------------------------------------------
    public async Task<Domain.ApiClanUserList> ListClanUsersAsync(
        string token, string clanId, int? limit = null, CancellationToken ct = default)
    {
        if (!long.TryParse(clanId, out var cid))
        {
            return new Domain.ApiClanUserList { ClanUsers = Array.Empty<Domain.ApiClanUser>() };
        }
        
        var req = new ListClanUsersRequest { ClanId = cid };
        
        try
        {
            var resp = await CallAsync(token, "ListClanUsers", req.ToByteArray(), ct, throwOnNonSuccess: true);
            
            if (resp == null || resp.Length == 0)
            {
                return new Domain.ApiClanUserList { ClanUsers = Array.Empty<Domain.ApiClanUser>() };
            }
            
            var result = ClanUserList.Parser.ParseFrom(resp);
            
            return new Domain.ApiClanUserList
            {
                ClanUsers = result.ClanUsers.Select(cu => new Domain.ApiClanUser
                {
                    User = cu.User != null ? new Domain.ApiUser
                    {
                        Id = cu.User.Id.ToString(),
                        Username = cu.User.Username,
                        DisplayName = cu.User.DisplayName,
                        AvatarUrl = cu.User.AvatarUrl,
                        Online = cu.User.Online,
                    } : null,
                    ClanNick = cu.ClanNick,
                    ClanAvatar = cu.ClanAvatar,
                }).ToArray(),
                Cursor = result.Cursor
            };
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception)
        {
            Console.WriteLine($"[ERROR] ListClanUsers: Error fetching clan users");
            return new Domain.ApiClanUserList { ClanUsers = Array.Empty<Domain.ApiClanUser>() };
        }
    }

    // -------------------------------------------------------------------------
    // List role users — POST /mezon.api.Mezon/ListRoleUsers
    // -------------------------------------------------------------------------
    public async Task<ApiRoleUserList> ListRoleUsersAsync(
        string token, string roleId, int? limit = null, string? cursor = null,
        CancellationToken ct = default)
    {
        if (!long.TryParse(roleId, out var rid))
        {
            return new ApiRoleUserList { RoleUsers = Array.Empty<RoleUserListRoleUser>() };
        }

        var req = new ListRoleUsersRequest
        {
            RoleId = rid,
            Limit = limit ?? 100,
            Cursor = cursor ?? ""
        };

        try
        {
            var resp = await CallAsync(token, "ListRoleUsers", req.ToByteArray(), ct);
            
            if (resp == null || resp.Length == 0)
            {
                Console.WriteLine($"[DEBUG] ListRoleUsers returned empty response for role {roleId}");
                return new ApiRoleUserList { RoleUsers = Array.Empty<RoleUserListRoleUser>() };
            }
            
            var result = RoleUserList.Parser.ParseFrom(resp);
            Console.WriteLine($"[DEBUG] ListRoleUsers returned {result.RoleUsers.Count} users for role {roleId}");

            return new ApiRoleUserList
            {
                Cursor = result.Cursor,
                RoleUsers = result.RoleUsers.Select(u => new RoleUserListRoleUser
                {
                    Id = u.Id.ToString(),
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    LangTag = u.LangTag,
                    Location = u.Location,
                    Online = u.Online
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ListRoleUsers: {ex.Message}");
            return new ApiRoleUserList { RoleUsers = Array.Empty<RoleUserListRoleUser>() };
        }
    }

    // -------------------------------------------------------------------------
    // Update role — POST /mezon.api.Mezon/UpdateRole
    // -------------------------------------------------------------------------
    public async Task<bool> UpdateRoleAsync(
        string token, string roleId, MezonUpdateRoleBody body, CancellationToken ct = default)
    {
        if (!long.TryParse(roleId, out var rid))
        {
            return false;
        }

        var req = new UpdateRoleRequest
        {
            RoleId = rid,
            Title = body.Title ?? "",
            Color = body.Color ?? "",
            RoleIcon = body.RoleIcon ?? "",
            Description = body.Description ?? "",
            DisplayOnline = body.DisplayOnline ?? 0,
            AllowMention = body.AllowMention ?? 0,
            ClanId = long.TryParse(body.ClanId, out var cid) ? cid : 0,
            MaxPermissionId = long.TryParse(body.MaxPermissionId, out var mpid) ? mpid : 0
        };

        if (body.AddUserIds != null)
            foreach (var uid in body.AddUserIds)
                if (long.TryParse(uid, out var u)) req.AddUserIds.Add(u);

        if (body.ActivePermissionIds != null)
            foreach (var pid in body.ActivePermissionIds)
                if (long.TryParse(pid, out var p)) req.ActivePermissionIds.Add(p);

        if (body.RemoveUserIds != null)
            foreach (var uid in body.RemoveUserIds)
                if (long.TryParse(uid, out var u)) req.RemoveUserIds.Add(u);

        if (body.RemovePermissionIds != null)
            foreach (var pid in body.RemovePermissionIds)
                if (long.TryParse(pid, out var p)) req.RemovePermissionIds.Add(p);

        try
        {
            var resp = await CallAsync(token, "UpdateRole", req.ToByteArray(), ct);
            return resp != null && resp.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // List roles — POST /mezon.api.Mezon/ListRoles
    // -------------------------------------------------------------------------
    public async Task<ApiRoleListEventResponse> ListRolesAsync(
        string token, string? clanId = null, int? limit = null, int? state = null, 
        string? cursor = null, CancellationToken ct = default)
    {
        var req = new RoleListEventRequest
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            Limit = limit ?? 100,
            State = state ?? 0,
            Cursor = cursor ?? ""
        };

        try
        {
            var resp = await CallAsync(token, "ListRoles", req.ToByteArray(), ct);
            var result = RoleListEventResponse.Parser.ParseFrom(resp);
            
            return new ApiRoleListEventResponse
            {
                Limit = result.Limit,
                State = result.State,
                Cursor = result.Cursor,
                ClanId = result.ClanId.ToString(),
                Roles = new ApiRoleList
                {
                    Roles = result.Roles?.Roles.Select(r => new ApiRole
                    {
                        Id = r.Id.ToString(),
                        Title = r.Title,
                        Color = r.Color,
                        RoleIcon = r.RoleIcon,
                        Slug = r.Slug,
                        Description = r.Description,
                        CreatorId = r.CreatorId.ToString(),
                        ClanId = r.ClanId.ToString(),
                        Active = r.Active,
                        DisplayOnline = r.DisplayOnline,
                        AllowMention = r.AllowMention
                    }).ToArray() ?? Array.Empty<ApiRole>()
                }
            };
        }
        catch (Exception)
        {
            return new ApiRoleListEventResponse
            {
                Roles = new ApiRoleList { Roles = Array.Empty<ApiRole>() }
            };
        }
    }

    // -------------------------------------------------------------------------
    // Add quick menu access — POST /mezon.api.Mezon/AddQuickMenuAccess
    // Note: Proto definition exists but implementation needs verification
    // -------------------------------------------------------------------------
    public Task<bool> AddQuickMenuAccessAsync(
        string token, ApiQuickMenuAccessRequest body, CancellationToken ct = default)
    {
        // TODO: Implement when AddQuickMenuAccessRequest proto mapping is verified
        return Task.FromResult(false);
    }

    // -------------------------------------------------------------------------
    // Delete quick menu access — POST /mezon.api.Mezon/DeleteQuickMenuAccess
    // Note: Proto definition exists but implementation needs verification
    // -------------------------------------------------------------------------
    public Task<bool> DeleteQuickMenuAccessAsync(
        string token, string? id = null, string? clanId = null, string? botId = null,
        string? menuName = null, string? background = null, string? actionMsg = null,
        CancellationToken ct = default)
    {
        // TODO: Implement when DeleteQuickMenuAccessRequest proto mapping is verified
        return Task.FromResult(false);
    }

    // -------------------------------------------------------------------------
    // List quick menu access — POST /mezon.api.Mezon/ListQuickMenuAccess
    // -------------------------------------------------------------------------
    public async Task<ApiQuickMenuAccessList> ListQuickMenuAccessAsync(
        string token, string? botId = null, string? channelId = null, int? menuType = null,
        CancellationToken ct = default)
    {
        var req = new ListQuickMenuAccessRequest
        {
            BotId = long.TryParse(botId, out var bid) ? bid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            MenuType = menuType ?? 0
        };

        try
        {
            var resp = await CallAsync(token, "ListQuickMenuAccess", req.ToByteArray(), ct);
            var result = QuickMenuAccessList.Parser.ParseFrom(resp);
            
            return new ApiQuickMenuAccessList
            {
                ListMenus = result.ListMenus.Select(m => new ApiQuickMenuAccess
                {
                    Id = m.Id.ToString(),
                    BotId = m.BotId.ToString(),
                    ClanId = m.ClanId.ToString(),
                    ChannelId = m.ChannelId.ToString(),
                    MenuName = m.MenuName,
                    Background = m.Background,
                    ActionMsg = m.ActionMsg,
                    MenuType = m.MenuType
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ListQuickMenuAccess: {ex.Message}");
            return new ApiQuickMenuAccessList { ListMenus = Array.Empty<ApiQuickMenuAccess>() };
        }
    }

    // -------------------------------------------------------------------------
    // Play media — POST /mezon.api.Mezon/PlayMedia
    // Note: Proto definition not available yet, method stubbed for future implementation
    // -------------------------------------------------------------------------
    public Task<bool> PlayMediaAsync(
        string token, string roomName, string participantIdentity, string participantName,
        string url, string name, CancellationToken ct = default)
    {
        // TODO: Implement when PlayMediaRequest proto is available
        return Task.FromResult(false);
    }

    // -------------------------------------------------------------------------
    private async Task<byte[]> CallAsync(
        string bearerToken,
        string method,
        byte[] body,
        CancellationToken ct,
        bool throwOnNonSuccess = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/mezon.api.Mezon/{method}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/proto"));
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/proto");
        
        var response = await _http.SendAsync(request, ct);
        
        // Mirror TS SDK behavior: on non-success, return empty bytes so proto parsers
        // return empty/default messages (e.g., empty channel list) instead of throwing.
        if (!response.IsSuccessStatusCode)
        {
            if (throwOnNonSuccess)
            {
                throw new HttpRequestException(
                    $"Mezon API call '{method}' failed with status {(int)response.StatusCode} {response.ReasonPhrase}",
                    null,
                    response.StatusCode);
            }

            return Array.Empty<byte>();
        }
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
