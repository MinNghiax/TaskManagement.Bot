using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Commands;
using MezonProto = Mezon.Sdk.Proto;

namespace TaskManagement.Bot.Application.Services;

public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        CancellationToken cancellationToken = default,
        string? replyToMessageId = null,
        ChannelMessage? originalMessage = null);
}

public class BotService : IBotService
{
    private static readonly MethodInfo? ProtoSocketSendAsyncMethod = typeof(Mezon.Sdk.Socket.MezonSocket)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .FirstOrDefault(m =>
            m.Name == "SendAsync"
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(MezonProto.Envelope)
            && m.GetParameters()[1].ParameterType == typeof(CancellationToken));

    private readonly ILogger<BotService> _logger;
    private readonly MezonClient _client;
    private readonly IEnumerable<ICommandHandler> _commandHandlers;
    private readonly IEnumerable<IComponentHandler> _componentHandlers;
    private readonly IMezonUserService _userService;
    private readonly ConcurrentDictionary<string, string> _channelClanMap = new();
    private HashSet<string> _dmChannelIds = new();
    private string? _botUserId;

    public BotService(
        ILogger<BotService> logger,
        MezonClient client,
        IEnumerable<ICommandHandler> commandHandlers,
        IEnumerable<IComponentHandler> componentHandlers,
        IMezonUserService userService)
    {
        _logger = logger;
        _client = client;
        _commandHandlers = commandHandlers;
        _componentHandlers = componentHandlers;
        _userService = userService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Mezon Bot...");

        _client.On("channel_message", OnChannelMessage);
        _client.On("message_button_clicked", OnComponent);

        var session = await _client.LoginAsync(cancellationToken);
        _botUserId = session.UserId;

        try
        {
            var dmChannels = await _client.ListChannelsAsync(channelType: 1, cancellationToken: cancellationToken);
            _dmChannelIds = dmChannels.ChannelDescs != null
                ? new HashSet<string>(dmChannels.ChannelDescs
                    .Select(c => c.ChannelId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))!)
                : new HashSet<string>();

            _logger.LogInformation("[DM CACHE] Cached {Count} DM channel ids", _dmChannelIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DM CACHE] Failed to cache DM channel ids");
        }

        // 🚀 PRELOAD ALL CLAN MEMBERS using new MezonUserService
        _logger.LogInformation("[USER_PRELOAD] Starting user preload from all clans...");
        try
        {
            var totalUsers = await _userService.PreloadAllClanMembersAsync(cancellationToken);
            _logger.LogInformation(
                "[USER_PRELOAD] ✅ Successfully preloaded {TotalUsers} users. Cache size: {CacheSize}",
                totalUsers,
                _userService.GetCacheSize());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[USER_PRELOAD] ❌ Failed to preload users. Will rely on message-based caching.");
        }

        _logger.LogInformation("Bot connected and ready");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping bot...");
        await _client.LogoutAsync(cancellationToken);
    }

    public async Task SendMessageAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        CancellationToken cancellationToken = default,
        string? replyToMessageId = null,
        ChannelMessage? originalMessage = null)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;

        var content = new ChannelMessageContent { Text = text };

        // Tạo references để reply
        var references = BuildMessageReferences(replyToMessageId, originalMessage);

        _logger.LogInformation(
            "[SEND] ClanId={ClanId} ChannelId={ChannelId} Mode={Mode} IsPublic={IsPublic} ReplyTo={ReplyToMessageId}",
            clanId,
            channelId,
            mode,
            finalIsPublic,
            replyToMessageId);

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: finalIsPublic,
            content: content,
            references: references,
            cancellationToken: cancellationToken);
    }

    private async Task SendMessageWithEmbedAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        IInteractiveMessageProps embed,
        string? replyToMessageId = null,
        ChannelMessage? originalMessage = null)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;
        var content = new ChannelMessageContent
        {
            Text = text,
            Embed = new object[] { embed }
        };

        var references = BuildMessageReferences(replyToMessageId, originalMessage);

        try
        {
            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
                references: references,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid channel identifier", StringComparison.OrdinalIgnoreCase) && mode == 2)
        {
            _logger.LogWarning(ex, "[SEND_EMBED] Retrying with mode=3 for channel {ChannelId}", channelId);

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: 3,
                isPublic: false,
                content: content,
                cancellationToken: CancellationToken.None);
        }
    }

    private async Task SendFormMessageAsync(
        string clanId,
        string channelId,
        ChannelMessageContent content,
        int mode,
        bool isPublic,
        string? replyToMessageId = null,
        ChannelMessage? originalMessage = null)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;

        var references = BuildMessageReferences(replyToMessageId, originalMessage);

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: finalIsPublic,
            content: content,
            references: references,
            cancellationToken: CancellationToken.None);
    }

    // Helper tạo references để reply
    private static ApiMessageRef[]? BuildMessageReferences(string? replyToMessageId, ChannelMessage? originalMessage)
    {
        if (string.IsNullOrEmpty(replyToMessageId) || originalMessage == null)
        {
            return null;
        }

        return new[]
        {
            new ApiMessageRef
            {
                MessageId = replyToMessageId,
                MessageRefId = replyToMessageId,
                MessageSenderId = originalMessage.SenderId ?? "",
                MessageSenderUsername = originalMessage.Username ?? "",
                MessageSenderDisplayName = originalMessage.DisplayName ?? "",
                MessageSenderClanNick = originalMessage.ClanNick ?? "",
                MesagesSenderAvatar = originalMessage.ClanAvatar ?? "",
                Content = originalMessage.Content?.Text ?? "",
                HasAttachment = originalMessage.Attachments?.Any() ?? false,
                RefType = 0
            }
        };
    }

    private async void OnChannelMessage(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not ChannelMessage message || message.SenderId == _botUserId)
            {
                return;
            }

            var clanId = message.ClanId ?? "";
            var channelId = message.ChannelId;

            if (!string.IsNullOrWhiteSpace(message.ChannelId) && !string.IsNullOrWhiteSpace(message.ClanId))
            {
                _channelClanMap[message.ChannelId] = message.ClanId;
            }

            // 📨 Cache user from message (real-time updates)
            if (!string.IsNullOrWhiteSpace(message.SenderId))
            {
                _userService.CacheUserFromMessage(
                    message.SenderId,
                    message.ClanNick,
                    message.DisplayName,
                    message.Username,
                    message.ClanAvatar,
                    message.ClanId);
            }

            // Cache all mentioned users
            if (message.Mentions != null)
            {
                foreach (var mention in message.Mentions)
                {
                    if (!string.IsNullOrWhiteSpace(mention.UserId))
                    {
                        _userService.CacheUserFromMessage(
                            mention.UserId,
                            null, // ApiMessageMention doesn't have ClanNick
                            null, // ApiMessageMention doesn't have DisplayName
                            mention.Username,
                            null,
                            message.ClanId);
                    }
                }
            }

            var content = ParseContent(message.Content?.Text);
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            foreach (var handler in _commandHandlers)
            {
                if (!handler.CanHandle(content))
                {
                    continue;
                }

                var response = await handler.HandleAsync(message, CancellationToken.None);
                if (response == null || (string.IsNullOrEmpty(response.Text) && response.Embed == null && response.Content == null))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(message.ClanId) || string.IsNullOrWhiteSpace(message.ChannelId))
                {
                    _logger.LogWarning("[SEND] Missing clan/channel id on message {MessageId}", message.MessageId);
                    break;
                }

                var finalMode = message.Mode ?? 2;
                var finalIsPublic = message.IsPublic ?? true;

                if (response.Content != null)
                {
                    await SendFormMessageAsync(message.ClanId, message.ChannelId, response.Content, finalMode, finalIsPublic, message.Id, message);
                }
                else if (response.Embed != null)
                {
                    await SendMessageWithEmbedAsync(message.ClanId, message.ChannelId, response.Text ?? string.Empty, finalMode, finalIsPublic, response.Embed, message.Id, message);
                }
                else
                {
                    await SendMessageAsync(message.ClanId, message.ChannelId, response.Text ?? string.Empty, finalMode, finalIsPublic, CancellationToken.None, message.Id, message);
                }

                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handle message error");
        }
    }

    private async void OnComponent(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data == null)
            {
                return;
            }

            var payload = JsonSerializer.SerializeToElement(e.Data);
            var customId = ComponentPayloadHelper.GetCustomId(payload);
            var channelId = ComponentPayloadHelper.GetChannelId(payload);
            var clanId = ComponentPayloadHelper.GetClanId(payload);

            if (string.IsNullOrWhiteSpace(clanId))
            {
                clanId = TryGetClanIdFromCustomId(customId);
            }

            if (string.IsNullOrWhiteSpace(clanId) && !string.IsNullOrWhiteSpace(channelId))
            {
                _channelClanMap.TryGetValue(channelId, out clanId);
            }

            if (string.IsNullOrWhiteSpace(customId) || string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(clanId))
            {
                _logger.LogWarning("[COMPONENT] Missing routing data. CustomId={CustomId} ClanId={ClanId} ChannelId={ChannelId}", customId, clanId, channelId);
                return;
            }

            var context = new ComponentContext
            {
                Payload = payload,
                CustomId = customId,
                ClanId = clanId,
                ChannelId = channelId,
                CurrentUserId = ComponentPayloadHelper.GetUserId(payload),
                MessageId = ComponentPayloadHelper.GetMessageId(payload),
                Mode = _dmChannelIds.Contains(channelId) ? 4 : 2,
                IsPublic = !_dmChannelIds.Contains(channelId)
            };

            foreach (var handler in _componentHandlers)
            {
                if (!handler.CanHandle(customId))
                {
                    continue;
                }

                var response = await handler.HandleAsync(context, CancellationToken.None);
                await SendComponentResponseAsync(response);
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handle component error");
        }
    }

    private async Task SendComponentResponseAsync(ComponentResponse response)
    {
        foreach (var deleteMessage in response.DeleteMessages)
        {
            try
            {
                await DeleteMessageViaProtoAsync(deleteMessage, CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(deleteMessage.ReplyToMessageId))
                {
                    await SendMessageAsync(
                        deleteMessage.ClanId,
                        deleteMessage.ChannelId,
                        "",
                        deleteMessage.Mode,
                        deleteMessage.IsPublic,
                        CancellationToken.None,
                        deleteMessage.ReplyToMessageId,
                        deleteMessage.OriginalMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[COMPONENT] Failed to delete message {MessageId} in channel {ChannelId}",
                    deleteMessage.MessageId,
                    deleteMessage.ChannelId);
            }
        }

        foreach (var message in response.Messages)
        {
            if (message.Content != null)
            {
                await SendFormMessageAsync(message.ClanId, message.ChannelId, message.Content, message.Mode, message.IsPublic, message.ReplyToMessageId, message.OriginalMessage);
            }
            else if (!string.IsNullOrWhiteSpace(message.Text))
            {
                await SendMessageAsync(message.ClanId, message.ChannelId, message.Text, message.Mode, message.IsPublic,CancellationToken.None, message.ReplyToMessageId, message.OriginalMessage);
            }
        }
    }

    private async Task DeleteMessageViaProtoAsync(ComponentDeleteMessage deleteMessage, CancellationToken cancellationToken)
    {
        if (ProtoSocketSendAsyncMethod == null)
        {
            throw new InvalidOperationException("Proto socket send method was not found.");
        }

        if (!long.TryParse(deleteMessage.ClanId, out var clanId))
        {
            throw new InvalidOperationException($"Invalid clan id '{deleteMessage.ClanId}' for component delete.");
        }

        if (!long.TryParse(deleteMessage.ChannelId, out var channelId))
        {
            throw new InvalidOperationException($"Invalid channel id '{deleteMessage.ChannelId}' for component delete.");
        }

        if (!long.TryParse(deleteMessage.MessageId, out var messageId))
        {
            throw new InvalidOperationException($"Invalid message id '{deleteMessage.MessageId}' for component delete.");
        }

        var finalIsPublic = deleteMessage.Mode == 4 ? false : deleteMessage.IsPublic;
        var envelope = new MezonProto.Envelope
        {
            ChannelMessageRemove = new MezonProto.ChannelMessageRemove
            {
                ClanId = clanId,
                ChannelId = channelId,
                MessageId = messageId,
                Mode = deleteMessage.Mode,
                IsPublic = finalIsPublic
            }
        };

        var task = ProtoSocketSendAsyncMethod.Invoke(_client.Socket, [envelope, cancellationToken]) as Task;
        if (task == null)
        {
            throw new InvalidOperationException("Proto socket send invocation did not return a task.");
        }

        await task;
    }

    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!raw.StartsWith("{", StringComparison.Ordinal))
        {
            return raw;
        }

        try
        {
            using var json = JsonDocument.Parse(raw);
            return json.RootElement.TryGetProperty("t", out var textNode)
                ? textNode.GetString()
                : raw;
        }
        catch
        {
            _logger.LogWarning("Failed to parse JSON message content");
            return raw;
        }
    }

    private static string? TryGetClanIdFromCustomId(string? customId)
    {
        if (string.IsNullOrWhiteSpace(customId))
        {
            return null;
        }

        var parts = customId.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        return parts[0].Equals("CREATE_TEAM", StringComparison.OrdinalIgnoreCase)
            || parts[0].Equals("CANCEL_TEAM", StringComparison.OrdinalIgnoreCase)
            ? parts[1]
            : parts[0].Equals("ACCEPT", StringComparison.OrdinalIgnoreCase)
                || parts[0].Equals("REJECT", StringComparison.OrdinalIgnoreCase)
                ? parts.Length >= 4 ? parts[3] : null
                : null;
    }
}
