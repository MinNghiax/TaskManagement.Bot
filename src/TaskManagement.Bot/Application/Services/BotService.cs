using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mezon.Sdk;
using Mezon.Sdk.Client;
using Mezon.Sdk.Proto;
using TaskManagement.Bot.Examples;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// Service to initialize and manage Mezon bot connection.
/// Handles incoming messages and bot lifecycle.
/// </summary>
public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(long clanId, long channelId, string text, CancellationToken cancellationToken = default);
}

public class BotService : IBotService
{
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITaskService _taskService;
    private MezonClient? _client;
    private BotCommandHandlerExample? _commandHandler;

    public BotService(
        ILogger<BotService> logger,
        IConfiguration configuration,
        ITaskService taskService)
    {
        _logger = logger;
        _configuration = configuration;
        _taskService = taskService;
    }

    /// <summary>
    /// Initialize and start the Mezon bot connection.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🤖 [Bot] Initializing Mezon bot connection...");

            // Read Mezon configuration from appsettings.json
            var mezonConfig = new MezonClientConfig
            {
                BotId = _configuration["Mezon:BotId"] ?? throw new InvalidOperationException("Mezon:BotId not configured"),
                Token = _configuration["Mezon:Token"] ?? throw new InvalidOperationException("Mezon:Token not configured"),
                Host = _configuration["Mezon:Host"] ?? "gw.mezon.ai",
                Port = _configuration["Mezon:Port"] ?? "443",
                UseSsl = bool.Parse(_configuration["Mezon:UseSsl"] ?? "true"),
                TimeoutMs = int.Parse(_configuration["Mezon:TimeoutMs"] ?? "7000")
            };

            // Create Mezon client with configuration
            _client = new MezonClient(mezonConfig);

            _logger.LogInformation($"✓ Bot ID: {mezonConfig.BotId}");
            _logger.LogInformation($"✓ Server: {(mezonConfig.UseSsl ? "https" : "http")}://{mezonConfig.Host}:{mezonConfig.Port}");

            // Subscribe to bot events
            SubscribeToEvents();

            // Login to Mezon server
            _logger.LogInformation("📡 Connecting to Mezon server...");
            await _client.LoginAsync(cancellationToken);

            _logger.LogInformation("✅ [Bot] Successfully connected to Mezon!");
            _logger.LogInformation("👂 [Bot] Listening for incoming messages...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Bot] Failed to start bot connection");
            throw;
        }
    }

    /// <summary>
    /// Stop the bot and clean up resources.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null)
        {
            _logger.LogInformation("🔌 [Bot] Disconnecting from Mezon...");
            await _client.DisposeAsync();
            _logger.LogInformation("✓ [Bot] Disconnected");
        }
    }

    /// <summary>
    /// Send a message to a Mezon channel.
    /// </summary>
    public async Task SendMessageAsync(long clanId, long channelId, string text, CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️  [Bot] Client not initialized. Cannot send message.");
            return;
        }

        try
        {
            var clanIdStr = clanId.ToString();
            var channelIdStr = channelId.ToString();
            
            _logger.LogInformation($"📤 [Bot] Preparing to send message:");
            _logger.LogInformation($"         Clan ID (string): {clanIdStr}");
            _logger.LogInformation($"         Channel ID (string): {channelIdStr}");
            _logger.LogInformation($"         Mode: 0, IsPublic: true");
            _logger.LogInformation($"         Text: '{text}'");
            
            // Send message using Mezon SDK
            _logger.LogInformation($"⏳ [Bot] Awaiting SendTextAsync...");
            
            var sendTask = _client.SendTextAsync(
                clanId: clanIdStr,
                channelId: channelIdStr,
                mode: 2,  // ChannelStreamMode.Channel
                isPublic: true,
                text: text,
                ct: cancellationToken);
            
            _logger.LogInformation($"⏳ [Bot] SendTextAsync task created, awaiting completion...");
            await sendTask;
            
            _logger.LogInformation($"✅ [Bot] SendTextAsync completed without exception");
            _logger.LogInformation($"✅ [Bot] Successfully sent message to channel {channelIdStr}: '{text}'");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "❌ [Bot] Message send cancelled (timeout)");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "❌ [Bot] Message send operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [Bot] EXCEPTION during SendTextAsync!");
            _logger.LogError($"   Exception Type: {ex.GetType().FullName}");
            _logger.LogError($"   Message: {ex.Message}");
            _logger.LogError($"   StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "   >>> Inner Exception:");
            }
        }
    }

    /// <summary>
    /// Subscribe to Mezon events.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (_client == null) return;

        // Initialize command handler with properly typed logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var commandLogger = loggerFactory.CreateLogger<BotCommandHandlerExample>();
        
        _commandHandler = new BotCommandHandlerExample(
            commandLogger,
            _taskService,
            _configuration);

        // Subscribe to incoming messages
        _client.ChannelMessage += OnChannelMessage;

        // Subscribe to ready event (connection established)
        _client.Ready += OnReady;
    }

    /// <summary>
    /// Handle incoming channel messages.
    /// </summary>
    private async void OnChannelMessage(ChannelMessage message)
    {
        try
        {
            _logger.LogInformation($"📬 [Received Message]");
            _logger.LogInformation($"   Clan ID: {message.ClanId}");
            _logger.LogInformation($"   Channel ID: {message.ChannelId}");
            _logger.LogInformation($"   Sender: {message.Username} (ID: {message.SenderId})");
            _logger.LogInformation($"   Content: {message.Content}");
            _logger.LogInformation($"   Time: {UnixTimeStampToDateTime(message.CreateTimeSeconds)}");

            // Ignore bot's own messages
            if (message.Username.Contains("bot", StringComparison.OrdinalIgnoreCase))
                return;

            // Process command handler (it handles both commands and default replies)
            if (_commandHandler != null && _client != null)
            {
                // Fire and forget (don't await to avoid blocking)
#pragma warning disable CS4014
                HandleCommandAsync(message);
#pragma warning restore CS4014
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing channel message");
        }
    }

    /// <summary>
    /// Handle command processing async
    /// </summary>
    private async Task HandleCommandAsync(ChannelMessage message)
    {
        try
        {
            if (_commandHandler == null || _client == null)
                return;

            // Process message through command handler
            await _commandHandler.ProcessMessageAsync(message, _client);

            // Give message queue time to process
            await Task.Delay(500);
            _logger.LogInformation($"✅ [Bot] Reply sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in command handling");
        }
    }

    /// <summary>
    /// Handle ready event (bot successfully connected and ready).
    /// </summary>
    private void OnReady()
    {
        _logger.LogInformation("🟢 [Bot] Ready event received - Bot is fully initialized and listening!");
    }

    /// <summary>
    /// Convert Unix timestamp to DateTime.
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}
