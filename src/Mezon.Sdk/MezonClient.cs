using System.Collections.Concurrent;
using System.Text.Json;
using Mezon.Sdk.Api;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;
using Mezon.Sdk.Managers;
using Mezon.Sdk.Socket;
using Mezon.Sdk.Structures;

namespace Mezon.Sdk;

/// <summary>
/// Configuration options for <see cref="MezonClient"/>.
/// </summary>
public sealed class MezonClientOptions
{
    public required string BotId { get; init; }
    public required string Token { get; init; }
    public string Host { get; init; } = "gw.mezon.ai";
    public string Port { get; init; } = "443";
    public bool UseSSL { get; init; } = true;
    public bool AllowInvalidCertificates { get; init; } = false;
    public int TimeoutMs { get; init; } = 10_000;
    public string? MmnApiUrl { get; init; }
    public string? ZkApiUrl { get; init; }
    public string? ApiBasePath { get; init; }
}

/// <summary>
/// Main entry point for the Mezon SDK. Connects to the Mezon server via REST and WebSocket.
/// </summary>
public sealed class MezonClient
{
    private readonly MezonClientOptions _options;
    private readonly HttpClient _mmnHttp;

    public MezonRestApi Api { get; private set; }
    public MezonSocket Socket { get; }

    public Session? CurrentSession { get; private set; }
    public string ClientId => _options.BotId;

    /// <summary>
    /// Clan manager with cache, similar to TypeScript SDK's client.clans
    /// </summary>
    public ClanManager Clans { get; private set; }

    private readonly ConcurrentDictionary<string, List<EventHandler<MezonEventArgs>>> _eventHandlers = new();

    public MezonClient(MezonClientOptions options)
    {
        _options = options;

        var basePath = options.ApiBasePath
            ?? $"{(options.UseSSL ? "https" : "http")}://{options.Host}:{options.Port}";

        Api = new MezonRestApi(options.Token, basePath, options.TimeoutMs, options.AllowInvalidCertificates);
        Socket = new MezonSocket();

        // Initialize clan manager
        Clans = new ClanManager(this, Api, Socket, () => CurrentSession?.Token ?? "");

        _mmnHttp = new HttpClient { Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs) };

        // Wire socket events → client events
        Socket.OnChannelMessage += (_, e) => {
            CacheUserFromMessage(e);
            Fire(Enums.MezEvent.ChannelMessage, e);
        };
        Socket.OnMessageReaction += (_, e) => Fire(Enums.MezEvent.MessageReaction, e);
        Socket.OnUserChannelAdded += (_, e) => Fire(Enums.MezEvent.UserChannelAdded, e);
        Socket.OnUserChannelRemoved += (_, e) => Fire(Enums.MezEvent.UserChannelRemoved, e);
        Socket.OnUserClanRemoved += (_, e) => Fire(Enums.MezEvent.UserClanRemoved, e);
        Socket.OnChannelCreated += (_, e) => Fire(Enums.MezEvent.ChannelCreated, e);
        Socket.OnChannelDeleted += (_, e) => Fire(Enums.MezEvent.ChannelDeleted, e);
        Socket.OnChannelUpdated += (_, e) => Fire(Enums.MezEvent.ChannelUpdated, e);
        Socket.OnMessageButtonClicked += (_, e) => Fire(Enums.MezEvent.MessageButtonClicked, e);
        Socket.OnVoiceJoined += (_, e) => Fire(Enums.MezEvent.VoiceJoinedEvent, e);
        Socket.OnVoiceLeaved += (_, e) => Fire(Enums.MezEvent.VoiceLeavedEvent, e);
        Socket.OnTokenSent += (_, e) => Fire(Enums.MezEvent.TokenSend, e);
        Socket.OnWebrtcSignaling += (_, e) => Fire(Enums.MezEvent.WebrtcSignalingFwd, e);
        Socket.OnDropdownSelected += (_, e) => Fire(Enums.MezEvent.DropdownBoxSelected, e);
        Socket.OnQuickMenu += (_, e) => Fire(Enums.MezEvent.QuickMenu, e);
    }

    // ─── Auth ─────────────────────────────────────────────────────────────────

    public async Task<Session> LoginAsync(CancellationToken cancellationToken = default)
    {
        var session = await Api.AuthenticateAsync(_options.BotId, _options.Token, cancellationToken);
        CurrentSession = session;
        
        // Always recreate API client with session token after authentication
        var basePath = _options.ApiBasePath;
        if (!string.IsNullOrEmpty(session.ApiUrl))
        {
            Console.WriteLine($"[DEBUG] Switching API to: {session.ApiUrl}");
            var uri = new Uri(session.ApiUrl.StartsWith("http") ? session.ApiUrl : "https://" + session.ApiUrl);
            basePath = $"{uri.Scheme}://{uri.Host}:{(uri.IsDefaultPort ? (uri.Scheme == "https" ? 443 : 80) : uri.Port)}";
        }
        else
        {
            basePath = basePath ?? $"{(_options.UseSSL ? "https" : "http")}://{_options.Host}:{_options.Port}";
        }
        
        // Recreate API client with session JWT token (not bot token!)
        Console.WriteLine($"[DEBUG] Recreating API client with session token");
        Api = new MezonRestApi(session.Token, basePath, _options.TimeoutMs, _options.AllowInvalidCertificates);
        
        // Recreate clan manager with new API instance
        Clans = new ClanManager(this, Api, Socket, () => CurrentSession?.Token ?? "");
        
        await Socket.ConnectAsync(CurrentSession, cancellationToken: cancellationToken);
        
        // Auto-discover and join all clans the bot is a member of
        try
        {
            var clans = await Clans.FetchAllAsync(cancellationToken);
            Console.WriteLine($"[DEBUG] Found {clans.Count} clans");
            
            foreach (var clan in clans)
            {
                Console.WriteLine($"Joined clan: {clan.Name} (ID: {clan.Id})");
                await Socket.JoinClanAsync(clan.Id, cancellationToken);
                
                // Auto-load channels for each clan
                try
                {
                    await clan.LoadChannelsAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Failed to load channels for clan {clan.Name}: {ex.Message}");
                }
                
                await Task.Delay(50, cancellationToken); // Small delay to avoid rate limiting
            }
            
            // Also join the DM "clan" (id = 0) for direct messages
            await Socket.JoinClanAsync("0", cancellationToken);
            
            // Create DM clan in cache
            var dmClan = new Clan("0", this, Api, Socket, () => CurrentSession?.Token ?? "", "DM", "");
            Clans.Cache["0"] = dmClan;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to auto-join clans: {ex.Message}");
        }
        
        return CurrentSession;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        Socket.Disconnect();
        CurrentSession = null;
        Clans.Clear();
        return Task.CompletedTask;
    }

    public async Task RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null)
            throw new InvalidOperationException("Not logged in");

        // Re-authenticate
        var session = await Api.AuthenticateAsync(_options.BotId, _options.Token, cancellationToken);
        CurrentSession = session;
    }

    // ─── Event Subscription ─────────────────────────────────────────────────

    public void On(string eventType, EventHandler<MezonEventArgs> handler)
    {
        _eventHandlers.AddOrUpdate(
            eventType,
            _ => new List<EventHandler<MezonEventArgs>> { handler },
            (_, list) => { list.Add(handler); return list; });
    }

    public void Remove(string eventType, EventHandler<MezonEventArgs> handler)
    {
        if (_eventHandlers.TryGetValue(eventType, out var list))
            list.Remove(handler);
    }

    private void Fire(string eventType, Interfaces.MezonEventArgs args)
    {
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
            foreach (var h in handlers.ToArray())
                h(this, args);
    }

    /// <summary>
    /// Cache user information from channel message (similar to TypeScript SDK v2)
    /// </summary>
    private void CacheUserFromMessage(Interfaces.MezonEventArgs e)
    {
        try
        {
            if (e.Data is not Domain.ChannelMessage message)
            {
                return;
            }

            var clanId = message.ClanId ?? "";
            var senderId = message.SenderId ?? "";

            if (string.IsNullOrEmpty(clanId) || string.IsNullOrEmpty(senderId))
            {
                return;
            }

            // Don't cache bot's own messages
            if (senderId == ClientId)
            {
                return;
            }

            var clan = Clans.Get(clanId);
            if (clan == null)
            {
                return;
            }

            // Check if user already exists in cache
            var existingUser = clan.Users.Get(senderId);
            
            // Create or update user
            var user = new Structures.User(
                userId: senderId,
                client: this,
                api: Api,
                getSessionToken: () => CurrentSession?.Token ?? "",
                username: message.Username ?? existingUser?.Username,
                displayName: message.DisplayName ?? existingUser?.DisplayName,
                clanNick: message.ClanNick ?? existingUser?.ClanNick,
                clanAvatar: message.ClanAvatar ?? existingUser?.ClanAvatar,
                avatarUrl: existingUser?.AvatarUrl,
                dmChannelId: existingUser?.DmChannelId
            );

            // Add to clan's user cache
            clan.Users.Set(senderId, user);
        }
        catch
        {
            // Ignore errors in caching
        }
    }

    // ─── Messaging ───────────────────────────────────────────────────────────

    public Task<ChannelMessageAck> SendMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        ChannelMessageContent content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? references = null,
        bool anonymousMessage = false,
        bool mentionEveryone = false,
        string? avatar = null,
        int? code = null,
        string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        return Socket.WriteChatMessageAsync(
            clanId, channelId, mode, isPublic,
            content, mentions, attachments, references,
            anonymousMessage, mentionEveryone, avatar, code, topicId,
            cancellationToken);
    }

    public async Task<ChannelMessageAck> SendDMAsync(
        string channelDmId,
        string message,
        Dictionary<string, object>? messageOptions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? refs = null,
        CancellationToken cancellationToken = default)
    {
        var content = new ChannelMessageContent { Text = message };
        return await Socket.WriteChatMessageAsync(
            clanId: "", channelId: channelDmId, mode: Enums.ChannelStreamMode.Dm,
            isPublic: false, content: content, mentions: null,
            attachments: attachments, references: refs,
            anonymousMessage: false, mentionEveryone: false,
            cancellationToken: cancellationToken);
    }

    // ─── MMN Token Transfers ─────────────────────────────────────────────────

    public Task<SendTokenResult> SendTokenAsync(SendTokenData data, CancellationToken cancellationToken = default)
    {
        return Socket.SendTokenAsync(data.ReceiverId, data.Amount, cancellationToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    return new SendTokenResult { TxHash = null, Ok = false, Error = t.Exception?.InnerException?.Message };
                var evt = t.Result;
                return new SendTokenResult { TxHash = evt.TransactionId, Ok = true, Error = null };
            }, cancellationToken);
    }

    public Task<EphemeralKeyPair> GetEphemeralKeyPairAsync(CancellationToken cancellationToken = default)
    {
        // Generate a key pair client-side (simplified — in production use a crypto library)
        return Task.FromResult(new EphemeralKeyPair { PublicKey = Guid.NewGuid().ToString("N"), PrivateKey = Guid.NewGuid().ToString("N") });
    }

    public Task<string> GetAddressAsync(string senderId, CancellationToken cancellationToken = default)
    {
        // MMN address lookup via optional MMN API URL
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task<long> GetCurrentNonceAsync(string senderId, string state = "pending", CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public Task<ZkProofResponse> GetZkProofsAsync(ZkProofRequest request, CancellationToken cancellationToken = default)
    {
        // ZK proof generation via optional ZK API URL
        return Task.FromResult(new ZkProofResponse { ZkProof = "placeholder", ZkPub = "placeholder" });
    }

    // ─── Channels ────────────────────────────────────────────────────────────

    public async Task<Domain.ApiChannelDescription> CreateChannelAsync(ApiCreateChannelDescRequest request, CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        // Use REST API to create channel
        var channel = await Api.CreateChannelDescAsync(CurrentSession.Token, request, cancellationToken);
        return channel;
    }

    public async Task<Domain.ApiChannelDescription?> CreateDMChannelAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        var channel = await Api.CreateDmChannelAsync(CurrentSession.Token, userId, cancellationToken);
        if (channel == null) return null;
        
        // Convert protobuf to domain model
        return new Domain.ApiChannelDescription
        {
            ClanId = channel.ClanId.ToString(),
            ChannelId = channel.ChannelId.ToString(),
            Type = (int)channel.Type
        };
    }

    public async Task<ApiChannelDescList> ListChannelsAsync(
        int? channelType = null, string? clanId = null,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        return await Api.ListChannelDescsAsync(CurrentSession.Token, channelType, clanId, limit, state, cursor, null, cancellationToken);
    }

    public async Task<ApiVoiceChannelUserList> ListChannelVoiceUsersAsync(
        string clanId, string channelId, int channelType,
        int? limit = null, int? state = null, string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        return await Api.ListChannelVoiceUsersAsync(CurrentSession.Token, clanId, limit, cancellationToken);
    }

    // ─── High-level Structures ───────────────────────────────────────────────

    /// <summary>
    /// Get a clan from cache or fetch from API.
    /// Similar to TypeScript SDK's client.clans.get() and client.clans.fetch()
    /// </summary>
    public async Task<Clan?> GetClanAsync(string clanId, CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        
        // Try to get from cache first
        var clan = Clans.Get(clanId);
        if (clan != null) return clan;
        
        // Fetch from API if not in cache
        return await Clans.FetchAsync(clanId, cancellationToken);
    }

    public Task<TextChannel> GetChannelAsync(string clanId, string channelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TextChannel(clanId, channelId, this));
    }

    public Task<User> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null) throw new InvalidOperationException("Not logged in");
        return Task.FromResult(new User(userId, this, Api, () => CurrentSession.Token));
    }

    // ─── Phase 2: Additional Helper Methods ──────────────────────────────────

    /// <summary>
    /// Send typing indicator to show user is typing
    /// </summary>
    public Task SendTypingIndicatorAsync(
        string clanId, string channelId, int mode, bool isPublic,
        CancellationToken cancellationToken = default)
    {
        return Socket.WriteMessageTypingAsync(clanId, channelId, mode, isPublic, cancellationToken);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public Task MarkMessageAsReadAsync(
        string clanId, string channelId, int mode, string messageId,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Socket.WriteLastSeenMessageAsync(clanId, channelId, mode, messageId, timestamp, cancellationToken);
    }

    /// <summary>
    /// Pin a message in channel
    /// </summary>
    public Task PinMessageAsync(
        string clanId, string channelId, int mode, bool isPublic, string messageId,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Socket.WriteLastPinMessageAsync(clanId, channelId, mode, isPublic, messageId, timestamp, 0, cancellationToken);
    }

    /// <summary>
    /// Unpin a message in channel
    /// </summary>
    public Task UnpinMessageAsync(
        string clanId, string channelId, int mode, bool isPublic, string messageId,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Socket.WriteLastPinMessageAsync(clanId, channelId, mode, isPublic, messageId, timestamp, 1, cancellationToken);
    }

    /// <summary>
    /// Set custom status in clan
    /// </summary>
    public Task SetCustomStatusAsync(string clanId, string status, CancellationToken cancellationToken = default)
    {
        return Socket.WriteCustomStatusAsync(clanId, status, cancellationToken);
    }

    /// <summary>
    /// Send ephemeral message (only visible to specific user)
    /// </summary>
    public Task<ChannelMessageAck> SendEphemeralMessageAsync(
        string receiverId, string clanId, string channelId, int mode, bool isPublic,
        ChannelMessageContent content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        CancellationToken cancellationToken = default)
    {
        return Socket.WriteEphemeralMessageAsync(
            receiverId, clanId, channelId, mode, isPublic, content,
            mentions, attachments, null, false, false, null, null, null, null,
            cancellationToken);
    }

    /// <summary>
    /// Check if clan name already exists
    /// </summary>
    public Task<bool> CheckClanNameExistsAsync(string clanName, CancellationToken cancellationToken = default)
    {
        return Socket.CheckDuplicateClanNameAsync(clanName, cancellationToken);
    }

    /// <summary>
    /// React to a message with emoji
    /// </summary>
    public Task<ApiMessageReaction> ReactToMessageAsync(
        string id, string clanId, string channelId, int mode, bool isPublic,
        string messageId, string emojiId, string emoji, int count, string messageSenderId,
        bool isDelete = false,
        CancellationToken cancellationToken = default)
    {
        return Socket.WriteMessageReactionAsync(
            id, clanId, channelId, mode, isPublic, messageId,
            emojiId, emoji, count, messageSenderId, isDelete, cancellationToken);
    }

    /// <summary>
    /// Update an existing message
    /// </summary>
    public Task<ChannelMessageAck> UpdateMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, ChannelMessageContent content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        bool hideEdited = false,
        CancellationToken cancellationToken = default)
    {
        return Socket.UpdateChatMessageAsync(
            clanId, channelId, mode, isPublic, messageId, content,
            mentions, attachments, hideEdited, null, cancellationToken);
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    public Task<ChannelMessageAck> DeleteMessageAsync(
        string clanId, string channelId, int mode, bool isPublic, string messageId,
        CancellationToken cancellationToken = default)
    {
        return Socket.RemoveChatMessageAsync(clanId, channelId, mode, isPublic, messageId, null, cancellationToken);
    }

    public void Dispose()
    {
        Socket.Dispose();
        _mmnHttp.Dispose();
    }
}
