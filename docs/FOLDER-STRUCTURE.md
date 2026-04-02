# Cấu Trúc Thư Mục - Kiến Trúc Feature-Based

Tài liệu này mô tả cấu trúc dự án mới theo phương pháp **Feature-Based Architecture**.

**Lợi ích:**
- ✅ Mỗi người làm trên 1 folder riêng → không đụng nhau
- ✅ Code dễ đọc hơn (mỗi feature tự chứa đủ CMD, Service, Persistence, Models)
- ✅ Scale tốt hơn (thêm feature mới không ảnh hưởng cái cũ)
- ✅ Dễ test hơn (feature độc lập)
- ✅ Dễ review PR (PR liên quan đến 1 feature cụ thể)

---

## 📁 Cấu Trúc Dự Án Mới

```
TaskManagement.Bot/
│
├── Features/                               # Các tính năng (4 người, 4 features)
│   │
│   ├── Task/                               # Feature 1: Quản lý Task (CRUD)
│   │   ├── Commands/                       # Người 1: Xử lý lệnh input
│   │   │   ├── TaskCreateCommand.cs
│   │   │   ├── TaskUpdateCommand.cs
│   │   │   ├── TaskDeleteCommand.cs
│   │   │   └── TaskStatusCommand.cs
│   │   │
│   │   ├── Services/                       # Người 1: Business logic
│   │   │   └── ITaskService.cs
│   │   │
│   │   ├── Persistence/                    # Người 1: Database access
│   │   │   └── ITaskRepository.cs
│   │   │
│   │   └── Models/                         # Người 1: Entities & DTOs
│   │       ├── TaskEntity.cs
│   │       ├── CreateTaskDto.cs
│   │       └── UpdateTaskDto.cs
│   │
│   ├── TaskQuery/                          # Feature 2: Tìm kiếm Task (READ-ONLY)
│   │   ├── Commands/                       # Người 2: Xử lý lệnh tìm kiếm
│   │   │   ├── TaskListCommand.cs
│   │   │   ├── TaskDetailsCommand.cs
│   │   │   ├── TaskSearchCommand.cs
│   │   │   └── TaskFilterCommand.cs
│   │   │
│   │   ├── Services/                       # Người 2: Search logic
│   │   │   └── ITaskSearchService.cs
│   │   │
│   │   ├── Persistence/                    # Người 2: Optimized queries
│   │   │   └── ITaskQueryRepository.cs (extends IRepository)
│   │   │
│   │   └── Models/                         # Người 2: Search DTOs
│   │       ├── TaskSearchFilterDto.cs
│   │       └── TaskSearchResultDto.cs
│   │
│   ├── Reminder/                           # Feature 3: Nhắc nhở (Scheduling)
│   │   ├── Commands/                       # Người 3: Lệnh nhắc nhở
│   │   │   ├── ReminderSetCommand.cs
│   │   │   ├── ReminderListCommand.cs
│   │   │   ├── ReminderSnoozeCommand.cs
│   │   │   └── ReminderDeleteCommand.cs
│   │   │
│   │   ├── Services/                       # Người 3: Scheduling logic
│   │   │   └── IReminderService.cs
│   │   │
│   │   ├── Persistence/                    # Người 3: Database access
│   │   │   └── IReminderRepository.cs
│   │   │
│   │   └── Models/                         # Người 3: Entities & DTOs
│   │       ├── ReminderEntity.cs
│   │       └── CreateReminderDto.cs
│   │
│   └── ThreadContext/                      # Feature 4: Liên kết Thread (Message binding)
│       ├── Commands/                       # Người 4: Lệnh liên kết
│       │   ├── TaskCreateHereCommand.cs
│       │   ├── TaskThreadListCommand.cs
│       │   └── TaskBindCommand.cs
│       │
│       ├── Services/                       # Người 4: Business logic
│       │   └── IThreadContextService.cs
│       │
│       ├── Persistence/                    # Người 4: Database access
│       │   └── ITaskContextRepository.cs
│       │
│       └── Models/                         # Người 4: Entities & DTOs
│           ├── TaskContextEntity.cs
│           └── CreateTaskContextDto.cs
│
├── Shared/                                 # Các tiện ích dùng chung
│   │
│   ├── Utils/                              # Helpers & utilities (Team)
│   │   ├── ValidationHelper.cs             # Parse, validate input
│   │   ├── DateTimeHelper.cs               # Format & parse dates
│   │   └── MezonHelper.cs                  # Mezon SDK wrappers
│   │
│   ├── Models/                             # Base classes & interfaces (Team)
│   │   ├── BaseEntity.cs                   # Abstract base class
│   │   ├── IRepository.cs                  # Generic repository interface
│   │   ├── ICommand.cs                     # Command interface
│   │   └── IResult.cs                      # Standard result type
│   │
│   └── Events/                             # Event handlers (Team)
│       ├── EventHandler.cs                 # Base event handler
│       └── ReminderScheduler.cs            # Scheduled reminder trigger
│
├── bin/ (gitignore)                        # Build output
├── obj/ (gitignore)                        # Intermediate files
├── Properties/                             # Project metadata
├── Program.cs                              # Entry point & DI setup
├── TaskManagement.Bot.csproj               # Project file
│
├── tests/
│   └── TaskManagement.Bot.Tests/
│       ├── Features/
│       │   ├── Task/
│       │   │   └── TaskServiceTests.cs     # Người 1: Unit tests
│       │   ├── TaskQuery/
│       │   │   └── TaskSearchServiceTests.cs # Người 2: Unit tests
│       │   ├── Reminder/
│       │   │   └── ReminderServiceTests.cs # Người 3: Unit tests
│       │   └── ThreadContext/
│       │       └── ThreadContextServiceTests.cs # Người 4: Unit tests
│       └── TaskManagement.Bot.Tests.csproj
│
├── docs/
│   ├── README.md
│   ├── KIEN-TRUC.md
│   ├── HUONG-DAN.md
│   ├── API.md
│   └── FOLDER-STRUCTURE.md
│
├── .github/
│   └── workflows/
│       ├── build.yml
│       └── deploy.yml
│
├── .gitignore
├── LICENSE
└── TaskManagement.Bot.sln
```

---

## 👥 Phân Công Theo Người

### **Người 1: Task Management (Feature 1)**

**Folder:** `Features/Task/`

**Trách nhiệm:**
- Tạo, sửa, xóa task
- Thay đổi status task
- Persistence layer cho task

**Files cần làm:**
```
Features/Task/
├── Commands/
│   ├── TaskCreateCommand.cs      ✍️ Lệnh tạo task
│   ├── TaskUpdateCommand.cs      ✍️ Lệnh cập nhật
│   ├── TaskDeleteCommand.cs      ✍️ Lệnh xóa
│   └── TaskStatusCommand.cs      ✍️ Lệnh đổi status
│
├── Services/ITaskService.cs      ✍️ Service interface (already have sample)
├── Persistence/ITaskRepository.cs ✍️ Repo interface (already have sample)
└── Models/
    ├── TaskEntity.cs             ✍️ Entity (already have sample)
    └── CreateTaskDto.cs          ✍️ DTO (already have sample)

tests/TaskManagement.Bot.Tests/Features/Task/
└── TaskServiceTests.cs           ✍️ Unit tests
```

**Công việc cần hoàn thành:**
- [ ] Code implementation cho TaskCreateCommand
- [ ] Code implementation cho TaskUpdateCommand
- [ ] Code implementation cho TaskDeleteCommand
- [ ] Code implementation cho TaskStatusCommand
- [ ] Write unit tests

---

### **Người 2: Task Search (Feature 2)**

**Folder:** `Features/TaskQuery/`

**Trách nhiệm:**
- Tìm kiếm task
- Lọc task theo status, assignee, deadline
- Phân trang
- Hiển thị chi tiết

**Files cần làm:**
```
Features/TaskQuery/
├── Commands/
│   ├── TaskListCommand.cs        ✍️ Lệnh liệt kê (already have sample)
│   ├── TaskDetailsCommand.cs     ✍️ Lệnh xem chi tiết
│   ├── TaskSearchCommand.cs      ✍️ Lệnh tìm kiếm
│   └── TaskFilterCommand.cs      ✍️ Lệnh lọc
│
├── Services/ITaskSearchService.cs ✍️ Service (already have sample)
├── Persistence/ITaskQueryRepository.cs ✍️ Query repo (already have sample)
└── Models/
    └── TaskSearchFilterDto.cs    ✍️ DTO (already have sample)

tests/TaskManagement.Bot.Tests/Features/TaskQuery/
└── TaskSearchServiceTests.cs     ✍️ Unit tests
```

**Công việc cần hoàn thành:**
- [ ] Code cho TaskDetailsCommand
- [ ] Code cho TaskSearchCommand
- [ ] Code cho TaskFilterCommand
- [ ] Implement search logic trong ITaskSearchService
- [ ] Write unit tests

---

### **Người 3: Reminders (Feature 3)**

**Folder:** `Features/Reminder/`

**Trách nhiệm:**
- Tạo nhắc nhở
- Lên lịch nhắc nhở
- Kéo dài thời gian nhắc nhở (snooze)
- Xóa nhắc nhở
- Trigger nhắc nhở theo lịch

**Files cần làm:**
```
Features/Reminder/
├── Commands/
│   ├── ReminderSetCommand.cs     ✍️ Lệnh set reminder (already have sample)
│   ├── ReminderListCommand.cs    ✍️ Lệnh liệt kê
│   ├── ReminderSnoozeCommand.cs  ✍️ Lệnh snooze
│   └── ReminderDeleteCommand.cs  ✍️ Lệnh xóa
│
├── Services/IReminderService.cs  ✍️ Service (already have sample)
├── Persistence/IReminderRepository.cs ✍️ Repo (already have sample)
└── Models/
    └── ReminderEntity.cs         ✍️ Entity (already have sample)

Shared/Events/
└── ReminderScheduler.cs          ✍️ Scheduled task trigger (Người 3 chính)

tests/TaskManagement.Bot.Tests/Features/Reminder/
└── ReminderServiceTests.cs       ✍️ Unit tests
```

**Công việc cần hoàn thành:**
- [ ] Code cho ReminderListCommand
- [ ] Code cho ReminderSnoozeCommand
- [ ] Code cho ReminderDeleteCommand
- [ ] Implement ReminderScheduler (background job)
- [ ] Write unit tests

---

### **Người 4: Thread Context (Feature 4)**

**Folder:** `Features/ThreadContext/`

**Trách nhiệm:**
- Liên kết task với thread/channel
- Hiển thị danh sách task trong thread
- Gửi nhắc nhở đúng thread
- Lấy context từ message

**Files cần làm:**
```
Features/ThreadContext/
├── Commands/
│   ├── TaskCreateHereCommand.cs  ✍️ Lệnh tạo task trong thread (already have)
│   ├── TaskThreadListCommand.cs  ✍️ Lệnh liệt kê task trong thread
│   └── TaskBindCommand.cs        ✍️ Lệnh liên kết task với thread
│
├── Services/IThreadContextService.cs ✍️ Service (already have sample)
├── Persistence/ITaskContextRepository.cs ✍️ Repo (already have sample)
└── Models/
    └── TaskContextEntity.cs      ✍️ Entity (already have sample)

tests/TaskManagement.Bot.Tests/Features/ThreadContext/
└── ThreadContextServiceTests.cs  ✍️ Unit tests
```

**Công việc cần hoàn thành:**
- [ ] Code cho TaskThreadListCommand
- [ ] Code cho TaskBindCommand
- [ ] Implement context fetching logic
- [ ] Integration với Mezon SDK threading
- [ ] Write unit tests

---

## 🚀 Quy Trình Phát Triển Hàng Ngày

### **Bước 1: Tạo Feature Branch**
```bash
# Người 1
git checkout -b feature/task-management develop

# Người 2
git checkout -b feature/task-search develop

# Người 3
git checkout -b feature/reminders develop

# Người 4
git checkout -b feature/thread-context develop
```

### **Bước 2: Làm Việc Trên Files Của Mình**
```bash
# Người 1 - làm trên Features/Task/*
code Features/Task/Commands/TaskCreateCommand.cs
code Features/Task/Services/ITaskService.cs
code tests/Features/Task/TaskServiceTests.cs

# Người 2 - làm trên Features/TaskQuery/*
code Features/TaskQuery/Commands/TaskDetailsCommand.cs
code Features/TaskQuery/Services/ITaskSearchService.cs
code tests/Features/TaskQuery/TaskSearchServiceTests.cs

# Vân vân...
```

### **Bước 3: Commit**
```bash
# Follow conventional commits
git commit -m "feat(task): implement task creation command"
git commit -m "feat(task): add task update logic"
git commit -m "test(task): write unit tests for TaskService"
```

### **Bước 4: Push & Tạo PR**
```bash
git push origin feature/task-management
# Vào GitHub → tạo Pull Request → chọn base branch là `develop`
```

### **Bước 5: Review & Merge**
```bash
# Chờ review từ team member khác
# Sau khi approved, merge vào `develop`
```

### **Bước 6: Hợp nhất Vào Main**
```bash
# Khi ready release (tất cả 4 features done)
git checkout develop
git pull origin develop
git checkout main
git pull origin main
git merge develop
git tag v1.1.0
git push origin main --tags
```

---

## ✅ Checklist Khi Tạo File Mới

### **Nếu tạo Command (trong Features/*/Commands/)**
```
✓ Inherit từ ICommand interface
✓ Implement Execute() method
✓ Add meaningful error handling
✓ Register trong Program.cs (DI container)
✓ Write unit tests
✓ Add XML comments
✓ Add to feature's folder structure
```

### **Nếu tạo Service (trong Features/*/Services/)**
```
✓ Create interface (IFeatureService)
✓ Implement interface
✓ Inject repository
✓ Add validation logic
✓ Add error handling
✓ Write unit tests
✓ Register trong Program.cs
✓ Add XML comments
```

### **Nếu tạo Repository (trong Features/*/Persistence/)**
```
✓ Implement IRepository<T> hoặc extend nó
✓ Use in-memory list hoặc DbContext (when ready)
✓ Add CRUD methods
✓ Add feature-specific queries
✓ Write unit tests (mock DbContext)
✓ Register trong Program.cs
```

### **Nếu tạo Model (trong Features/*/Models/)**
```
✓ Entity: inherit từ BaseEntity
✓ DTO: add for API/command request
✓ Add XML comments
✓ Use nullable types (C# 9)
✓ Add enums if needed
```

### **Nếu tạo Shared code (trong Shared/)**
```
✓ IMPORTANT: Only add nếu dùng bởi 2+ features
✓ Don't duplicate code into Shared
✓ Add XML comments
✓ Make generic & reusable
✓ Write unit tests
```

---

## 🎯 Program.cs - DI Container

File `Program.cs` đã được setup với DI container cho cả 4 features:

```csharp
// Feature 1: Task Management (Người 1)
services.AddScoped<ITaskRepository, TaskRepository>();
services.AddScoped<ITaskService, TaskService>();
services.AddScoped<TaskCreateCommand>();

// Feature 2: Task Search (Người 2)
services.AddScoped<ITaskSearchService, TaskSearchService>();
services.AddScoped<TaskListCommand>();

// Feature 3: Reminders (Người 3)
services.AddScoped<IReminderRepository, ReminderRepository>();
services.AddScoped<IReminderService, ReminderService>();
services.AddScoped<ReminderSetCommand>();

// Feature 4: Thread Context (Người 4)
services.AddScoped<ITaskContextRepository, TaskContextRepository>();
services.AddScoped<IThreadContextService, ThreadContextService>();
services.AddScoped<TaskCreateHereCommand>();
```

**Quy tắc:**
- Khi tạo Service/Command/Repository PHẢI register trong Program.cs
- Registration order không quan trọng (DI tự resolve dependencies)
- Dùng `AddScoped` cho típ service (create new per request)

---

## 📊 Dependency Graph

```
Command
  ↓
Service (Inject Repository)
  ↓
Repository (Inject DbContext)
  ↓
Database
```

**Ví dụ:**
```
TaskCreateCommand
  ↓ (Inject)
ITaskService / TaskService
  ↓ (Inject)
ITaskRepository / TaskRepository
  ↓
Database
```

---

## 🔄 Tránh Xung Đột

### ✅ **Tốt - Tách biệt hoàn toàn**
```
Người 1: Features/Task/*
Người 2: Features/TaskQuery/*
Người 3: Features/Reminder/*
Người 4: Features/ThreadContext/*
```
→ Không ai đụng nhau, code review dễ

### ❌ **Tồi - Cùng sửa 1 file**
```
Người 1 & 2 cùng sửa Features/Task/Services/ITaskService.cs
```
→ Merge conflict, khó review, rủi ro lỗi

### ✅ **Cách xử lý nếu cần dùng chung**
```
A cần dùng method từ B
  ↓
B tạo public interface & implement
  ↓
A inject & dùng qua interface
```

---

## 📋 Các File Mẫu Đã Có

Những file sau đã được tạo làm template:

**Shared:**
- ✅ `Shared/Models/BaseEntity.cs` - Base class cho entities
- ✅ `Shared/Models/IRepository.cs` - Generic repository interface
- ✅ `Shared/Models/ICommand.cs` - Base command interface
- ✅ `Shared/Utils/ValidationHelper.cs` - Utilities
- ✅ `Shared/Utils/DateTimeHelper.cs` - Date utilities
- ✅ `Shared/Events/EventHandler.cs` - Base event handler

**Features:**
- ✅ `Features/Task/*` - Sample Task feature (có code template)
- ✅ `Features/TaskQuery/*` - Sample TaskQuery feature
- ✅ `Features/Reminder/*` - Sample Reminder feature
- ✅ `Features/ThreadContext/*` - Sample ThreadContext feature

**Công việc còn lại:** Mỗi người hoàn thành code logic còn thiếu trong feature của mình.

---

## 🎓 Tips & Best Practices

### **Namespace Convention**
```csharp
// Format: TaskManagement.Bot.Features.[FeatureName].[LayerName]
TaskManagement.Bot.Features.Task.Commands
TaskManagement.Bot.Features.Task.Services
TaskManagement.Bot.Features.Task.Persistence
TaskManagement.Bot.Features.Task.Models

// Shared
TaskManagement.Bot.Shared.Models
TaskManagement.Bot.Shared.Utils
TaskManagement.Bot.Shared.Events
```

### **File Naming**
```csharp
// Commands
TaskCreateCommand.cs
TaskUpdateCommand.cs

// Services
ITaskService.cs (interface)
TaskService.cs (implementation) - nếu có

// Repositories
ITaskRepository.cs (interface)
TaskRepository.cs (implementation) - nếu có

// Models
TaskEntity.cs (Entity)
CreateTaskDto.cs (DTO)
TaskStatus.cs (Enum)

// Tests
TaskServiceTests.cs
TaskRepositoryTests.cs
```

### **Quick Commands**
```bash
# Build project
dotnet build

# Run tests
dotnet test

# Add NuGet package
dotnet add package PackageName

# Create migration (when ready for DB)
dotnet ef migrations add MigrationName

# Format code
dotnet format
```

---

## 🏁 Summary

| Aspect | Chi Tiết |
|--------|----------|
| **Architecture** | Feature-Based (4 features, 4 người) |
| **Path per person** | `Features/[FeatureName]/*` |
| **Shared code** | `Shared/*` - dùng khi 2+ features cần |
| **Branching** | `feature/[feature-name]` - 1 branch per person |
| **Base classes** | `BaseEntity`, `IRepository<T>`, `ICommand` trong Shared |
| **DI Setup** | Tất cả service/command/repo phải register trong Program.cs |
| **Testing** | Mỗi feature có folder tests tương ứng |
| **Merge strategy** | Feature → develop → main (release only) |

---

**Các câu hỏi?** Xem [KIEN-TRUC.md](KIEN-TRUC.md) hoặc [HUONG-DAN.md](HUONG-DAN.md) 🚀
