# TaskManagement.Bot - Mezon Chatbot for Task Management

A feature-rich chatbot built on the **Mezon SDK** for task management and deadline tracking in chat environments.

## 📋 Project Overview

This project implements a **Task Management Chatbot** that integrates with the Mezon platform to help teams:
- **Create, update, and manage tasks** in chat
- **Set reminders** for deadlines
- **Track task status** (Todo → Doing → Done)
- **Organize tasks** by context (threads, channels, DMs)
- **Search and filter** tasks easily

## 🏗️ Project Structure

```
TaskManagement.Bot/
├── src/
│   └── TaskManagement.Bot/
│       ├── Commands/          # Chat command handlers
│       ├── Services/          # Business logic
│       ├── Persistence/       # Database repositories
│       ├── Models/            # Data entities & DTOs
│       ├── Utils/             # Helper utilities
│       ├── Events/            # Event handlers
│       ├── Properties/        # Project metadata
│       ├── appsettings.json   # Configuration
│       └── Program.cs         # Entry point
├── tests/
│   └── TaskManagement.Bot.Tests/  # Unit tests
├── docs/
│   ├── ARCHITECTURE.md        # System design
│   ├── SETUP.md              # Development setup
│   └── API.md                # API documentation
├── .github/
│   └── workflows/            # CI/CD pipelines
├── .gitignore
├── TaskManagement.Bot.sln
└── LICENSE
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 or higher
- Git
- Visual Studio 2022 or VS Code

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/TaskManagement.Bot.git
   cd TaskManagement.Bot
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   - Edit `src/TaskManagement.Bot/appsettings.json` with your Mezon credentials

4. **Run the project**
   ```bash
   dotnet run --project src/TaskManagement.Bot
   ```

5. **Run tests**
   ```bash
   dotnet test
   ```

## 📦 Features (4 Independent Modules)

### 1. **Task Management** (Feature 1)
- ✅ Create, update, delete tasks
- ✅ Change task status (todo/doing/done)
- **Owned by:** Team Member 1

### 2. **Task Listing & Search** (Feature 2)
- ✅ List all tasks
- ✅ Filter by status, assignee, deadline
- ✅ Full-text search
- ✅ Pagination & sorting
- **Owned by:** Team Member 2

### 3. **Reminders & Notifications** (Feature 3)
- ✅ Remind before deadline
- ✅ Notify when overdue
- ✅ Recurring reminders (daily, weekly)
- ✅ Snooze functionality
- **Owned by:** Team Member 3

### 4. **Thread-based Context** (Feature 4)
- ✅ Bind tasks to chat threads
- ✅ Context-aware reminders
- ✅ Message references
- ✅ Fallback to DM if thread deleted
- **Owned by:** Team Member 4

## 🛠️ Technology Stack

- **Language:** C# 9+
- **Framework:** ASP.NET Core 6/8
- **Database:** SQL Server / PostgreSQL (Entity Framework Core)
- **Testing:** xUnit, Moq, FluentAssertions
- **Logging:** Serilog
- **SDK:** Mezon.Sdk

## 📖 Documentation

- [Architecture](docs/ARCHITECTURE.md) - System design & patterns
- [Setup Guide](docs/SETUP.md) - Development environment
- [API Docs](docs/API.md) - Commands & endpoints

## 🔄 Git Workflow

### Branch Strategy
```
main (stable)
  ↑
develop (integration)
  ↑
feature/task-management (work in progress)
feature/task-search
feature/reminders
feature/thread-context
```

### Commit Convention
```
feat: add new feature
fix: fix bug
docs: documentation
test: add tests
chore: maintenance
```

## 🤝 Contributing

1. Create feature branch from `develop`
   ```bash
   git checkout -b feature/your-feature develop
   ```

2. Commit with clear messages
   ```bash
   git commit -m "feat: implement task creation"
   ```

3. Push and create Pull Request
   ```bash
   git push origin feature/your-feature
   ```

4. Team review & merge to `develop`

## 📝 License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

## 👥 Team

| Member | Feature |
|--------|---------|
| Person 1 | Task Management |
| Person 2 | Task Listing & Search |
| Person 3 | Reminders |
| Person 4 | Thread Context |

## 📞 Support

For issues or questions:
- Check [docs/SETUP.md](docs/SETUP.md) for troubleshooting
- Review existing issues on GitHub
- Contact team lead

---

**Built with ❤️ using Mezon SDK**
