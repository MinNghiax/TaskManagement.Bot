# Mezon Bot Connection - Testing Guide

This document explains the minimal working example to test Mezon SDK connection.

## 📋 What's Included

### 1. **BotService** (`Application/Services/BotService.cs`)
- Handles Mezon client initialization
- Reads configuration from `appsettings.json`
- Manages bot connection lifecycle
- Listens to incoming messages
- Provides logging at each step

### 2. **Program.cs** (Entry Point)
- Sets up Dependency Injection
- Configures logging
- Initializes and starts the bot
- Handles graceful shutdown with Ctrl+C

### 3. **appsettings.Development.json** (Configuration)
- Mezon authentication credentials (BotId, Token)
- Server connection settings (Host, Port, SSL)
- Timeout configuration

## 🚀 Quick Start

### Step 1: Configure Credentials

Edit `appsettings.Development.json`:

```json
{
  "Mezon": {
    "BotId": "YOUR_BOT_ID",           // ← Replace with your bot ID
    "Token": "YOUR_BOT_TOKEN",        // ← Replace with your bot token
    "Host": "gw.mezon.ai",            // Keep default or use custom server
    "Port": "443",                    // Default Mezon port
    "UseSsl": true,                   // Use SSL/TLS
    "TimeoutMs": 7000                 // Connection timeout
  }
}
```

**Where to get BotId and Token:**
- Contact your Mezon server administrator
- These should be provided when the bot is registered

### Step 2: Build Project

```bash
cd src/TaskManagement.Bot
dotnet build
```

### Step 3: Run the Bot

```bash
dotnet run
```

**Expected Output:**

```
╔═══════════════════════════════════════════════════╗
║    🤖 TaskManagement.Bot - Mezon Connection Test   ║
╚═══════════════════════════════════════════════════╝

info: TaskManagement.Bot.Program[0]
      🚀 [Startup] Application initializing...
info: TaskManagement.Bot.Application.Services.BotService[0]
      🤖 [Bot] Initializing Mezon bot connection...
info: TaskManagement.Bot.Application.Services.BotService[0]
      ✓ Bot ID: YOUR_BOT_ID
info: TaskManagement.Bot.Application.Services.BotService[0]
      ✓ Server: https://gw.mezon.ai:443
info: TaskManagement.Bot.Application.Services.BotService[0]
      📡 Connecting to Mezon server...
info: TaskManagement.Bot.Application.Services.BotService[0]
      ✅ [Bot] Successfully connected to Mezon!
info: TaskManagement.Bot.Application.Services.BotService[0]
      👂 [Bot] Listening for incoming messages...
info: TaskManagement.Bot.Program[0]
      ✅ [Ready] Bot is running. Press Ctrl+C to stop.
```

### Step 4: Test Message Reception

From a Mezon client (web, mobile, desktop), send a message to the bot's channel.

Expected log output:

```
info: TaskManagement.Bot.Application.Services.BotService[0]
      📬 [Received Message]
info: TaskManagement.Bot.Application.Services.BotService[0]
      Clan ID: 12345
info: TaskManagement.Bot.Application.Services.BotService[0]
      Channel ID: 67890
info: TaskManagement.Bot.Application.Services.BotService[0]
      Sender: john_doe (ID: 99999)
info: TaskManagement.Bot.Application.Services.BotService[0]
      Content: Hello bot!
info: TaskManagement.Bot.Application.Services.BotService[0]
      Time: 2024-04-06 14:30:45
info: TaskManagement.Bot.Application.Services.BotService[0]
      ✓ Message from user - Bot would reply: 'Bot is working!'
```

### Step 5: Stop the Bot

Press **Ctrl+C** in the terminal:

```
^C
⚠️  [Shutdown] Ctrl+C received, gracefully shutting down...
🔌 [Bot] Disconnecting from Mezon...
✓ [Bot] Disconnected
✓ [Shutdown] Application stopped successfully
```

## 📁 Project Structure

```
src/TaskManagement.Bot/
├── Program.cs                           ← Bot entry point (updated)
├── appsettings.Development.json         ← Bot configuration (NEW)
├── Application/
│   └── Services/
│       └── BotService.cs                ← Bot connection logic (NEW)
├── Domain/
│   └── Entities/
├── Infrastructure/
└── Presentation/
    └── Controllers/
```

## 🔧 How It Works

### Connection Flow

```
1. Program.cs
   ├─ Read configuration from appsettings.json
   ├─ Setup Dependency Injection
   ├─ Create BotService instance
   └─ Call StartAsync()

2. BotService.StartAsync()
   ├─ Create MezonClientConfig from settings
   ├─ Create MezonClient
   ├─ Subscribe to events (ChannelMessage, Ready)
   └─ Call LoginAsync() → Connect to server

3. Listen for Events
   ├─ OnChannelMessage() ← Triggered when message arrives
   ├─ OnReady() ← Triggered when connection ready
   └─ Logs all incoming messages to console
```

### Event Handling

**ChannelMessage Event:** Fired when a message arrives

```csharp
// Logs:
// - Clan ID
// - Channel ID  
// - Sender username and ID
// - Message content
// - Timestamp
```

**Ready Event:** Fired when bot is fully initialized

```csharp
// Indicates bot is connected and ready to receive messages
```

## 🐛 Troubleshooting

### Issue: "Failed to connect to Mezon server"

**Causes:**
- Invalid BotId or Token
- Mezon server unreachable
- Network connectivity issue

**Solutions:**
```bash
# 1. Verify credentials
# Edit appsettings.Development.json and double-check BotId and Token

# 2. Test network connectivity
ping gw.mezon.ai

# 3. Check firewall/proxy settings
# Ensure port 443 is open for HTTPS

# 4. Try with verbose logging - change minimum level to Debug
# In Program.cs: config.SetMinimumLevel(LogLevel.Debug);
```

### Issue: "BotId or Token not configured"

**Solution:**
```json
// Make sure appsettings.Development.json has these keys:
{
  "Mezon": {
    "BotId": "actual_value_here",
    "Token": "actual_token_here"
  }
}
```

### Issue: "No incoming messages appearing in logs"

**Possible causes:**
- Bot not subscribed to correct clan/channel
- Messages being sent to wrong channel
- Message event handler not triggered

**Debug steps:**
```csharp
// Add more logging in BotService.StartAsync():
_logger.LogInformation($"✓ Available Clans: {string.Join(", ", _client.Clans.GetAll())}");

// Subscribe to more events to see what's happening:
_client.Notification += OnNotification;
_client.UserChannelAdded += OnUserChannelAdded;
```

## 📝 Configuration Options

### Mezon Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `BotId` | Required | Your bot's unique identifier |
| `Token` | Required | Bot authentication token |
| `Host` | `gw.mezon.ai` | Mezon server hostname |
| `Port` | `443` | Mezon server port (usually 443 for HTTPS) |
| `UseSsl` | `true` | Enable SSL/TLS encryption |
| `TimeoutMs` | `7000` | Connection timeout in milliseconds |

### Example: Custom Server

```json
{
  "Mezon": {
    "BotId": "bot_123",
    "Token": "token_abc",
    "Host": "custom.mezon.server",
    "Port": "8443",
    "UseSsl": true,
    "TimeoutMs": 10000
  }
}
```

## 🎯 Next Steps

After verifying the connection works:

1. **Implement Message Handling:** Process incoming messages in `OnChannelMessage()`
2. **Send Replies:** Use `SendMessageAsync()` to respond
3. **Add Commands:** Create a command parser (e.g., "!task create ...")
4. **Database Integration:** Save messages/tasks to database using Repository Pattern
5. **Create Proper Services:** Move bot logic to Application/Services

## 📚 Code Structure

### BotService Interface & Implementation

```csharp
public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string clanId, string channelId, string text, CancellationToken cancellationToken = default);
}

public class BotService : IBotService
{
    // Implementation with event handlers
}
```

### Key Methods

| Method | Purpose |
|--------|---------|
| `StartAsync()` | Initialize connection, read config, login |
| `StopAsync()` | Gracefully disconnect and cleanup |
| `SendMessageAsync()` | Send a message to a channel |
| `OnChannelMessage()` | Handle incoming messages |
| `OnReady()` | Handle connection ready event |

## ✅ Verification Checklist

- [ ] `appsettings.Development.json` created with valid BotId and Token
- [ ] `BotService.cs` created in `Application/Services/`
- [ ] `Program.cs` updated with DI and bot initialization
- [ ] Project builds without errors: `dotnet build`
- [ ] Bot connects: `dotnet run` shows "✅ Successfully connected"
- [ ] Bot receives messages: Incoming messages appear in console logs
- [ ] Graceful shutdown: Ctrl+C disconnects cleanly

## 🔗 Mezon SDK References

Key Mezon SDK classes used:

- `MezonClient` - Main bot client class
- `MezonClientConfig` - Configuration holder
- `ChannelMessage` - Incoming message data structure
- `Session` - Server session after authentication

Event Types:
- `Ready` - Connection established
- `ChannelMessage` - Message received
- `Notification` - Notification received
- `MessageReaction` - Emoji reaction received

## 📞 Support

If you encounter issues:

1. Check logs - look for error messages
2. Verify credentials - double-check BotId and Token
3. Check network - ensure server is reachable
4. Review Mezon SDK documentation - check client usage examples
5. Add Debug logging - change LogLevel to Debug for more details

---

**Happy Bot Development! 🚀**
