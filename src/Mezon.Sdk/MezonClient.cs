namespace Mezon.Sdk;

using Mezon.Sdk.Api;
using Mezon.Sdk.Client;
using Mezon.Sdk.Managers;
using Mezon.Sdk.Proto;
using Mezon.Sdk.Utils;
using MezonSession = Mezon.Sdk.Session.Session;

public class MezonClient : IAsyncDisposable
{
    // ---- Typed events ----
    public event Action<ChannelMessage>?       ChannelMessage;
    public event Action<MessageReaction>?      MessageReaction;
    public event Action<UserChannelAdded>?     UserChannelAdded;
    public event Action<UserChannelRemoved>?   UserChannelRemoved;
    public event Action<UserClanRemoved>?      UserClanRemoved;
    public event Action<AddClanUserEvent>?     AddClanUser;
    public event Action<TokenSentEvent>?       TokenSend;
    public event Action<GiveCoffeeEvent>?      GiveCoffee;
    public event Action<RoleEvent>?            RoleEvent;
    public event Action<RoleAssignedEvent>?    RoleAssign;
    public event Action<Notifications>?        Notification;
    public event Action<CreateEventRequest>?   ClanEventCreated;
    public event Action<MessageButtonClicked>? MessageButtonClicked;
    public event Action<StreamingJoinedEvent>? StreamingJoined;
    public event Action<StreamingLeavedEvent>? StreamingLeaved;
    public event Action<DropdownBoxSelected>?  DropdownBoxSelected;
    public event Action<WebrtcSignalingFwd>?   WebrtcSignalingFwd;
    public event Action<VoiceStartedEvent>?    VoiceStarted;
    public event Action<VoiceEndedEvent>?      VoiceEnded;
    public event Action<VoiceJoinedEvent>?     VoiceJoined;
    public event Action<VoiceLeavedEvent>?     VoiceLeaved;
    public event Action<AIAgentEnabledEvent>?  AIAgentEnabled;
    public event Action<ChannelCreatedEvent>?  ChannelCreated;
    public event Action<ChannelUpdatedEvent>?  ChannelUpdated;
    public event Action<ChannelDeletedEvent>?  ChannelDeleted;
    public event Action?                       Ready;

    // ---- Joined clans (populated during login) ----
    public CacheManager<string, ClanDesc> Clans { get; } = new();

    // ---- Config ----
    public string ClientId { get; }
    public string Token { get; }

    private readonly MezonClientConfig _config;
    private MezonRestApi _api = null!;
    private SessionManager _sessionManager = null!;
    private SocketManager _socketManager = null!;
    private ChannelManager _channelManager = null!;
    private readonly MessageQueue _messageQueue = new();
    private MezonSession? _session;

    public MezonClient(MezonClientConfig config)
    {
        _config = config;
        ClientId = config.BotId;
        Token = config.Token;

        var scheme = config.UseSsl ? "https://" : "http://";
        var basePath = $"{scheme}{config.Host}:{config.Port}";
        InitManagers(basePath);
    }

    private void InitManagers(string basePath)
    {
        _api = new MezonRestApi(_config.Token, basePath, _config.TimeoutMs);
        _sessionManager = new SessionManager(_api);
        _channelManager = new ChannelManager(_api);
        _socketManager = new SocketManager(_api, _config.Host, _config.Port, _config.UseSsl);
        _socketManager.OnEnvelope = DispatchEnvelope;
        _socketManager.OnDisconnected = () => Console.WriteLine("[Mezon] Disconnected.");
    }

    public async Task LoginAsync(CancellationToken ct = default)
    {
        _session = await _sessionManager.AuthenticateAsync(ClientId, Token, ct);

        if (!string.IsNullOrEmpty(_session.ApiUrl))
        {
            var uri = new Uri(_session.ApiUrl.StartsWith("http") ? _session.ApiUrl : "https://" + _session.ApiUrl);
            var newBase = $"{uri.Scheme}://{uri.Host}:{(uri.IsDefaultPort ? (uri.Scheme == "https" ? 443 : 80) : uri.Port)}";
            InitManagers(newBase);
            _socketManager.OnEnvelope = DispatchEnvelope;
            _socketManager.OnDisconnected = () => Console.WriteLine("[Mezon] Disconnected.");
        }

        await _socketManager.ConnectAsync(_session, ct);

        var clans = await _api.ListClansAsync(_session.Token, ct);
        foreach (var clan in clans)
        {
            var id = clan.ClanId.ToString();
            Clans.Set(id, clan);
            await _socketManager.JoinClanAsync(id, ct);
            await Task.Delay(50, ct);
        }
        // Also join the DM "clan" (id = 0)
        await _socketManager.JoinClanAsync("0", ct);

        Ready?.Invoke();
    }

    public async Task CloseAsync(CancellationToken ct = default)
    {
        await _socketManager.CloseAsync();
    }

    private void DispatchEnvelope(Envelope e)
    {
        if (e.ChannelMessage != null)          { ChannelMessage?.Invoke(e.ChannelMessage); return; }
        if (e.MessageReactionEvent != null)    { MessageReaction?.Invoke(e.MessageReactionEvent); return; }
        if (e.UserChannelAddedEvent != null)   { UserChannelAdded?.Invoke(e.UserChannelAddedEvent); return; }
        if (e.UserChannelRemovedEvent != null) { UserChannelRemoved?.Invoke(e.UserChannelRemovedEvent); return; }
        if (e.UserClanRemovedEvent != null)    { UserClanRemoved?.Invoke(e.UserClanRemovedEvent); return; }
        if (e.AddClanUserEvent != null)        { AddClanUser?.Invoke(e.AddClanUserEvent); return; }
        if (e.TokenSentEvent != null)          { TokenSend?.Invoke(e.TokenSentEvent); return; }
        if (e.GiveCoffeeEvent != null)         { GiveCoffee?.Invoke(e.GiveCoffeeEvent); return; }
        if (e.RoleEvent != null)               { RoleEvent?.Invoke(e.RoleEvent); return; }
        if (e.RoleAssignEvent != null)         { RoleAssign?.Invoke(e.RoleAssignEvent); return; }
        if (e.Notifications != null)           { Notification?.Invoke(e.Notifications); return; }
        if (e.ClanEventCreated != null)        { ClanEventCreated?.Invoke(e.ClanEventCreated); return; }
        if (e.MessageButtonClicked != null)    { MessageButtonClicked?.Invoke(e.MessageButtonClicked); return; }
        if (e.StreamingJoinedEvent != null)    { StreamingJoined?.Invoke(e.StreamingJoinedEvent); return; }
        if (e.StreamingLeavedEvent != null)    { StreamingLeaved?.Invoke(e.StreamingLeavedEvent); return; }
        if (e.DropdownBoxSelected != null)     { DropdownBoxSelected?.Invoke(e.DropdownBoxSelected); return; }
        if (e.WebrtcSignalingFwd != null)      { WebrtcSignalingFwd?.Invoke(e.WebrtcSignalingFwd); return; }
        if (e.VoiceStartedEvent != null)       { VoiceStarted?.Invoke(e.VoiceStartedEvent); return; }
        if (e.VoiceEndedEvent != null)         { VoiceEnded?.Invoke(e.VoiceEndedEvent); return; }
        if (e.VoiceJoinedEvent != null)        { VoiceJoined?.Invoke(e.VoiceJoinedEvent); return; }
        if (e.VoiceLeavedEvent != null)        { VoiceLeaved?.Invoke(e.VoiceLeavedEvent); return; }
        if (e.AiagentEnabledEvent != null)     { AIAgentEnabled?.Invoke(e.AiagentEnabledEvent); return; }
        if (e.ChannelCreatedEvent != null)     { ChannelCreated?.Invoke(e.ChannelCreatedEvent); return; }
        if (e.ChannelUpdatedEvent != null)     { ChannelUpdated?.Invoke(e.ChannelUpdatedEvent); return; }
        if (e.ChannelDeletedEvent != null)     { ChannelDeleted?.Invoke(e.ChannelDeletedEvent); return; }
    }

    // ---- Convenience send methods ----

    public Task SendAsync(string clanId, string channelId, int mode, bool isPublic,
        string contentJson, int code = 0, string? topicId = null, CancellationToken ct = default)
        => _messageQueue.EnqueueAsync((Func<Task>)(() =>
            _socketManager.SendChatMessageAsync(clanId, channelId, mode, isPublic,
                contentJson, code: code, topicId: topicId, ct: ct)!));

    public Task SendTextAsync(string clanId, string channelId, int mode, bool isPublic,
        string text, int code = 0, string? topicId = null, CancellationToken ct = default)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"")
                          .Replace("\n", "\\n").Replace("\r", "\\r");
        return SendAsync(clanId, channelId, mode, isPublic,
            $"{{\"t\":\"{escaped}\"}}", code, topicId, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _socketManager.DisposeAsync();
        await _messageQueue.DisposeAsync();
    }
}
