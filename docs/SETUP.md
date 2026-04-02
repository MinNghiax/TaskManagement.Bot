# Hướng Dẫn Setup Phát Triển

## Yêu Cầu Hệ Thống

- **.NET SDK 8.0+**
- **Git**
- **Visual Studio 2022** hoặc **VS Code**
- **SQL Server** hoặc **PostgreSQL** (tuỳ chọn)

```powershell
dotnet --version
git --version
```

## Setup Dự Án

### 1. Clone Repository

```powershell
git clone https://github.com/your-username/TaskManagement.Bot.git
cd TaskManagement.Bot
```

### 2. Restore & Build

```powershell
dotnet restore
dotnet build
```

### 3. Thêm Mezon SDK (nếu cần)

```powershell
cd src/TaskManagement.Bot
dotnet add package Mezon.Sdk
```

## Database Setup (Tuỳ Chọn)

### SQL Server

```powershell
sqlcmd -S (localdb)\mssqllocaldb -Q "CREATE DATABASE TaskManagementBotDb"
```

### PostgreSQL

```bash
createdb taskmanagement_bot
```

### Tạo appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementBotDb;Trusted_Connection=true;"
  },
  "Mezon": {
    "BotToken": "your-token",
    "GuildId": "your-guild-id"
  }
}
```

## Chạy Ứng Dụng

```powershell
# Chạy project
dotnet run --project src/TaskManagement.Bot

# Hoặc chạy tests
dotnet test
```

## Git Workflow

### Tạo Feature Branch

```powershell
git checkout develop
git pull origin develop
git checkout -b feature/your-feature develop
```

### Commit & Push

```powershell
git add .
git commit -m "feat: implement feature"
git push origin feature/your-feature
```

### Pull Request

1. Vào GitHub → Pull Requests → New
2. Base: `develop`, Compare: `feature/your-feature`
3. Add description
4. Request review

## Debugging

### VS Code - launch.json

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/TaskManagement.Bot/bin/Debug/net8.0/TaskManagement.Bot.dll",
      "cwd": "${workspaceFolder}/src/TaskManagement.Bot"
    }
  ]
}
```

## Troubleshooting

```powershell
# Clear cache
dotnet nuget locals all --clear
dotnet restore

# Rebuild
dotnet clean
dotnet build

# Run tests
dotnet test
```

---

**Liên hệ team lead khi gặp vấn đề.**
