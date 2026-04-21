using Google.Protobuf;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;
using Mezon.Sdk.Interfaces;
using Mezon.Sdk.Realtime;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mezon.Sdk.Socket;

public sealed class MezonSocket : IMezonSocket
{
    private readonly WebSocketAdapter _ws;
    private Session? _session;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Envelope>> _pending = new();
    private Timer? _heartbeatTimer;
    private Timer? _connectTimeoutTimer;
    private bool _disposed;
    private int _cid;

    public const int DefaultHeartbeatTimeoutMs = 10_000;
    public const int DefaultSendTimeoutMs = 10_000;
    public const int DefaultConnectTimeoutMs = 30_000;

    private int _heartbeatTimeoutMs = DefaultHeartbeatTimeoutMs;
    public bool IsOpen => _ws.IsOpen;

    public event EventHandler<MezonEventArgs>? OnChannelMessage;
#pragma warning disable CS0067 
    public event EventHandler<MezonEventArgs>? OnMessageReaction;
    public event EventHandler<MezonEventArgs>? OnUserChannelAdded;
    public event EventHandler<MezonEventArgs>? OnUserChannelRemoved;
    public event EventHandler<MezonEventArgs>? OnUserClanRemoved;
    public event EventHandler<MezonEventArgs>? OnChannelCreated;
    public event EventHandler<MezonEventArgs>? OnChannelDeleted;
    public event EventHandler<MezonEventArgs>? OnChannelUpdated;
    public event EventHandler<MezonEventArgs>? OnRoleEvent;
    public event EventHandler<MezonEventArgs>? OnGiveCoffee;
    public event EventHandler<MezonEventArgs>? OnRoleAssign;
    public event EventHandler<MezonEventArgs>? OnAddClanUser;
    public event EventHandler<MezonEventArgs>? OnTokenSent;
    public event EventHandler<MezonEventArgs>? OnClanEventCreated;
    public event EventHandler<MezonEventArgs>? OnMessageButtonClicked;
    public event EventHandler<MezonEventArgs>? OnStreamingJoined;
    public event EventHandler<MezonEventArgs>? OnStreamingLeaved;
    public event EventHandler<MezonEventArgs>? OnDropdownSelected;
    public event EventHandler<MezonEventArgs>? OnWebrtcSignaling;
    public event EventHandler<MezonEventArgs>? OnVoiceStarted;
    public event EventHandler<MezonEventArgs>? OnVoiceEnded;
    public event EventHandler<MezonEventArgs>? OnVoiceJoined;
    public event EventHandler<MezonEventArgs>? OnVoiceLeaved;
    public event EventHandler<MezonEventArgs>? OnNotifications;
    public event EventHandler<MezonEventArgs>? OnQuickMenu;
#pragma warning restore CS0067
    public event EventHandler? OnDisconnected;
    public event EventHandler? OnHeartbeatTimeout;

    public MezonSocket()
    {
        _ws = new WebSocketAdapter();
        _ws.OnMessage += HandleMessage;
        _ws.OnClose += (_, e) => OnDisconnected?.Invoke(this, EventArgs.Empty);
        _ws.OnError += (_, e) => {   };
    }

    public async Task<Session> ConnectAsync(Session session, bool createStatus = true, int? connectTimeoutMs = null, CancellationToken cancellationToken = default)
    {
        _session = session;

        var wsHost = session.WsUrl ?? "sock.mezon.ai";

        if (!wsHost.StartsWith("ws://") && !wsHost.StartsWith("wss://"))
        {
            wsHost = $"wss://{wsHost}";
        }

        var token = Uri.EscapeDataString(session.Token);
        var wsUrl = $"{wsHost}/ws?lang=en&status={createStatus.ToString().ToLower()}&token={token}&format=protobuf";

        Console.WriteLine($"[DEBUG] WebSocket URL: {wsUrl.Replace(token, "***TOKEN***")}");

        var timeout = connectTimeoutMs ?? DefaultConnectTimeoutMs;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        _connectTimeoutTimer = new Timer(_ => OnHeartbeatTimeout?.Invoke(this, EventArgs.Empty), null, timeout, Timeout.Infinite);

        await _ws.ConnectAsync(wsUrl, cts.Token);

        _ = Task.Run(() => StartHeartbeatLoop(), cancellationToken);
        return session;
    }

    public void Disconnect(bool fireDisconnectEvent = true)
    {
        _heartbeatTimer?.Dispose();
        _ws.Close();
        if (!fireDisconnectEvent)
            OnDisconnected = null;
    }

    public async Task<Domain.Channel> JoinClanChatAsync(string clanId, CancellationToken cancellationToken = default)
    {
        var clanJoin = new Proto.ClanJoin { ClanId = long.Parse(clanId) };
        var env = new Proto.Envelope { ClanJoin = clanJoin };

        var bytes = env.ToByteArray();
        await _ws.SendAsync(new ArraySegment<byte>(bytes), cancellationToken);

        return new Domain.Channel { Id = clanId };
    }

    public Task<Domain.Channel> JoinClanAsync(string clanId, CancellationToken cancellationToken = default)
        => JoinClanChatAsync(clanId, cancellationToken);

    public async Task<Domain.Channel> JoinChatAsync(string clanId, string channelId, int channelType, bool isPublic, CancellationToken cancellationToken = default)
    {
        var env = new Envelope
        {
            ChannelJoinMsg = new ChannelJoin
            {
                ClanId = clanId,
                ChannelId = channelId,
                ChannelType = channelType,
                IsPublic = isPublic
            }.Encode()
        };
        var reply = await SendAsync(env, cancellationToken);
        return ToChannel(reply.Channel);
    }

    public Task LeaveChatAsync(string clanId, string channelId, int channelType, bool isPublic, CancellationToken cancellationToken = default)
    {
        var env = new Envelope
        {
            ChannelLeave = new ChannelLeave
            {
                ClanId = clanId,
                ChannelId = channelId,
                ChannelType = channelType,
                IsPublic = isPublic
            }.Encode()
        };
        return SendAndForget(env, cancellationToken);
    }

    public async Task<ChannelMessageAck> WriteChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        object content,
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
        var contentJson = content is string s ? s : JsonSerializer.Serialize(content);

        var msg = new Proto.ChannelMessageSend
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            Content = contentJson,
            Mode = mode,
            IsPublic = isPublic,
            AnonymousMessage = anonymousMessage,
            MentionEveryone = mentionEveryone,
            Avatar = avatar ?? "",
            Code = code ?? TypeMessage.Ephemeral,
            TopicId = !string.IsNullOrEmpty(topicId) && long.TryParse(topicId, out var tid) ? tid : 0
        };

        if (mentions != null)
            foreach (var m in mentions)
            {
                msg.Mentions.Add(new Proto.MessageMention
                {
                    UserId = long.TryParse(m.UserId, out var uid) ? uid : 0,
                    Username = m.Username ?? "",
                    RoleId = long.TryParse(m.RoleId, out var rid) ? rid : 0
                });
            }

        if (references != null)
            foreach (var r in references)
            {
                msg.References.Add(new Proto.MessageRef
                {
                    MessageId = long.TryParse(r.MessageId, out var msgId) ? msgId : 0,
                    MessageRefId = long.TryParse(r.MessageRefId, out var refId) ? refId : 0,
                    MessageSenderId = long.TryParse(r.MessageSenderId, out var senderId) ? senderId : 0,
                    RefType = r.RefType ?? 0,
                    MessageSenderUsername = r.MessageSenderUsername ?? "",
                    MesagesSenderAvatar = r.MesagesSenderAvatar ?? "",
                    MessageSenderClanNick = r.MessageSenderClanNick ?? "",
                    MessageSenderDisplayName = r.MessageSenderDisplayName ?? "",
                    Content = r.Content ?? "",
                    HasAttachment = r.HasAttachment ?? false
                });
            }

        var env = new Proto.Envelope { ChannelMessageSend = msg };
        await SendAsync(env, cancellationToken);

        return new ChannelMessageAck { ChannelId = channelId, MessageId = "" };
    }

    public async Task<ChannelMessageAck> WriteEphemeralMessageAsync(
        string receiverId, string clanId, string channelId, int mode, bool isPublic,
        object content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        ApiMessageRef[]? references = null,
        bool anonymousMessage = false,
        bool mentionEveryone = false,
        string? avatar = null,
        int? code = null,
        string? topicId = null,
        string? messageId = null,
        CancellationToken cancellationToken = default)
    {
        var contentJson = content is string s ? s : JsonSerializer.Serialize(content);

        var msg = new Proto.ChannelMessageSend
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            Content = contentJson,
            Mode = mode,
            IsPublic = isPublic,
            AnonymousMessage = anonymousMessage,
            MentionEveryone = mentionEveryone,
            Avatar = avatar ?? "",
            Code = code ?? 0,
            TopicId = !string.IsNullOrEmpty(topicId) && long.TryParse(topicId, out var tid) ? tid : 0
        };

        if (mentions != null)
            foreach (var m in mentions)
            {
                msg.Mentions.Add(new Proto.MessageMention
                {
                    UserId = long.TryParse(m.UserId, out var uid) ? uid : 0,
                    Username = m.Username ?? "",
                    RoleId = long.TryParse(m.RoleId, out var rid) ? rid : 0
                });
            }

        if (references != null)
            foreach (var r in references)
            {
                msg.References.Add(new Proto.MessageRef
                {
                    MessageId = long.TryParse(r.MessageId, out var msgId) ? msgId : 0,
                    MessageRefId = long.TryParse(r.MessageRefId, out var refId) ? refId : 0,
                    MessageSenderId = long.TryParse(r.MessageSenderId, out var senderId) ? senderId : 0,
                    RefType = r.RefType ?? 0,
                    MessageSenderUsername = r.MessageSenderUsername ?? "",
                    MesagesSenderAvatar = r.MesagesSenderAvatar ?? "",
                    MessageSenderClanNick = r.MessageSenderClanNick ?? "",
                    MessageSenderDisplayName = r.MessageSenderDisplayName ?? "",
                    Content = r.Content ?? "",
                    HasAttachment = r.HasAttachment ?? false
                });
            }

        if (!long.TryParse(receiverId, out var receiverIdValue))
        {
            throw new InvalidOperationException($"Invalid receiver id '{receiverId}' for ephemeral message.");
        }

        var ephemeral = new Proto.EphemeralMessageSend
        {
            Message = msg
        };
        ephemeral.ReceiverIds.Add(receiverIdValue);

        Console.WriteLine($"[DEBUG] Sending ephemeral message:");
        Console.WriteLine($"  ReceiverId: '{receiverId}' (type: {receiverId?.GetType().Name})");
        Console.WriteLine($"  ReceiverIds count: {ephemeral.ReceiverIds.Count}");
        Console.WriteLine($"  ClanId: {clanId}");
        Console.WriteLine($"  ChannelId: {channelId}");
        Console.WriteLine($"  Code: {msg.Code}");
        Console.WriteLine($"  Mode: {mode}");
        Console.WriteLine($"  IsPublic: {isPublic}");
        Console.WriteLine($"  TopicId: {msg.TopicId}");
        Console.WriteLine($"  Avatar: '{avatar ?? ""}'");
        Console.WriteLine($"  Content: {contentJson}");
        Console.WriteLine($"  References count: {references?.Length ?? 0}");
        Console.WriteLine($"  Mentions count: {mentions?.Length ?? 0}");

        var env = new Proto.Envelope { EphemeralMessageSend = ephemeral };
        await SendAsync(env, cancellationToken);

        return new ChannelMessageAck { ChannelId = channelId, MessageId = messageId ?? "" };
    }

    public async Task<ChannelMessageAck> UpdateChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, object content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        bool? hideEditted = null,
        string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        var contentJson = content is string s ? s : JsonSerializer.Serialize(content);

        var msg = new Proto.ChannelMessageUpdate
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            MessageId = long.TryParse(messageId, out var mid) ? mid : 0,
            Content = contentJson,
            Mode = mode,
            IsPublic = isPublic,
            HideEditted = hideEditted ?? false,
            TopicId = !string.IsNullOrEmpty(topicId) && long.TryParse(topicId, out var tid) ? tid : 0
        };

        var env = new Proto.Envelope { ChannelMessageUpdate = msg };
        await SendAsync(env, cancellationToken);

        return new ChannelMessageAck { ChannelId = channelId, MessageId = messageId };
    }

    public Task<ChannelMessageAck> RemoveChatMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        var env = new Envelope
        {
            ChannelMessageRemove = new ChannelMessageRemove
            {
                ClanId = clanId,
                ChannelId = channelId,
                MessageId = messageId
            }.Encode()
        };
        return SendAndWaitForAck(env, cancellationToken);
    }

    public async Task<ApiMessageReaction> WriteMessageReactionAsync(
        string id, string clanId, string channelId, int mode, bool isPublic,
        string messageId, string emojiId, string emoji, int count,
        string messageSenderId, bool actionDelete,
        CancellationToken cancellationToken = default)
    {
        var env = new Envelope
        {
            MessageReaction = Array.Empty<byte>()
        };
        await SendAndForget(env, cancellationToken);
        return new ApiMessageReaction
        {
            SenderId = _session?.UserId ?? "",
            EmojiId = emojiId,
            Emoji = emoji
        };
    }

    public Task WriteMessageTypingAsync(
        string clanId, string channelId, int mode, bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var msg = new Proto.MessageTypingEvent
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            Mode = mode,
            IsPublic = isPublic
        };

        var env = new Proto.Envelope { MessageTypingEvent = msg };
        return SendAsync(env, cancellationToken);
    }

    public Task WriteLastSeenMessageAsync(
        string clanId, string channelId, int mode,
        string messageId, long timestampSeconds,
        CancellationToken cancellationToken = default)
    {
        var msg = new Proto.LastSeenMessageEvent
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            MessageId = long.TryParse(messageId, out var mid) ? mid : 0,
            Mode = mode,
            TimestampSeconds = (uint)timestampSeconds
        };

        var env = new Proto.Envelope { LastSeenMessageEvent = msg };
        return SendAsync(env, cancellationToken);
    }

    public Task WriteLastPinMessageAsync(
        string clanId, string channelId, int mode, bool isPublic,
        string messageId, long timestampSeconds, int operation,
        CancellationToken cancellationToken = default)
    {
        var msg = new Proto.LastPinMessageEvent
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            ChannelId = long.TryParse(channelId, out var chid) ? chid : 0,
            MessageId = long.TryParse(messageId, out var mid) ? mid : 0,
            Mode = mode,
            IsPublic = isPublic,
            TimestampSeconds = (uint)timestampSeconds,
            Operation = operation 
        };

        var env = new Proto.Envelope { LastPinMessageEvent = msg };
        return SendAsync(env, cancellationToken);
    }

    public Task WriteCustomStatusAsync(string clanId, string status, CancellationToken cancellationToken = default)
    {
        var msg = new Proto.CustomStatusEvent
        {
            ClanId = long.TryParse(clanId, out var cid) ? cid : 0,
            Status = status ?? ""
        };

        var env = new Proto.Envelope { CustomStatusEvent = msg };
        return SendAsync(env, cancellationToken);
    }

    public Task UpdateStatusAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var env = new Envelope { StatusUpdate = Array.Empty<byte>() };
        return SendAndForget(env, cancellationToken);
    }

    public async Task<Domain.TokenSentEvent> SendTokenAsync(string receiverId, int amount, CancellationToken cancellationToken = default)
    {
        var env = new Envelope
        {
            TokenSentEvent = new Realtime.TokenSentEvent
            {
                SenderId = _session?.UserId ?? "",
                ReceiverId = receiverId,
                Amount = amount
            }.Encode()
        };
        var reply = await SendAsync(env, cancellationToken);
        var evt = reply.TokenSentEvent != null ? Realtime.TokenSentEvent.Decode(reply.TokenSentEvent) : null;
        return new Domain.TokenSentEvent
        {
            SenderId = evt?.SenderId ?? _session?.UserId ?? "",
            ReceiverId = evt?.ReceiverId ?? receiverId,
            Amount = evt?.Amount ?? amount,
            TransactionId = evt?.TransactionId
        };
    }

    public Task<ClanEmoji[]> ListClanEmojiAsync(string clanId, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<ClanEmoji>());

    public Task<ClanSticker[]> ListClanStickersAsync(string clanId, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<ClanSticker>());

    public Task<Domain.ChannelDescriptionEvent[]> ListChannelsByUserIdAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<Domain.ChannelDescriptionEvent>());

    public Task<HashtagDm[]> HashtagDMListAsync(string[] userIds, int limit, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<HashtagDm>());

    public Task<bool> CheckDuplicateClanNameAsync(string clanName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<NotificationChannelSettingEvent?> GetNotificationChannelSettingAsync(string channelId, CancellationToken cancellationToken = default)
        => Task.FromResult<NotificationChannelSettingEvent?>(null);

    public Task<NotificationCategorySettingEvent?> GetNotificationCategorySettingAsync(string categoryId, CancellationToken cancellationToken = default)
        => Task.FromResult<NotificationCategorySettingEvent?>(null);

    public Task<NotificationClanSettingEvent?> GetNotificationClanSettingAsync(string clanId, CancellationToken cancellationToken = default)
        => Task.FromResult<NotificationClanSettingEvent?>(null);


    private string NextCid() => Interlocked.Increment(ref _cid).ToString();

    private async Task<Envelope> SendAsync(Envelope env, CancellationToken cancellationToken)
    {
        var cid = string.IsNullOrEmpty(env.Cid) ? NextCid() : env.Cid;
        env.Cid = cid;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultSendTimeoutMs);

        var tcs = new TaskCompletionSource<Envelope>();
        _pending.TryAdd(cid, tcs);

        var bytes = RealtimeSerializer.Encode(env);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), cts.Token);

        return await tcs.Task.WaitAsync(cts.Token);
    }

    private async Task SendAsync(Proto.Envelope env, CancellationToken cancellationToken)
    {
        var bytes = env.ToByteArray();
        await _ws.SendAsync(new ArraySegment<byte>(bytes), cancellationToken);
    }

    private Task SendAndForget(Envelope env, CancellationToken cancellationToken)
    {
        _ = SendAsync(env, cancellationToken);
        return Task.CompletedTask;
    }

    private async Task<ChannelMessageAck> SendAndWaitForAck(Envelope env, CancellationToken cancellationToken)
    {
        var reply = await SendAsync(env, cancellationToken);
        return ToChannelMessageAck(reply.ChannelMessageAck);
    }

    private void HandleMessage(object? sender, ArraySegment<byte> data)
    {
        try
        {
            var env = Proto.Envelope.Parser.ParseFrom(data.Array!, data.Offset, data.Count);
            DispatchProto(env);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to decode envelope: {ex.Message}");
        }
    }

    private void DispatchProto(Proto.Envelope env)
    {
        if (env.Error != null)
        {
            Console.WriteLine($"[ERROR] Server error: Code={env.Error.Code}, Message={env.Error.Message}");
        }

        if (env.ChannelMessage != null)
        {
            try
            {
                var protoMsg = env.ChannelMessage;

                var domainMsg = new Domain.ChannelMessage
                {
                    Id = protoMsg.MessageId.ToString(),
                    MessageId = protoMsg.MessageId.ToString(),
                    ChannelId = protoMsg.ChannelId.ToString(),
                    ChannelLabel = protoMsg.ChannelLabel ?? "",
                    ClanId = protoMsg.ClanId.ToString(),
                    SenderId = protoMsg.SenderId.ToString(),
                    Username = protoMsg.Username,
                    DisplayName = protoMsg.DisplayName,
                    ClanNick = protoMsg.ClanNick,
                    Content = ParseChannelMessageContent(protoMsg.Content),
                    Mentions = ParseMentions(protoMsg.Mentions),
                    CreateTimeSeconds = protoMsg.CreateTimeSeconds,
                    Code = protoMsg.Code,
                    Mode = protoMsg.Mode,
                    IsPublic = protoMsg.IsPublic,
                };

                if (domainMsg.Mentions?.Any() == true)
                {
                    Console.WriteLine(
                        $"[DEBUG][SOCKET] Message {domainMsg.MessageId} in channel {domainMsg.ChannelId} has mentions: {string.Join(", ", domainMsg.Mentions.Select(x => x.UserId ?? x.RoleId ?? "unknown"))}");
                }

                OnChannelMessage?.Invoke(this, new Interfaces.MezonEventArgs { Data = domainMsg });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to decode ChannelMessage: {ex.Message}");
            }
        }
        else if (env.MessageButtonClicked != null)
        {
            OnMessageButtonClicked?.Invoke(this, new Interfaces.MezonEventArgs { Data = env });
        }
    }

    private void StartHeartbeatLoop()
    {
        _heartbeatTimer = new Timer(async _ =>
        {
            try
            {
                await SendAndForget(new Envelope { Ping = Array.Empty<byte>() }, CancellationToken.None);
            }
            catch
            {
                OnHeartbeatTimeout?.Invoke(this, EventArgs.Empty);
            }
        }, null, _heartbeatTimeoutMs, _heartbeatTimeoutMs);
    }

    private static Domain.Channel ToChannel(byte[]? data) => new()
    {
        Id = data != null ? Realtime.Channel.Decode(data).Id : ""
    };

    private static Domain.ChannelMessageContent? ParseChannelMessageContent(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Domain.ChannelMessageContent>(rawContent)
                ?? new Domain.ChannelMessageContent { Text = rawContent };
        }
        catch
        {
            return new Domain.ChannelMessageContent { Text = rawContent };
        }
    }

    private static IReadOnlyList<ApiMessageMention>? ParseMentions(ByteString rawMentions)
    {
        if (rawMentions == null || rawMentions.IsEmpty)
        {
            return null;
        }

        try
        {
            var mentionList = Proto.MessageMentionList.Parser.ParseFrom(rawMentions);
            return mentionList.Mentions
                .Select(m => new ApiMessageMention
                {
                    Id = m.Id == 0 ? null : m.Id.ToString(),
                    UserId = m.UserId == 0 ? null : m.UserId.ToString(),
                    Username = string.IsNullOrWhiteSpace(m.Username) ? null : m.Username,
                    RoleId = m.RoleId == 0 ? null : m.RoleId.ToString(),
                    RoleName = string.IsNullOrWhiteSpace(m.Rolename) ? null : m.Rolename,
                    CreateTime = m.CreateTimeSeconds == 0 ? null : m.CreateTimeSeconds.ToString(),
                    S = m.S,
                    E = m.E
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to decode mentions: {ex.Message}");
            return null;
        }
    }

    private static ChannelMessageAck ToChannelMessageAck(byte[]? data) => new()
    {
        ChannelId = data != null ? RealtimeChannelMessageAck.Decode(data).ChannelId : "",
        MessageId = data != null ? RealtimeChannelMessageAck.Decode(data).MessageId : ""
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _heartbeatTimer?.Dispose();
        _connectTimeoutTimer?.Dispose();
        _ws.Dispose();
    }
}
