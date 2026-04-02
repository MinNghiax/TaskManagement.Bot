# Cấu Trúc Thư Mục & Nhiệm Vụ

Tài liệu này mô tả chi tiết chức năng của từng thư mục trong dự án TaskManagement.Bot.

---

## 📁 Cấu Trúc Dự Án

```
TaskManagement.Bot/
├── src/                                    # Source code chính
│   └── TaskManagement.Bot/
│       ├── Commands/                       # Feature 1-4: Command handlers
│       ├── Services/                       # Feature 1-4: Business logic
│       ├── Persistence/                    # Feature 1-4: Database access
│       ├── Models/                         # Shared: Data entities
│       ├── Utils/                          # Shared: Utilities & helpers
│       ├── Events/                         # Shared: Event handlers
│       ├── Properties/                     # Project metadata (.csproj properties)
│       ├── bin/ (gitignore)                # Build output
│       ├── obj/ (gitignore)                # Intermediate build files
│       ├── Program.cs                      # Entry point
│       └── TaskManagement.Bot.csproj       # Project file
├── tests/
│   └── TaskManagement.Bot.Tests/
│       ├── bin/ (gitignore)                # Test build output
│       ├── obj/ (gitignore)                # Test intermediate files
│       └── TaskManagement.Bot.Tests.csproj # Test project file
├── docs/                                   # Documentation
│   ├── README.md                           # Project overview
│   ├── KIEN-TRUC.md                        # Architecture & design patterns
│   ├── HUONG-DAN.md                        # Setup & development guide
│   ├── API.md                              # Bot commands & API
│   └── FOLDER-STRUCTURE.md                 # This file
├── .github/
│   └── workflows/                          # GitHub Actions CI/CD
│       ├── build.yml                       # Build & test pipeline
│       └── deploy.yml                      # Deployment pipeline
├── .gitignore                              # Git ignore rules
├── LICENSE                                 # MIT License
├── README.md                               # Project README
└── TaskManagement.Bot.sln                  # Visual Studio Solution
```

---

## 🎯 Chi Tiết Từng Thư Mục

### **src/TaskManagement.Bot/**
**Thư mục chính chứa toàn bộ source code của bot.**

#### **src/TaskManagement.Bot/Program.cs**
- **Chức năng:** Entry point của application
- **Nội dung:**
  - Khởi tạo DI container (Dependency Injection)
  - Setup configuration (appsettings.json)
  - Initialize logging
  - Register services
  - Launch bot
- **Ai sửa:** Team (khi setup framework)
- **Ví dụ:**
```csharp
var builder = Host.CreateDefaultBuilder(args);
builder.Services.AddScoped<ITaskService, TaskService>();
await builder.Build().RunAsync();
```

---

#### **src/TaskManagement.Bot/Commands/**
**Thư mục lệnh - Xử lý input từ user.**

- **Chức năng:**
  - Parse user commands từ chat
  - Validate input parameters
  - Route đến Services phù hợp
  - Format & gửi responses

- **Ai làm:**
  - Người 1: TaskCreateCommand, TaskUpdateCommand, TaskDeleteCommand, TaskStatusCommand
  - Người 2: TaskListCommand, TaskDetailsCommand, TaskSearchCommand, TaskFilterCommand
  - Người 3: ReminderSetCommand, ReminderListCommand, ReminderSnoozeCommand, ReminderDeleteCommand
  - Người 4: TaskCreateHereCommand, TaskThreadListCommand, TaskBindCommand

- **Ví dụ file:**
```
Commands/
  ├── TaskCreateCommand.cs       (Người 1)
  ├── TaskUpdateCommand.cs       (Người 1)
  ├── TaskDeleteCommand.cs       (Người 1)
  ├── TaskStatusCommand.cs       (Người 1)
  ├── TaskListCommand.cs         (Người 2)
  ├── TaskDetailsCommand.cs      (Người 2)
  ├── TaskSearchCommand.cs       (Người 2)
  ├── TaskFilterCommand.cs       (Người 2)
  ├── ReminderSetCommand.cs      (Người 3)
  ├── ReminderListCommand.cs     (Người 3)
  ├── ReminderSnoozeCommand.cs   (Người 3)
  ├── ReminderDeleteCommand.cs   (Người 3)
  ├── TaskCreateHereCommand.cs   (Người 4)
  ├── TaskThreadListCommand.cs   (Người 4)
  └── TaskBindCommand.cs         (Người 4)
```

- **Chuỗi xử lý:**
```
User message: "/task create Mua hàng"
    ↓
TaskCreateCommand.Execute()
    ↓
Parse: title="Mua hàng"
    ↓
Validate input
    ↓
Call TaskService.CreateAsync(dto)
    ↓
Return response
```

---

#### **src/TaskManagement.Bot/Services/**
**Thư mục business logic - Xử lý business rules.**

- **Chức năng:**
  - Implement feature logic
  - Validate business rules
  - Coordinate với Repositories
  - Handle errors & exceptions

- **Ai làm:**
  - Người 1: TaskService (CRUD, Status)
  - Người 2: TaskSearchService (List, Search, Filter)
  - Người 3: ReminderService (Create, Schedule, Snooze)
  - Người 4: ThreadContextService (Bind, Get, Send reminders)

- **Ví dụ:**
```
Services/
  ├── TaskService.cs             (Người 1)
  │   ├── CreateAsync()
  │   ├── UpdateAsync()
  │   ├── DeleteAsync()
  │   └── ChangeStatusAsync()
  ├── TaskSearchService.cs        (Người 2)
  │   ├── ListAsync()
  │   ├── GetDetailsAsync()
  │   └── SearchAsync()
  ├── ReminderService.cs          (Người 3)
  │   ├── CreateAsync()
  │   ├── SnoozeAsync()
  │   └── TriggerAsync()
  └── ThreadContextService.cs     (Người 4)
      ├── BindTaskToThreadAsync()
      └── GetContextualReminders()
```

- **Luồng gọi:**
```
Command
  ↓
Service (business logic)
  ↓
Repository (DB access)
  ↓
Database
```

---

#### **src/TaskManagement.Bot/Persistence/**
**Thư mục data access - Truy cập database.**

- **Chức năng:**
  - Implement Repository pattern
  - Query database
  - CRUD operations
  - Data mapping (Entity → DTO)

- **Ai làm:** Team (shared)

- **Ví dụ:**
```
Persistence/
  ├── IRepository.cs                         (Base interface)
  ├── TaskRepository.cs                      (Người 1 + 2 share)
  ├── ReminderRepository.cs                  (Người 3)
  └── TaskContextRepository.cs               (Người 4)
```

- **Giao diện chủ yếu:**
```csharp
public interface IRepository<T>
{
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<T> GetByIdAsync(int id);
    Task<List<T>> ListAsync(FilterDto filter);
}
```

---

#### **src/TaskManagement.Bot/Models/**
**Thư mục entities - Định nghĩa dữ liệu.**

- **Chức náng:**
  - Define data models (Entities)
  - Define DTOs (Data Transfer Objects)
  - Define enums & constants

- **Ai làm:** Team (shared)

- **Ví dụ:**
```
Models/
  ├── Task.cs                     (Người 1 + 2: Task entity)
  │   ├── Id
  │   ├── Title
  │   ├── Status (enum)
  │   ├── Deadline
  │   └── AssignedTo
  ├── Reminder.cs                 (Người 3: Reminder entity)
  │   ├── Id
  │   ├── TaskId (FK)
  │   ├── RemindTime
  │   └── RepeatType (enum)
  ├── TaskContext.cs              (Người 4: Thread context)
  │   ├── TaskId (FK)
  │   ├── ThreadId
  │   └── MessageId
  └── DTOs/
      ├── CreateTaskDto.cs        (Người 1)
      ├── TaskSearchFilterDto.cs  (Người 2)
      ├── ReminderRuleDto.cs      (Người 3)
      └── TaskContextDto.cs       (Người 4)
```

- **Phân biệt Entity vs DTO:**
```
Entity (Models)     DB representation
  ↓
Service             Business logic
  ↓
DTO                 API response
```

---

#### **src/TaskManagement.Bot/Utils/**
**Thư mục tiện ích - Hàm helper & utilities.**

- **Chức năng:**
  - Date/time helpers
  - String utilities
  - Validation helpers
  - Conversion helpers

- **Ai làm:** Team (shared)

- **Ví dụ:**
```
Utils/
  ├── DateTimeHelper.cs           (Parse/format dates)
  ├── StringHelper.cs             (String operations)
  ├── ValidationHelper.cs         (Input validation)
  └── MezonHelper.cs              (Mezon SDK wrappers)
```

---

#### **src/TaskManagement.Bot/Events/**
**Thư mục sự kiện - Event handlers.**

- **Chức năng:**
  - Handle Mezon events (message, reaction, etc.)
  - Trigger business logic based on events
  - Background jobs (reminders scheduler)

- **Ai làm:**
  - Người 3: ReminderScheduler (trigger reminders)
  - Team: Other event handlers

- **Ví dụ:**
```
Events/
  ├── MessageReceivedHandler.cs   (Parse commands)
  ├── ReminderScheduler.cs        (Người 3: Run scheduled reminders)
  └── TaskEventHandler.cs         (React to task events)
```

---

#### **src/TaskManagement.Bot/Properties/**
**Thư mục metadata - Thông tin project.**

- **Chức năng:**
  - Assembly info
  - Version info
  - Custom attributes

- **File:**
  - `AssemblyInfo.cs` (tự động)

---

#### **src/TaskManagement.Bot/bin/** & **obj/** (gitignore)
**Thư mục build output - TỰ ĐỘNG TẠO, KHÔNG commit.**

- **Chức năng:**
  - `bin/`: Compiled DLLs & executables
  - `obj/`: Temporary object files

- **Quy tắc:**
  - ✅ Nằm trong `.gitignore`
  - ✅ Tự động sinh khi `dotnet build`
  - ❌ Không bao giờ commit

---

#### **src/TaskManagement.Bot/TaskManagement.Bot.csproj**
**File cấu hình project.**

- **Chức năng:**
  - Define .NET version
  - List NuGet dependencies
  - Build settings
  - Output configuration

- **Không edit trực tiếp** - Dùng commands:
```powershell
# Thêm package
dotnet add package SomePackage

# Remove package
dotnet remove package SomePackage
```

---

### **tests/TaskManagement.Bot.Tests/**
**Test project - Unit tests cho từng feature.**

- **Chức năng:**
  - Write unit tests
  - Test services & logic
  - Mock dependencies
  - Assert expected behavior

- **Ai làm:**
  - Người 1: TaskServiceTests
  - Người 2: TaskSearchServiceTests
  - Người 3: ReminderServiceTests
  - Người 4: ThreadContextServiceTests

- **Ví dụ:**
```
tests/TaskManagement.Bot.Tests/
  ├── Services/
  │   ├── TaskServiceTests.cs         (Người 1)
  │   ├── TaskSearchServiceTests.cs   (Người 2)
  │   ├── ReminderServiceTests.cs     (Người 3)
  │   └── ThreadContextServiceTests.cs (Người 4)
  └── Mocks/
      ├── MockRepository.cs
      └── MockMezonClient.cs
```

- **Test structure:**
```csharp
[Fact]
public async Task CreateTask_WithValidData_ReturnsTask()
{
    // Arrange
    var dto = new CreateTaskDto { Title = "Test" };
    
    // Act
    var result = await _service.CreateAsync(dto);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Title);
}
```

---

### **docs/**
**Tài liệu dự án.**

| File | Nội Dung |
|------|----------|
| **README.md** | Overview & quick start |
| **KIEN-TRUC.md** | Architecture & system design |
| **HUONG-DAN.md** | Setup environment & development |
| **API.md** | Bot commands & endpoints |
| **FOLDER-STRUCTURE.md** | This file - Cấu trúc thư mục |

---

### **.github/workflows/**
**CI/CD pipelines - Tự động build & test.**

| File | Chức Năng |
|------|-----------|
| **build.yml** | Validate structure, build, run tests |
| **deploy.yml** | Deploy to production (disabled for now) |

---

## 📊 Nhiệm Vụ Theo Người

### **Người 1 - Task Management (Feature 1)**
```
Commands/TaskCreateCommand.cs
Commands/TaskUpdateCommand.cs
Commands/TaskDeleteCommand.cs
Commands/TaskStatusCommand.cs
Services/TaskService.cs
Models/Task.cs
Models/CreateTaskDto.cs
Models/UpdateTaskDto.cs
tests/TaskServiceTests.cs
```

---

### **Người 2 - Task Search (Feature 2)**
```
Commands/TaskListCommand.cs
Commands/TaskDetailsCommand.cs
Commands/TaskSearchCommand.cs
Commands/TaskFilterCommand.cs
Services/TaskSearchService.cs
Models/TaskSearchFilterDto.cs
Persistence/TaskRepository.cs (share with Người 1)
tests/TaskSearchServiceTests.cs
```

---

### **Người 3 - Reminders (Feature 3)**
```
Commands/ReminderSetCommand.cs
Commands/ReminderListCommand.cs
Commands/ReminderSnoozeCommand.cs
Commands/ReminderDeleteCommand.cs
Services/ReminderService.cs
Events/ReminderScheduler.cs
Models/Reminder.cs
Models/ReminderRuleDto.cs
Persistence/ReminderRepository.cs
tests/ReminderServiceTests.cs
```

---

### **Người 4 - Thread Context (Feature 4)**
```
Commands/TaskCreateHereCommand.cs
Commands/TaskThreadListCommand.cs
Commands/TaskBindCommand.cs
Services/ThreadContextService.cs
Models/TaskContext.cs
Models/TaskContextDto.cs
Persistence/TaskContextRepository.cs
tests/ThreadContextServiceTests.cs
```

---

## 🔄 Workflow Hàng Ngày

```
1. Feature Branch
   git checkout -b feature/task-creation

2. Sửa Code
   - Commands/TaskCreateCommand.cs
   - Services/TaskService.cs
   - Models/CreateTaskDto.cs
   - tests/TaskServiceTests.cs

3. Commit
   git commit -m "feat(task): implement creation"

4. Push & PR
   git push origin feature/task-creation
   [Create Pull Request on GitHub]

5. Review & Merge
   [Team review → Approve → Merge to develop]
```

---

## ✅ Checklist Khi Tạo File Mới

```
Nếu tạo trong Commands/:
  ✓ Inherit từ ICommand interface
  ✓ Implement Execute() method
  ✓ Add unit test trong tests/
  ✓ Register trong DI container (Program.cs)

Nếu tạo trong Services/:
  ✓ Create interface (IService)
  ✓ Inject Repository
  ✓ Add validation logic
  ✓ Add error handling

Nếu tạo trong Models/:
  ✓ Add XML comments
  ✓ Use nullable classes (C# 9)
  ✓ Create corresponding DTO

Nếu tạo trong Persistence/:
  ✓ Implement IRepository<T>
  ✓ Use EF Core DbContext
  ✓ Add filtering & pagination
```

---

## 🎯 Tóm Tắt

| Thư Mục | Chức Năng | Ai Làm |
|---------|-----------|--------|
| **Commands/** | Parse commands | Từng người (feature riêng) |
| **Services/** | Business logic | Từng người (feature riêng) |
| **Persistence/** | DB access | Team (shared) |
| **Models/** | Entities & DTOs | Team (shared) |
| **Utils/** | Helpers | Team (shared) |
| **Events/** | Event handlers | Team (shared) |
| **tests/** | Unit tests | Từng người (feature riêng) |
| **docs/** | Documentation | Team (curated) |

---

**Các câu hỏi?** Xem chi tiết tại [KIEN-TRUC.md](KIEN-TRUC.md) hoặc [HUONG-DAN.md](HUONG-DAN.md) 🚀
