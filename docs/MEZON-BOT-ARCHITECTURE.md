# Mezon Bot Architecture Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Mezon Server (Cloud)                        │
├─────────────────────────────────────────────────────────────────────┤
│                   gw.mezon.ai:443 (WebSocket/HTTPS)                 │
│                                                                      │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │
│  │   Clan 1     │    │   Clan 2     │    │   Clan N     │          │
│  │              │    │              │    │              │          │
│  │  ┌────────┐  │    │  ┌────────┐  │    │  ┌────────┐  │          │
│  │  │ Channel│  │    │  │ Channel│  │    │  │ Channel│  │          │
│  │  └────────┘  │    │  └────────┘  │    │  └────────┘  │          │
│  │  ┌────────┐  │    │  ┌────────┐  │    │  ┌────────┐  │          │
│  │  │ Msgs   │  │    │  │ Msgs   │  │    │  │ Msgs   │  │          │
│  │  └────────┘  │    │  └────────┘  │    │  └────────┘  │          │
│  └──────────────┘    └──────────────┘    └──────────────┘          │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                    ChannelMessage (incoming)
                    │
┌──────────────────▼──────────────────────────────────────────────────┐
│                                                                      │
│                  🤖 TaskManagement.Bot Application                   │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │              Program.cs (Entry Point)                          │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ • Load configuration (appsettings.json)                 │ │ │
│  │  │ • Setup Dependency Injection                            │ │ │
│  │  │ • Initialize logging                                    │ │ │
│  │  │ • Create BotService instance                            │ │ │
│  │  │ • Start bot connection                                  │ │ │
│  │  │ • Handle graceful shutdown (Ctrl+C)                     │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │              IBotService / BotService                         │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ • Create MezonClient with config                        │ │ │
│  │  │ • Connect to Mezon server (LoginAsync)                  │ │ │
│  │  │ • Subscribe to events:                                  │ │ │
│  │  │   - OnChannelMessage()                                  │ │ │
│  │  │   - OnReady()                                           │ │ │
│  │  │ • Log received messages                                 │ │ │
│  │  │ • Send messages back to channel                         │ │ │
│  │  │ • Graceful disconnect (StopAsync)                       │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │         BotCommandHandlerExample (Optional Extension)         │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ • Parse incoming messages for commands                  │ │ │
│  │  │ • Route to command handlers:                            │ │ │
│  │  │   - !task create [title] [desc]                         │ │ │
│  │  │   - !task list                                          │ │ │
│  │  │   - !task done [id]                                     │ │ │
│  │  │   - !task delete [id]                                   │ │ │
│  │  │   - !help                                               │ │ │
│  │  │ • Call Application Services (TaskService, etc.)         │ │ │
│  │  │ • Send responses back to user                           │ │ │
│  │  │ • Handle errors gracefully                              │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                  Application Layer                            │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ Services:                                                │ │ │
│  │  │ • ITaskService / TaskService                            │ │ │
│  │  │ • IReminderService / ReminderService                    │ │ │
│  │  │ • IReportService / ReportService                        │ │ │
│  │  │ • IComplainService / ComplainService                    │ │ │
│  │  │                                                          │ │ │
│  │  │ DTOs:                                                    │ │ │
│  │  │ • TaskDto, CreateTaskDto, UpdateTaskDto                │ │ │
│  │  │ • ReminderDto, CreateReminderDto, UpdateReminderDto    │ │ │
│  │  │ • ReportDto, CreateReportDto                           │ │ │
│  │  │ • ComplainDto, CreateComplainDto, UpdateComplainStatusDto │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                  Domain Layer                                 │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ Entities:                                                │ │ │
│  │  │ • Task (Title, Description, Status, Priority, etc.)    │ │ │
│  │  │ • Reminder (TaskId, ReminderTime, Message, etc.)       │ │ │
│  │  │ • Report (Title, ReportType, Statistics, etc.)         │ │ │
│  │  │ • Complain (TaskId, Content, Status, etc.)             │ │ │
│  │  │                                                          │ │ │
│  │  │ Enums:                                                   │ │ │
│  │  │ • TaskStatus (ToDo, InProgress, Completed, Cancelled)  │ │ │
│  │  │ • ComplainStatus (Pending, InProgress, Resolved, ...)  │ │ │
│  │  │ • PriorityLevel (Low, Medium, High, Critical)           │ │ │
│  │  │                                                          │ │ │
│  │  │ Interfaces:                                              │ │ │
│  │  │ • IRepository<T>, ITaskRepository, IReminderRepository │ │ │
│  │  │ • IReportRepository, IComplainRepository               │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                 Infrastructure Layer                          │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ DbContext:                                               │ │ │
│  │  │ • TaskManagementDbContext (EF Core)                     │ │ │
│  │  │ • DbSet<Task>, DbSet<Reminder>, DbSet<Report>, ...     │ │ │
│  │  │ • OnModelCreating (Fluent API config)                  │ │ │
│  │  │ • Soft delete filters                                   │ │ │
│  │  │ • Performance indices                                   │ │ │
│  │  │                                                          │ │ │
│  │  │ Repositories:                                            │ │ │
│  │  │ • Repository<T> (Generic base)                         │ │ │
│  │  │ • TaskRepository, ReminderRepository, etc.             │ │ │
│  │  │                                                          │ │ │
│  │  │ Migrations:                                              │ │ │
│  │  │ • Infrastructure/Migrations/                           │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                  SQL Server Database                          │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ TaskManagementBot                                        │ │ │
│  │  │ • Tasks table (CRUD operations)                        │ │ │
│  │  │ • Reminders table (Task reminders)                     │ │ │
│  │  │ • Reports table (Statistics & reports)                 │ │ │
│  │  │ • Complains table (Task modification requests)         │ │ │
│  │  │                                                          │ │ │
│  │  │ Indices:                                                 │ │ │
│  │  │ • Task: AssignedTo, CreatedBy, Status, DueDate         │ │ │
│  │  │ • Reminder: TaskId, IsSent                             │ │ │
│  │  │ • Report: ReportType                                    │ │ │
│  │  │ • Complain: TaskId, Status                             │ │ │
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Message Flow Diagram

```
┌─────────────┐
│ Mezon User  │
│  (Client)   │
└──────┬──────┘
       │ Sends Message
       │
       ▼
┌─────────────────────────────────────┐
│      Mezon Server (Cloud)           │
│  gw.mezon.ai:443 (WebSocket)        │
└──────┬──────────────────────────────┘
       │ Forwards to Bot
       │ ChannelMessage Event
       │
       ▼
┌──────────────────────────────────────────┐
│  MezonClient (BotService._client)        │
│  • Receives event                        │
│  • Parses ChannelMessage protobuf       │
│  • Triggers OnChannelMessage handler    │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  BotService.OnChannelMessage()           │
│  • Logs message details                  │
│  • Checks if it's a command (starts w/ !)│
│  • Passes to BotCommandHandlerExample    │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  BotCommandHandlerExample                │
│  • Parses command & arguments            │
│  • Routes to appropriate handler         │
│  • Calls Application Services            │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  TaskService / Other Application Service │
│  • Processes business logic              │
│  • Calls Repository methods              │
│  • Returns result                        │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Repository Pattern                      │
│  • TaskRepository.CreateAsync()          │
│  • Performs database operation           │
│  • Returns entity to service             │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Entity Framework Core                   │
│  • DbContext.Tasks.AddAsync()            │
│  • SaveChangesAsync()                    │
│  • Generates SQL & executes              │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  SQL Server Database                     │
│  (localdb)\mssqllocaldb / TaskManagementBot    │
│  • INSERT INTO Tasks (...)               │
│  • Returns generated ID                  │
└──────┬───────────────────────────────────┘
       │
       ▼ (Response flow back)
       
┌──────────────────────────────────────────┐
│  BotCommandHandler generates response    │
│  "✅ Task created successfully! ID: 123" │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Send response via message.ReplyAsync()  │
│  (Uses SocketManager internally)         │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│      Mezon Server                        │
│  Receives reply message                  │
└──────┬───────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────┐
│  Mezon User (Client)                     │
│  Receives bot's reply in chat            │
│  "✅ Task created successfully! ID: 123" │
└──────────────────────────────────────────┘
```

---

## Dependency Injection Structure

```
Program.cs (ConfigureServices)
    │
    ├─► IConfiguration
    │   └─ Loaded from appsettings.json
    │
    ├─► ILogger<T>
    │   └─ Console + Debug logging
    │
    ├─► IBotService → BotService
    │   │
    │   ├─ Depends on: ILogger<BotService>
    │   ├─ Depends on: IConfiguration
    │   └─ Creates: MezonClient (Mezon.Sdk)
    │
    ├─► ITaskService → TaskService
    │   ├─ Depends on: ITaskRepository
    │   └─ Depends on: ILogger<TaskService>
    │
    ├─► ITaskRepository → TaskRepository
    │   └─ Depends on: TaskManagementDbContext
    │
    ├─► IDbContext → TaskManagementDbContext
    │   ├─ Depends on: DbContextOptions<TaskManagementDbContext>
    │   └─ Configured for SQL Server LocalDB
    │
    └─► Similar chain for:
        • IReminderService → ReminderService
        • IReportService → ReportService
        • IComplainService → ComplainService
```

---

## Configuration Hierarchy

```
┌─────────────────────────────────────────┐
│   Configuration Loading Order           │
├─────────────────────────────────────────┤
│                                         │
│  1. appsettings.json (Base settings)   │
│     ├─ Logging configuration            │
│     ├─ Task settings (DefaultChannelId) │
│     └─ Default values for all env       │
│                                         │
│  2. appsettings.Development.json        │
│     ├─ Mezon.BotId                     │
│     ├─ Mezon.Token                     │
│     ├─ Mezon.Host                      │
│     ├─ Mezon.Port                      │
│     ├─ Mezon.UseSsl                    │
│     └─ Mezon.TimeoutMs                 │
│                                         │
│  3. Environment Variables               │
│     (Override any setting)              │
│                                         │
│  Result: IConfiguration object           │
│  ➜ Accessed via _configuration["Key"]   │
│                                         │
└─────────────────────────────────────────┘
```

---

## Event Subscription Pattern

```
BotService.SubscribeToEvents()
    │
    ├─► _client.ChannelMessage += OnChannelMessage
    │   │
    │   └─ When message arrives:
    │      OnChannelMessage(ChannelMessage msg)
    │      ├─ Log message details
    │      ├─ Check for commands
    │      └─ Call command handler
    │
    └─► _client.Ready += OnReady
        │
        └─ When bot is ready:
           OnReady()
           └─ Log "Bot is ready"
```

---

## Layered Architecture - Request Flow

```
                    HTTP Request / Mezon Message
                              │
                              ▼
                    ┌─────────────────────┐
                    │ Presentation Layer  │
                    │ (Controllers)       │
INBOUND             │ (MezonClient Events)│
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ Application Layer   │
                    │ (Services, DTOs)    │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ Domain Layer        │
                    │ (Entities, Rules)   │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ Infrastructure      │
                    │ (Database, EF Core) │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ SQL Server Database │
                    └─────────────────────┘

Response follows same path in reverse
```

---

## State Diagram: Bot Lifecycle

```
                 ┌─────────────────┐
                 │  Application    │
                 │  Startup        │
                 └────────┬────────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │ BotService      │
                 │ Created         │
                 │ (Not connected) │
                 └────────┬────────┘
                          │ StartAsync()
                          ▼
                 ┌─────────────────┐
                 │ MezonClient     │
                 │ Created         │
                 │ Configured      │
                 └────────┬────────┘
                          │ LoginAsync()
                          ▼
                 ┌─────────────────┐
                 │ Connecting to   │
                 │ Mezon Server    │
                 │ (WebSocket)     │
                 └────────┬────────┘
                          │ Success
                          ▼
                 ┌─────────────────┐
                 │ Ready Event     │
                 │ Triggered       │
                 │ Listening for   │
                 │ Messages        │
                 └────────┬────────┘
                          │ Message arrives
                          ▼
                 ┌─────────────────┐
                 │ ChannelMessage  │
                 │ Event Triggered │
                 │ Handler called  │
                 └────────┬────────┘
                          │ Ctrl+C pressed
                          ▼
                 ┌─────────────────┐
                 │ StopAsync()     │
                 │ Disconnect      │
                 │ Cleanup         │
                 └────────┬────────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │ Application     │
                 │ Shutdown        │
                 └─────────────────┘
```

---

## Files Reference

| Component | File | Layer |
|-----------|------|-------|
| Entry Point | Program.cs | Presentation |
| Bot Logic | BotService.cs | Presentation |
| Commands | BotCommandHandlerExample.cs | Application |
| Configuration | appsettings.Development.json | Config |
| Task Service | TaskService.cs | Application |
| Task Entity | Task.cs | Domain |
| Task Repository | TaskRepository.cs | Infrastructure |
| DbContext | TaskManagementDbContext.cs | Infrastructure |

---

**Architecture Status: ✅ Complete and Documented**
