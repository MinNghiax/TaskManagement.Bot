namespace Mezon.Sdk.Managers;

using Mezon.Sdk.Api;
using Mezon.Sdk.Proto;
using Mezon.Sdk.Socket;
using MezonSession = Mezon.Sdk.Session.Session;

public class SocketManager : IAsyncDisposable
{
    private IWebSocketAdapter? _adapter;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _isHardDisconnect;
    private bool _isRetrying;
    private MezonSession? _currentSession;

    private readonly MezonRestApi _api;
    private readonly string _host;
    private readonly string _port;
    private readonly bool _useSsl;

    public Action<Envelope>? OnEnvelope { get; set; }
    public Action? OnDisconnected { get; set; }
    public bool IsOpen => _adapter?.IsOpen ?? false;

    public SocketManager(MezonRestApi api, string host, string port, bool useSsl)
    {
        _api = api;
        _host = host;
        _port = port;
        _useSsl = useSsl;
    }

    public async Task ConnectAsync(MezonSession session, CancellationToken ct = default)
    {
        _currentSession = session;
        _isHardDisconnect = false;

        _adapter = new WebSocketAdapterPb();
        var wsUrl = BuildWsUrl(session);
        await _adapter.ConnectAsync(wsUrl, ct);

        _receiveCts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
    }

    private string BuildWsUrl(MezonSession session)
    {
        var scheme = _useSsl ? "wss://" : "ws://";
        var baseUrl = string.IsNullOrEmpty(session.WsUrl)
            ? $"{_host}:{_port}"
            : session.WsUrl;
        var token = Uri.EscapeDataString(session.Token);
        return $"{scheme}{baseUrl}/ws?lang=en&status=true&token={token}&format=protobuf";
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var envelope in _adapter!.ReceiveAsync(ct))
            {
                try { OnEnvelope?.Invoke(envelope); }
                catch (Exception ex) { Console.Error.WriteLine($"[Mezon] OnEnvelope error: {ex.Message}"); }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Console.Error.WriteLine($"[Mezon] Receive loop error: {ex.Message}"); }
        finally
        {
            if (!_isHardDisconnect)
            {
                OnDisconnected?.Invoke();
                _ = ReconnectAsync();
            }
        }
    }

    private static long ParseId(string id) => long.TryParse(id, out var v) ? v : 0L;

    public async Task JoinClanAsync(string clanId, CancellationToken ct = default)
    {
        var envelope = new Envelope { ClanJoin = new ClanJoin { ClanId = ParseId(clanId) } };
        await SendAsync(envelope, ct);
    }

    public async Task JoinChannelAsync(string clanId, string channelId, int channelType, bool isPublic, CancellationToken ct = default)
    {
        var envelope = new Envelope
        {
            ChannelJoin = new ChannelJoin
            {
                ClanId = ParseId(clanId),
                ChannelId = ParseId(channelId),
                ChannelType = channelType,
                IsPublic = isPublic,
            }
        };
        await SendAsync(envelope, ct);
    }

    public async Task<ChannelMessageAck?> SendChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string contentJson, string? mentions = null, string? attachments = null,
        string? references = null, bool anonymous = false, bool mentionEveryone = false,
        string? avatar = null, int code = 0, string? topicId = null,
        CancellationToken ct = default)
    {
        var send = new ChannelMessageSend
        {
            ClanId = ParseId(clanId),
            ChannelId = ParseId(channelId),
            Mode = mode,
            IsPublic = isPublic,
            Content = contentJson,
            AnonymousMessage = anonymous,
            MentionEveryone = mentionEveryone,
        };
        if (!string.IsNullOrEmpty(avatar)) send.Avatar = avatar;
        if (code != 0) send.Code = code;
        if (!string.IsNullOrEmpty(topicId)) send.TopicId = ParseId(topicId);

        var envelope = new Envelope { ChannelMessageSend = send };
        await SendAsync(envelope, ct);
        return null;
    }

    public async Task SendEphemeralMessageAsync(
        string receiverId, string clanId, string channelId, int mode, bool isPublic,
        string contentJson, CancellationToken ct = default)
    {
        var inner = new ChannelMessageSend
        {
            ClanId = ParseId(clanId),
            ChannelId = ParseId(channelId),
            Mode = mode,
            IsPublic = isPublic,
            Content = contentJson,
        };
        var send = new EphemeralMessageSend { Message = inner };
        send.ReceiverIds.Add(ParseId(receiverId));
        var envelope = new Envelope { EphemeralMessageSend = send };
        await SendAsync(envelope, ct);
    }

    public async Task UpdateChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, string contentJson, CancellationToken ct = default)
    {
        var update = new ChannelMessageUpdate
        {
            ClanId = ParseId(clanId),
            ChannelId = ParseId(channelId),
            Mode = mode,
            IsPublic = isPublic,
            MessageId = ParseId(messageId),
            Content = contentJson,
        };
        var envelope = new Envelope { ChannelMessageUpdate = update };
        await SendAsync(envelope, ct);
    }

    public async Task RemoveChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, string? topicId = null, CancellationToken ct = default)
    {
        var remove = new ChannelMessageRemove
        {
            ClanId = ParseId(clanId),
            ChannelId = ParseId(channelId),
            Mode = mode,
            IsPublic = isPublic,
            MessageId = ParseId(messageId),
        };
        if (!string.IsNullOrEmpty(topicId)) remove.TopicId = ParseId(topicId);
        var envelope = new Envelope { ChannelMessageRemove = remove };
        await SendAsync(envelope, ct);
    }

    public async Task SendAsync(Envelope envelope, CancellationToken ct = default)
    {
        if (_adapter == null || !_adapter.IsOpen) throw new InvalidOperationException("Socket not connected");
        await _adapter.SendAsync(envelope, ct);
    }

    private async Task ReconnectAsync()
    {
        if (_isRetrying || _isHardDisconnect || _currentSession == null) return;
        _isRetrying = true;
        var delay = 5000;
        const int maxDelay = 60000;
        while (!_isHardDisconnect)
        {
            await Task.Delay(delay);
            try
            {
                await ConnectAsync(_currentSession);
                Console.WriteLine("[Mezon] Reconnected successfully.");
                _isRetrying = false;
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Mezon] Reconnect failed: {ex.Message}. Retrying in {delay / 1000}s...");
                delay = Math.Min(delay * 2, maxDelay);
            }
        }
        _isRetrying = false;
    }

    public async Task CloseAsync()
    {
        _isHardDisconnect = true;
        _receiveCts?.Cancel();
        if (_adapter != null) await _adapter.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        if (_adapter != null) await _adapter.DisposeAsync();
    }
}
