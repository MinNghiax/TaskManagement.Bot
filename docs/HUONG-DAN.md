# Hướng Dẫn Phát Triển - TaskManagement.Bot

## 📋 Yêu Cầu Hệ Thống

### Bắt buộc:
- `.NET 8.0` SDK hoặc cao hơn
- Visual Studio 2022 hoặc VS Code
- SQL Server (LocalDB hoặc Express)
- Git

### Optional:
- SQL Server Management Studio (SSMS)
- Postman/Insomnia (test API)

---

## 🚀 Setup Phát Triển

### Bước 1: Clone & Open Project

```bash
# Clone repository
git clone <repo-url>
cd TaskManagement.Bot

# Open trong VS Code
code .

# Hoặc VS 2022
start TaskManagement.Bot.sln
```

### Bước 2: Restore Dependencies

```bash
# Trong solution root hoặc src/TaskManagement.Bot folder
dotnet restore
```

### Bước 3: Install Entity Framework CLI

```bash
# Global tool để tạo migrations
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Bước 4: Cấu Hình Database

**Kiểm tra appsettings.json:**

```json
// src/TaskManagement.Bot/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementBot;Trusted_Connection=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Nếu cần thay đổi connection string:**

```json
// SQL Server locally
"DefaultConnection": "Server=.;Database=TaskManagementBot;Trusted_Connection=true;"

// SQL Server Express
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=TaskManagementBot;Trusted_Connection=true;"

// SQL Server với password
"DefaultConnection": "Server=localhost;Database=TaskManagementBot;User Id=sa;Password=YourPassword;"
```

### Bước 5: Code-First Database Creation

#### **5a. Tạo Initial Migration**

```bash
# Standart src/TaskManagement.Bot folder
cd src/TaskManagement.Bot

# Tạo migration
dotnet ef migrations add InitialCreate

# Xem file được tạo
ls Infrastructure/Migrations/
```

**Kết quả:** File migration `*_InitialCreate.cs` được tạo

#### **5b. Apply Migration -> Tạo Database**

```bash
# Create database từ migration
dotnet ef database update

# Nếu thành công, terminal sẽ in:
# info: Microsoft.EntityFrameworkCore.Database.Command[20101]
#   Executed DbCommand (50ms) [Parameters=[], CommandType='Text']
#   CREATE DATABASE [TaskManagementBot]
# ...
```

#### **5c. Verify Database**

**Option 1: Dùng SQL Server Object Explorer (VS 2022)**
```
View → SQL Server Object Explorer
┌─ SQL Server
   └─ (localdb)\mssqllocaldb
      └─ TaskManagementBot
         ├─ Tables
         │  ├─ Tasks
         │  ├─ Reminders  
         │  ├─ Reports
         │  └─ Complains
```

**Option 2: Dùng SSMS**
```sql
-- Kiểm tra databases
SELECT * FROM sys.databases WHERE name = 'TaskManagementBot';

-- Kiểm tra tables
USE TaskManagementBot;
SELECT * FROM INFORMATION_SCHEMA.TABLES;

-- Xem structure Task table
EXEC sp_help 'dbo.Tasks';
```

**Option 3: Dùng dotnet ef**
```bash
dotnet ef dbcontext info
```

### Bước 6: Build Project

```bash
cd src/TaskManagement.Bot

# Build
dotnet build

# Build và run
dotnet run
```

Expected output:
```
╔════════════════════════════════════════════╗
║  TaskManagement.Bot - Layered Architecture  ║
╚════════════════════════════════════════════╝

📚 Kiến Trúc (Architecture):
   ├─ Presentation Layer: Controllers (API endpoints)
   ├─ Application Layer: Services, DTOs
   ├─ Domain Layer: Entities, Enums, Interfaces
   └─ Infrastructure Layer: DbContext, Repositories

🎯 Các Tính Năng (Features):
   ✓ CRUD Task - Quản lý công việc
   ✓ Reminder - Nhắc nhở công việc
   ✓ Report - Báo cáo, thống kê
   ✓ Complain - Yêu cầu sửa chữa task

✅ TaskManagement.Bot - Sẵn sàng!
```

---

## 💻 Development Workflow

### Daily Workflow:

```bash
# 1. Get latest code
git pull origin develop

# 2. Create feature branch
git checkout -b feature/task-list

# 3. Build & verify no errors
dotnet build

# 4. Implement feature → code

# 5. If modify entities: Create migration
dotnet ef migrations add AddNewField

# 6. Apply migration
dotnet ef database update

# 7. Run unit tests
dotnet test

# 8. Commit with message
git add .
git commit -m "feat: implement task list functionality"

# 9. Push
git push origin feature/task-list

# 10. Create Pull Request on GitHub
```

---

## 📖 Add Entity & Database Changes

### Scenario: Thêm field `Priority` cho Task

**Bước 1: Update Entity**
```csharp
// Domain/Entities/Task.cs
public class Task : BaseEntity
{
    // ... existing fields
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium; // ← NEW
}
```

**Bước 2: Create Migration**
```bash
dotnet ef migrations add AddPriorityToTask
```

**Bước 3: Review Migration (optional)**
```csharp
// Infrastructure/Migrations/*_AddPriorityToTask.cs
public partial class AddPriorityToTask : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Priority",
            table: "Tasks",
            type: "int",
            nullable: false,
            defaultValue: 1); // Medium = 1
    }
}
```

**Bước 4: Apply Migration**
```bash
dotnet ef database update
```

**Bước 5: Update DTO (if needed)**
```csharp
// Application/DTOs/TaskDto.cs
public class TaskDto
{
    // ... existing
    public PriorityLevel Priority { get; set; } // ← NEW
}
```

**Bước 6: Update Service (if needed)**
```csharp
// Application/Services/TaskService.cs
private static TaskDto MapToDto(Task task)
{
    return new TaskDto
    {
        // ... existing mapping
        Priority = task.Priority // ← NEW
    };
}
```

### Scenario: Xóa Migration (nếu chưa apply)

```bash
# Undo last migration
dotnet ef migrations remove

# Verify deleted
ls Infrastructure/Migrations/
```

### Scenario: Rollback Database

```bash
# Rollback to previous migration
dotnet ef database update <PreviousMigrationName>

# Example
dotnet ef database update InitialCreate
```

### Full Example: Create New Entity

```bash
# 1. Create Entity file
# Domain/Entities/Task.cs (if doesn't exist)

# 2. Add DbSet to DbContext
# Infrastructure/DbContext/TaskManagementDbContext.cs
public DbSet<Task> Tasks { get; set; } = null!;

# 3. Create migration
dotnet ef migrations add AddTask

# 4. Review & apply
dotnet ef database update

# 5. Create Repository Interface & Class
# Domain/Interfaces/ITaskRepository.cs
# Infrastructure/Repositories/TaskRepository.cs

# 6. Create Service Interface & Class
# Application/Services/TaskService.cs

# 7. Create DTOs
# Application/DTOs/TaskDto.cs

# 8. Register DI in Program.cs
services.AddScoped<ITaskRepository, TaskRepository>();
services.AddScoped<ITaskService, TaskService>();

# 9. Create Controller
# Presentation/Controllers/TaskController.cs

# 10. Test
dotnet build
dotnet test
```

---

## 🧪 Unit Testing

### Chạy Tests:

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter TaskServiceTests

# Run with verbose output
dotnet test --verbosity detailed
```

### Test File Structure:

```
tests/TaskManagement.Bot.Tests/
├── Services/
│   ├── TaskServiceTests.cs
│   ├── ReminderServiceTests.cs
│   └── ...
├── Repositories/
│   ├── TaskRepositoryTests.cs
│   └── ...
└── TaskManagement.Bot.Tests.csproj
```

---

## 🐛 Troubleshooting

### Issue: "SQL Server connection failed"

**Solution 1: Verify LocalDB is installed**
```bash
sqllocaldb info
sqllocaldb start mssqllocaldb
```

**Solution 2: Change connection string**
```json
"DefaultConnection": "Server=.;Database=TaskManagementBot;Trusted_Connection=true;"
```

**Solution 3: Use SQL Server Express**
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=TaskManagementBot;Trusted_Connection=true;"
```

### Issue: "EF Core tools not found"

```bash
dotnet tool install --global dotnet-ef
```

### Issue: "Migration failed"

```bash
# Check status
dotnet ef migrations list

# Rollback & retry
dotnet ef database update <PreviousMigration>
dotnet ef migrations remove
dotnet ef migrations add <NewName>
dotnet ef database update
```

### Issue: "Changes not reflected in database"

```bash
# Make sure to apply migration
dotnet ef database update

# Verify
dotnet ef dbcontext info
```

---

## 📝 Code Standards

### Naming Conventions:

| Type | Example | Rule |
|------|---------|------|
| Class | `TaskService` | PascalCase |
| Interface | `ITaskService` | IPascalCase |
| Method | `GetTaskById` | PascalCase |
| Parameter | `taskId` | camelCase |
| Field | `_repository` | _camelCase (private) |
| Constant | `MAX_RETRY` | UPPER_CASE |

### File Naming:

```
✅ TaskDto.cs           (Entity-based)
✅ ITaskRepository.cs   (Interface)
✅ TaskRepository.cs    (Implementation)
✅ TaskService.cs       (Service)
✅ TaskController.cs    (Controller)

❌ task-dto.cs          (kebab-case)
❌ TaskResponseDTO.cs   (DTO suffix)
❌ class.cs             (generic name)
```

---

## 🔗 Useful Links

- **Entity Framework Core Docs:** https://docs.microsoft.com/en-us/ef/core/
- **Migrations Guide:** https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **Dependency Injection:** https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
- **Layered Architecture:** [ARCHITECTURE.md](ARCHITECTURE.md)

---

## ✅ Checklist - First Time Setup

```
[ ] .NET 8.0 SDK installed
[ ] Project cloned
[ ] dotnet restore completed
[ ] dotnet ef tool installed
[ ] appsettings.json configured
[ ] Initial migration created
[ ] Database updated (created)
[ ] Build successful
[ ] dotnet run works
[ ] Understand folder structure
```

---

**Bạn đã sẵn sàng!** 🎉

Tiếp theo:
1. Xem [ARCHITECTURE.md](ARCHITECTURE.md) để hiểu chi tiết kiến trúc
2. Implement Presentation Layer (Controllers)
3. Viết unit tests
4. Start development!

Có vấn đề? Xem [ARCHITECTURE.md](ARCHITECTURE.md) hoặc commit message examples trong Git.
