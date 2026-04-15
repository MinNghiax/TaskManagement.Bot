
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Commands;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

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
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<ICommandHandler> _handlers;
    private MezonClient? _client;
    private HashSet<string> _dmChannelIds = new();

    public BotService(
        ILogger<BotService> logger,
        IConfiguration configuration,
        IEnumerable<ICommandHandler> handlers)
    {
        _logger = logger;
        _configuration = configuration;
        _handlers = handlers;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤖 Starting Mezon Bot...");
        var options = new MezonClientOptions
        {
            BotId = _configuration["Mezon:BotId"] ?? throw new Exception("Missing BotId"),
            Token = _configuration["Mezon:Token"] ?? throw new Exception("Missing Token"),
            Host = _configuration["Mezon:Host"] ?? "gw.mezon.ai",
            Port = _configuration["Mezon:Port"] ?? "443",
            UseSSL = bool.Parse(_configuration["Mezon:UseSsl"] ?? "true"),
            TimeoutMs = int.Parse(_configuration["Mezon:TimeoutMs"] ?? "10000")
        };
        _client = new MezonClient(options);
        _client.On("channel_message", OnChannelMessage);
        await _client.LoginAsync(cancellationToken);
        try
        {
            var dmChannels = await _client.ListChannelsAsync(channelType: 1, cancellationToken: cancellationToken);
            _dmChannelIds = dmChannels.ChannelDescs != null
                ? new HashSet<string>(dmChannels.ChannelDescs.Select(c => c.ChannelId ?? ""))
                : new HashSet<string>();
            _logger.LogInformation($"[DM CACHE] Cached {_dmChannelIds.Count} DM channel ids");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DM CACHE] Failed to cache DM channel ids");
        }
        _logger.LogInformation("✅ Bot connected & ready!");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null)
        {
            _logger.LogInformation("🔌 Stopping bot...");
            await _client.LogoutAsync(cancellationToken);
        }
    }

    public async Task SendMessageAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: new ChannelMessageContent { Text = text },
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("✅ Sent!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send failed");
            throw;
        }
    }

    private async Task SendMessageWithEmbedAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        IInteractiveMessageProps embed)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND_EMBED] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            var content = new ChannelMessageContent
            {
                Text = text ?? "",
                Embed = new object[] { embed }
            };

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
                cancellationToken: CancellationToken.None
            );

            _logger.LogInformation("✅ Sent with embed!");
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid channel identifier") && mode == 2)
        {
            _logger.LogWarning($"[RETRY] Got 'Invalid channel identifier' with mode=2, retrying with mode=3 (private group)...");
            try
            {
                var content = new ChannelMessageContent
                {
                    Text = string.IsNullOrWhiteSpace(text) ? "📊 Report" : text,
                    Embed = new object[] { embed }
                };

                await _client.SendMessageAsync(
                    clanId: clanId,
                    channelId: channelId,
                    mode: 3,
                    isPublic: false,
                    content: content,
                    cancellationToken: CancellationToken.None
                );

                _logger.LogInformation("✅ Sent with embed (mode=3 private group)!");
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "❌ Send with embed (retry mode=3) failed");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send with embed failed");
            throw;
        }
    }

    private async Task SendFormMessageAsync(
        string clanId,
        string channelId,
        ChannelMessageContent content,
        int mode,
        bool isPublic)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND_FORM] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
                cancellationToken: CancellationToken.None
            );

            _logger.LogInformation("✅ Sent interactive form!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send form failed");
            throw;
        }
    }

    private async void OnChannelMessage(object? sender, Mezon.Sdk.Interfaces.MezonEventArgs e)
    {
        try
        {
            if (_client == null) return;
            if (e.Data is not ChannelMessage message) return;

            _logger.LogInformation($"📦 MESSAGE FULL: ClanId={message.ClanId} ChannelId={message.ChannelId} Mode={message.Mode} IsPublic={message.IsPublic} Sender={message.SenderId}");

            var rawText = message.Content?.Text;
            _logger.LogInformation($"📥 RAW: {rawText}");

            var content = ParseContent(rawText);
            _logger.LogInformation($"📥 PARSED: {content}");

            if (string.IsNullOrWhiteSpace(content)) return;
            if (message.SenderId == _client.ClientId) return;

            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(content))
                {
                    var response = await handler.HandleAsync(message, CancellationToken.None);

                    if (response == null || (string.IsNullOrEmpty(response.Text) && response.Embed == null && response.Content == null))
                        break;
                    var finalMode = message.Mode ?? 2;
                    var finalIsPublic = message.IsPublic ?? true;

                    _logger.LogInformation($"[SEND_MODE] Mode={message.Mode}→{finalMode} IsPublic={message.IsPublic}→{finalIsPublic}");

                    if (response.Content != null)
                    {
                        await SendFormMessageAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Content,
                            finalMode,
                            finalIsPublic
                        );
                    }
                    else if (response.Embed != null)
                    {
                        await SendMessageWithEmbedAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Text ?? "",
                            finalMode,
                            finalIsPublic,
                            response.Embed
                        );
                    }
                    else
                    {
                        await SendMessageAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Text ?? "",
                            finalMode,
                            finalIsPublic
                        );
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Handle message error");
        }
    }

    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (raw.StartsWith("{"))
        {
            try
            {
                var json = JsonDocument.Parse(raw);
                return json.RootElement.GetProperty("t").GetString();
            }
            catch
            {
                _logger.LogWarning("⚠️ Parse JSON failed");
            }
        }
        return raw;
    }
}