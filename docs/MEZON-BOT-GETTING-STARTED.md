# ✅ Mezon Bot - Getting Started Checklist

## 📦 What You Received

A **complete minimal working example** to test Mezon SDK connection in your layered architecture project.

### ✅ Files Delivered

#### Code Files
- ✅ **BotService.cs** - Core bot service with connection management
- ✅ **Program.cs** - Updated entry point with DI and bot initialization
- ✅ **BotCommandHandlerExample.cs** - Example showing command handling and service integration
- ✅ **appsettings.Development.json** - Mezon configuration template

#### Documentation
- ✅ **MEZON-BOT-SETUP.md** - Comprehensive setup guide (500+ lines)
- ✅ **MEZON-BOT-QUICK-REF.md** - Quick reference card
- ✅ **MEZON-BOT-SUMMARY.md** - Complete overview
- ✅ **MEZON-BOT-ARCHITECTURE.md** - Architecture diagrams
- ✅ **MEZON-BOT-GETTING-STARTED.md** - This file

---

## 🚀 Quick Start (5 Minutes)

### Step 1️⃣: Configure Credentials (1 min)

**File:** `src/TaskManagement.Bot/appsettings.Development.json`

```json
{
  "Mezon": {
    "BotId": "YOUR_ACTUAL_BOT_ID",      // ← Replace with your bot ID
    "Token": "YOUR_ACTUAL_BOT_TOKEN"    // ← Replace with your token
  }
}
```

**Where to get these:**
- Contact your Mezon server administrator
- Should have been provided when bot was registered

### Step 2️⃣: Build Project (2 min)

```bash
cd src/TaskManagement.Bot
dotnet build
```

**Expected**: Build completes with no errors

### Step 3️⃣: Run Bot (1 min)

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
      ✅ [Bot] Successfully connected to Mezon!
info: TaskManagement.Bot.Application.Services.BotService[0]
      👂 [Bot] Listening for incoming messages...

✅ [Ready] Bot is running. Press Ctrl+C to stop.
```

### Step 4️⃣: Test Message Reception (1 min)

1. Open a Mezon client (web, mobile, desktop)
2. Navigate to the bot's channel
3. Send: `Hello bot!`

**Expected Log:**
```
info: TaskManagement.Bot.Application.Services.BotService[0]
      📬 [Received Message]
      Clan ID: 12345
      Channel ID: 67890
      Sender: your_username (ID: 99999)
      Content: Hello bot!
      Time: 2024-04-06 14:30:45
      ✓ Message from user - Bot would reply: 'Bot is working!'
```

### Step 5️⃣: Stop Bot (30 sec)

Press **Ctrl+C** in the terminal

**Expected**: Clean shutdown with disconnect message

---

## 📚 Documentation Guide

| Doc | Purpose | Read Time | Audience |
|-----|---------|-----------|----------|
| **MEZON-BOT-SETUP.md** | Complete setup guide with troubleshooting | 15 min | First-time setup |
| **MEZON-BOT-QUICK-REF.md** | Quick reference card for common tasks | 5 min | Quick lookup |
| **MEZON-BOT-SUMMARY.md** | Architecture, data flow, integration guide | 12 min | Understanding design |
| **MEZON-BOT-ARCHITECTURE.md** | Visual diagrams of system architecture | 10 min | Visual learners |
| **MEZON-BOT-GETTING-STARTED.md** | This file - quick checklist | 3 min | Immediate start |

---

## 🎯 Verification Checklist

After following Quick Start above, verify:

- [ ] Project builds without errors: `dotnet build`
- [ ] Bot connects to Mezon server (see "Successfully connected" in logs)
- [ ] Bot waits for messages (see "Listening for incoming messages")
- [ ] Incoming messages are logged (sends test message and see logs)
- [ ] Logs show message details (sender, content, timestamp)
- [ ] Graceful shutdown works (Ctrl+C stops cleanly)

**All checks passing?** ✅ You're ready!

---

## 🔍 What Works Now

### ✅ Phase 1: Connection Testing (Ready)
- Bot connects to Mezon server
- Receives incoming messages
- Logs all message details to console
- Handles graceful shutdown

### ✅ Phase 2: Command Handling (Example Provided)
- Example code shows command parsing (`!task create`, `!task list`, etc.)
- Integration points with TaskService documented
- Error handling patterns shown
- Ready to implement and extend

### ✅ Phase 3: Full Integration (Next Step)
- Framework in place for service integration
- TaskService already implemented
- Database infrastructure ready
- Just need to wire together

---

## 📁 File Locations

```
src/TaskManagement.Bot/
├── Program.cs                    ← UPDATED (Entry point)
├── appsettings.Development.json  ← NEW (Config)
├── Application/Services/
│   ├── BotService.cs             ← NEW (Bot logic)
│   ├── TaskService.cs            ← EXISTING
│   └── ...other services
└── Examples/
    └── BotCommandHandlerExample.cs  ← NEW (Usage example)

docs/
├── MEZON-BOT-SETUP.md           ← NEW (Detailed guide)
├── MEZON-BOT-QUICK-REF.md       ← NEW (Quick reference)
├── MEZON-BOT-SUMMARY.md         ← NEW (Overview)
├── MEZON-BOT-ARCHITECTURE.md    ← NEW (Diagrams)
└── MEZON-BOT-GETTING-STARTED.md ← NEW (This file)
```

---

## 🐛 Troubleshooting Quick Links

### Problem: "Mezon:BotId not configured"
→ See [appsettings.Development.json Configuration](#step-1️⃣-configure-credentials-1-min)

### Problem: "Failed to connect to Mezon"
→ See [MEZON-BOT-SETUP.md - Troubleshooting](MEZON-BOT-SETUP.md#-troubleshooting)

### Problem: "No messages appearing in logs"
→ See [MEZON-BOT-SETUP.md - Troubleshooting](MEZON-BOT-SETUP.md#issue-no-incoming-messages-appearing-in-logs)

### Problem: Build errors
→ See [MEZON-BOT-SETUP.md - Getting Started](MEZON-BOT-SETUP.md#-setup-development)

---

## 💻 Code Structure (TL;DR)

### Program.cs
- Loads config from JSON files
- Sets up Dependency Injection
- Creates BotService instance
- Starts bot connection
- Handles Ctrl+C shutdown

### BotService
- Takes ILogger and IConfiguration in constructor
- Creates MezonClient with config
- Calls LoginAsync() to connect
- Subscribes to ChannelMessage event
- Logs all incoming messages
- Handles graceful disconnect

### Event Handler (OnChannelMessage)
- Logs message details (sender, content, time)
- Shows where to add command handling
- Can be extended to call BotCommandHandlerExample

### BotCommandHandlerExample
- Shows how to parse commands
- Routes to specific handlers
- Calls TaskService for database operations
- Generates and sends responses
- Complete with error handling

---

## 🎓 Learning Path

### Beginner (20 min)
1. Run Quick Start steps 1-4 above
2. Watch bot connect and receive messages
3. Read MEZON-BOT-QUICK-REF.md

### Intermediate (45 min)
1. Review BotService.cs code line-by-line
2. Understand event handling pattern
3. Review BotCommandHandlerExample.cs
4. Read MEZON-BOT-SUMMARY.md

### Advanced (2+ hours)
1. Plan command structure for your needs
2. Integrate BotCommandHandlerExample logic
3. Implement custom message handlers
4. Add database persistence
5. Test end-to-end flows

---

## 🔧 Common Customizations

### Change Mezon Server
Edit `appsettings.Development.json`:
```json
{
  "Mezon": {
    "Host": "custom.mezon.server",
    "Port": "8443"
  }
}
```

### Change Log Level
Edit `Program.cs`:
```csharp
config.SetMinimumLevel(LogLevel.Debug);  // More verbose
```

### Add More Event Handlers
Edit `BotService.cs`:
```csharp
_client.Notification += OnNotification;
_client.MessageReaction += OnMessageReaction;
```

### Implement Message Sending
See example in `BotCommandHandlerExample.cs`:
```csharp
await message.ReplyTextAsync("Bot is working!");
```

---

## 📊 Architecture at a Glance

```
User (Mezon Client) ←→ Mezon Server (Cloud)
                           ↓ WebSocket
                      BotService (MezonClient)
                           ↓ Events
            (OnChannelMessage, OnReady, etc.)
                           ↓
            BotCommandHandlerExample (Optional)
                           ↓
                   Application Services
            (TaskService, ReminderService, etc.)
                           ↓
                    Repository Pattern
                           ↓
                      SQL Server Database
```

---

## ✨ Features Included

### Phase 1: Connection ✅
- [x] Read config from appsettings.json
- [x] Create MezonClient with secure connection
- [x] Connect to Mezon server
- [x] Listen for incoming messages
- [x] Log messages to console
- [x] Handle graceful shutdown

### Phase 2: Extension (Example) ✅
- [x] Parse commands from messages
- [x] Route to command handlers
- [x] Integration with TaskService shown
- [x] Error handling patterns
- [x] Response generation

### Phase 3: Full Integration 📋
- [ ] Implement actual command handlers
- [ ] Wire up database operations
- [ ] Add reply functionality
- [ ] Test end-to-end

---

## 🚀 Next After Verification

Once you've verified the connection works:

### Option A: Extend with Commands (2-3 hours)
1. Review BotCommandHandlerExample.cs
2. Integrate command handling into BotService
3. Register TaskService in DI
4. Implement command handlers
5. Test with `!task create`, `!task list`, etc.

**Reference:** [MEZON-BOT-SUMMARY.md - Phase 2](MEZON-BOT-SUMMARY.md#phase-2-extending-with-commands)

### Option B: Add More Features (Parallel)
1. Add reminder handling
2. Add report generation
3. Add complaint tracking
4. Customize response messages

### Option C: Production Readiness (Pre-deployment)
1. Move credentials to environment variables
2. Add comprehensive error handling
3. Add message persistence
4. Add unit tests
5. Add logging with proper levels

---

## 📞 Quick Reference

### Run Bot
```bash
cd src/TaskManagement.Bot
dotnet run
```

### Stop Bot
```
Press Ctrl+C
```

### View Logs in Real-Time
```
Watch terminal while bot is running
Look for [Received Message] entries
```

### Configure Credentials
```
Edit: src/TaskManagement.Bot/appsettings.Development.json
Replace: BotId and Token with actual values
```

### Change Logging Level
```
Edit: Program.cs
Find: config.SetMinimumLevel()
```

---

## 💡 Pro Tips

1. **Keep appsettings.Development.json in .gitignore**
   - Never commit bot credentials to git
   - Use environment variables in production

2. **Check Logs First**
   - Bot logs everything that happens
   - Look there before debugging

3. **Test Incrementally**
   - Verify connection first
   - Then add features one at a time
   - Test each piece as you go

4. **Use Debug Level While Developing**
   - Change LogLevel to Debug in Program.cs
   - Provides more detailed information
   - Switch back to Information for production

5. **Keep Event Handlers Simple**
   - Move complex logic to separate handler classes
   - Call async methods without awaiting if not needed
   - Use try/catch around everything

---

## 📋 Pre-Flight Checklist

Before deploying to production:

- [ ] Credentials stored in environment variables
- [ ] Connection timeout is appropriate (default 7s)
- [ ] Error handling is comprehensive
- [ ] Logging level is appropriate (Information for prod)
- [ ] Command handlers are implemented correctly
- [ ] Database migrations are applied
- [ ] Unit tests pass
- [ ] Code reviewed by team member

---

## 🎉 Success Criteria

You've successfully completed this task when:

✅ Bot connects to Mezon server  
✅ Bot receives incoming messages  
✅ Messages logged with full details  
✅ Graceful shutdown works  
✅ Can follow instructions to test connection  
✅ Understand code structure  
✅ Can customize for your needs  

---

## 📚 Full Documentation Index

1. **MEZON-BOT-GETTING-STARTED.md** (This file)
   - Quick checklist and getting started
   - 5-minute quick start
   - Verification steps

2. **MEZON-BOT-SETUP.md**
   - Detailed setup instructions
   - Configuration options
   - Troubleshooting guide
   - Code examples

3. **MEZON-BOT-QUICK-REF.md**
   - Quick reference card
   - Command cheatsheet
   - Important classes
   - Common snippets

4. **MEZON-BOT-SUMMARY.md**
   - Complete overview
   - Architecture explanation
   - Data flow diagrams
   - Learning path

5. **MEZON-BOT-ARCHITECTURE.md**
   - System architecture diagram
   - Message flow visualization
   - DI structure
   - State diagrams

---

## 🔗 Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - Project layered architecture
- [FOLDER-STRUCTURE.md](FOLDER-STRUCTURE.md) - Project organization
- [HUONG-DAN.md](HUONG-DAN.md) - Development guide

---

## 📞 Support

### If something doesn't work:

1. **Check the logs** - Bot logs everything
2. **Review MEZON-BOT-SETUP.md** - Troubleshooting section
3. **Verify credentials** - Double-check BotId and Token
4. **Verify network** - Ensure can reach gw.mezon.ai:443
5. **Check configuration** - Ensure appsettings.Development.json is valid JSON

### Common Issues & Quick Fixes:
- "Build error" → `dotnet clean` then `dotnet build`
- "Connection refused" → Verify network connectivity
- "Token invalid" → Get new token from admin
- "No messages" → Bot is in correct clan/channel?

---

## ⏱️ Time Estimates

| Task | Time | Difficulty |
|------|------|------------|
| Configure & Test Connection | 5 min | Easy |
| Read All Documentation | 30 min | Easy |
| Understand Code Structure | 30 min | Medium |
| Implement Command Handler | 1-2 hours | Medium |
| Full Integration with Services | 2-4 hours | Medium |
| Production Setup | 1-2 hours | Medium-Hard |

---

**Ready to get started?**

👉 **Next Step:** Configure `appsettings.Development.json` and run `dotnet run`

**Questions?** Check [MEZON-BOT-SETUP.md](MEZON-BOT-SETUP.md) or [MEZON-BOT-SUMMARY.md](MEZON-BOT-SUMMARY.md)

**Happy Bot Development! 🚀**
