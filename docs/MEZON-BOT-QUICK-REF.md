# Mezon Bot Connection - Quick Reference

## 📦 Files Created

### 1. Program.cs (Updated)
**Location:** `src/TaskManagement.Bot/Program.cs`

**Purpose:** Entry point - Initializes DI container, starts bot service

**Key Features:**
- Configuration loading from `appsettings.json` and `appsettings.Development.json`
- Dependency Injection setup for BotService
- Console logging configuration
- Graceful shutdown handling (Ctrl+C)
- Error handling with detailed logging

```bash
# Run the bot:
cd src/TaskManagement.Bot
dotnet run
```

---

### 2. BotService.cs (New)
**Location:** `src/TaskManagement.Bot/Application/Services/BotService.cs`

**Purpose:** Manages Mezon client connection and message handling

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `StartAsync()` | Connect to Mezon server |
| `StopAsync()` | Disconnect gracefully |
| `SendMessageAsync()` | Send message to channel |

**Event Handlers:**
- `OnChannelMessage()` - Logs incoming messages with details
- `OnReady()` - Confirms bot is ready

**Features:**
- Reads Mezon config from appsettings
- Creates MezonClient with proper configuration
- Subscribes to bot events
- Detailed logging at each step
- Error handling with try/catch

---

### 3. appsettings.Development.json (New)
**Location:** `src/TaskManagement.Bot/appsettings.Development.json`

**Purpose:** Environment-specific configuration for Mezon credentials

**Required Configuration:**
```json
{
  "Mezon": {
    "BotId": "YOUR_BOT_ID",      // ← Update with actual ID
    "Token": "YOUR_BOT_TOKEN",   // ← Update with actual token
    "Host": "gw.mezon.ai",       // Keep default or customize
    "Port": "443",               // Keep default
    "UseSsl": true,              // Keep default (true)
    "TimeoutMs": 7000            // Keep default
  }
}
```

---

### 4. MEZON-BOT-SETUP.md (New)
**Location:** `docs/MEZON-BOT-SETUP.md`

**Purpose:** Comprehensive guide with troubleshooting and examples

---

## 🎯 Initial Setup Steps

### Step 1️⃣ Configure Credentials
```bash
# Open appsettings.Development.json
# Replace BotId and Token with your actual values
```

### Step 2️⃣ Build
```bash
cd src/TaskManagement.Bot
dotnet build
```

### Step 3️⃣ Run
```bash
dotnet run
```

### Step 4️⃣ Send Test Message
From a Mezon client, send a message to the bot's channel

### Step 5️⃣ Verify Logs
Bot should log:
- ✅ Connection established
- 📬 Incoming message with details
- ✓ Message handling confirmation

### Step 6️⃣ Stop
Press **Ctrl+C** to gracefully shutdown

---

## 📊 Data Flow

```
User sends message in Mezon
         ↓
Mezon server receives message
         ↓
Message forwarded to bot (WebSocket)
         ↓
BotService.OnChannelMessage() triggered
         ↓
Log message details to console
         ↓
(Future: Process & reply)
```

---

## 🔍 What Gets Logged

When a message arrives, you'll see:

```
📬 [Received Message]
   Clan ID: 12345
   Channel ID: 67890
   Sender: john_doe (ID: 99999)
   Content: Hello bot!
   Time: 2024-04-06 14:30:45
   ✓ Message from user - Bot would reply: 'Bot is working!'
```

---

## ⚙️ Configuration Properties

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `BotId` | string | - | Required. Unique bot identifier |
| `Token` | string | - | Required. Authentication token |
| `Host` | string | gw.mezon.ai | Mezon server hostname |
| `Port` | string | 443 | Mezon server port |
| `UseSsl` | bool | true | Use HTTPS/WSS |
| `TimeoutMs` | int | 7000 | Connection timeout |

---

## 🐛 Common Issues

### ❌ "Mezon:BotId not configured"
**Solution:** Update `appsettings.Development.json` with your BotId

### ❌ "Failed to connect"
**Solution:** Verify token is valid and server is reachable

### ❌ "No messages appearing"
**Solution:** Verify bot is registered in the correct clan/channel

---

## 📝 Code Snippets

### Read Config in BotService
```csharp
var botId = _configuration["Mezon:BotId"];
var token = _configuration["Mezon:Token"];
```

### Create Client
```csharp
var config = new MezonClientConfig
{
    BotId = botId,
    Token = token,
    Host = host,
    Port = port,
    UseSsl = useSsl,
    TimeoutMs = timeoutMs
};
var client = new MezonClient(config);
```

### Subscribe to Events
```csharp
client.ChannelMessage += OnChannelMessage;
client.Ready += OnReady;
```

### Connect
```csharp
await client.LoginAsync(cancellationToken);
```

### Handle Messages
```csharp
private void OnChannelMessage(ChannelMessage message)
{
    _logger.LogInformation($"From {message.Username}: {message.Content}");
}
```

---

## ✅ Quick Verification

Run this command to verify setup:
```bash
cd src/TaskManagement.Bot
dotnet run
```

**Expected output sequence:**
1. ✓ Application initializing
2. ✓ Mezon bot initializing
3. ✓ Bot ID and server logged
4. ✓ Connecting to server
5. ✓ Successfully connected
6. ✓ Ready state

---

## 📚 Mezon SDK Classes Used

| Class | Purpose |
|-------|---------|
| `MezonClient` | Main bot client |
| `MezonClientConfig` | Configuration holder |
| `ChannelMessage` | Incoming message data |

---

## 🚀 Next Steps

After verifying connection:

1. Implement actual message processing logic
2. Create command handlers (e.g., `!task create Task Name`)
3. Integrate with Application/Service layer
4. Add database persistence
5. Create task management features

---

## 📖 Related Documentation

- [MEZON-BOT-SETUP.md](MEZON-BOT-SETUP.md) - Detailed setup guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - Project architecture
- [FOLDER-STRUCTURE.md](FOLDER-STRUCTURE.md) - Project organization

---

**Status: ✅ Ready for Testing**

The minimal working example is complete. Configure credentials and run `dotnet run` to test connection!
