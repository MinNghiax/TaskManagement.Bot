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
        CancellationToken cancellationToken = default);
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
    private readonly ConcurrentDictionary<string, string> _channelClanMap = new();
    private HashSet<string> _dmChannelIds = new();

    public BotService(
        ILogger<BotService> logger,
        MezonClient client,
        IEnumerable<ICommandHandler> commandHandlers,
        IEnumerable<IComponentHandler> componentHandlers)
    {
        _logger = logger;
        _client = client;
        _commandHandlers = commandHandlers;
        _componentHandlers = componentHandlers;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Mezon Bot...");

        _client.On("channel_message", OnChannelMessage);
        _client.On("message_button_clicked", OnComponent);

        await _client.LoginAsync(cancellationToken);

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
        CancellationToken cancellationToken = default)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;

        _logger.LogInformation(
            "[SEND] ClanId={ClanId} ChannelId={ChannelId} Mode={Mode} IsPublic={IsPublic}",
            clanId,
            channelId,
            mode,
            finalIsPublic);

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: finalIsPublic,
            content: new ChannelMessageContent { Text = text },
            cancellationToken: cancellationToken);
    }

    private async Task SendMessageWithEmbedAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        IInteractiveMessageProps embed)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;
        var content = new ChannelMessageContent
        {
            Text = text,
            Embed = new object[] { embed }
        };

        try
        {
            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
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
        bool isPublic)
    {
        var finalIsPublic = mode == 4 ? false : isPublic;

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: finalIsPublic,
            content: content,
            cancellationToken: CancellationToken.None);
    }

    private async void OnChannelMessage(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not ChannelMessage message)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(message.ChannelId) && !string.IsNullOrWhiteSpace(message.ClanId))
            {
                _channelClanMap[message.ChannelId] = message.ClanId;
            }

            var content = ParseContent(message.Content?.Text);
            if (string.IsNullOrWhiteSpace(content) || message.SenderId == _client.ClientId)
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
                    await SendFormMessageAsync(message.ClanId, message.ChannelId, response.Content, finalMode, finalIsPublic);
                }
                else if (response.Embed != null)
                {
                    await SendMessageWithEmbedAsync(message.ClanId, message.ChannelId, response.Text ?? string.Empty, finalMode, finalIsPublic, response.Embed);
                }
                else
                {
                    await SendMessageAsync(message.ClanId, message.ChannelId, response.Text ?? string.Empty, finalMode, finalIsPublic);
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
                Mode = 2,
                IsPublic = true
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
                await SendFormMessageAsync(message.ClanId, message.ChannelId, message.Content, message.Mode, message.IsPublic);
            }
            else if (!string.IsNullOrWhiteSpace(message.Text))
            {
                await SendMessageAsync(message.ClanId, message.ChannelId, message.Text, message.Mode, message.IsPublic);
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
            : null;
    }
}
