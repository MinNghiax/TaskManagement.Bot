# Cấu Trúc Thư Mục & Hướng Dẫn Phát Triển

Dự án TaskManagement.Bot sử dụng **Layered Architecture** - Kiến trúc phân tầng theo các lớp kỹ thuật.

---

## 📁 Cấu Trúc Chính

```
TaskManagement.Bot/
│
├── src/TaskManagement.Bot/              # Source code chính
│   │
│   ├── Presentation/                    # TẦNG TRÌNH BÀY
│   │   └── Controllers/
│   │       ├── TaskController.cs        (TODO: Implement)
│   │       ├── ReminderController.cs    (TODO: Implement)
│   │       ├── ReportController.cs      (TODO: Implement)
│   │       └── ComplainController.cs    (TODO: Implement)
│   │
│   ├── Application/                     # TẦNG ỨNG DỤNG
│   │   ├── Services/
│   │   │   ├── TaskService.cs           ✅ (Có sample code)
│   │   │   ├── ReminderService.cs       ✅ (Có sample code)
│   │   │   ├── ReportService.cs         ✅ (Có sample code)
│   │   │   └── ComplainService.cs       ✅ (Có sample code)
│   │   ├── DTOs/
│   │   │   ├── TaskDto.cs               ✅
│   │   │   ├── ReminderDto.cs           ✅
│   │   │   ├── ReportDto.cs             ✅
│   │   │   └── ComplainDto.cs           ✅
│   │   └── Interfaces/
│   │
│   ├── Domain/                          # TẦNG MIỀN
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs            ✅ Base class
│   │   │   ├── Task.cs                  ✅ Task entity
│   │   │   ├── Reminder.cs              ✅ Reminder entity
│   │   │   ├── Report.cs                ✅ Report entity
│   │   │   └── Complain.cs              ✅ Complain entity
│   │   ├── Enums/
│   │   │   ├── TaskStatus.cs            ✅
│   │   │   ├── ComplainStatus.cs        ✅
│   │   │   └── PriorityLevel.cs         ✅
│   │   └── Interfaces/
│   │       ├── IRepository.cs           ✅
│   │       ├── ITaskRepository.cs       ✅
│   │       ├── IReminderRepository.cs   ✅
│   │       ├── IReportRepository.cs     ✅
│   │       └── IComplainRepository.cs   ✅
│   │
│   ├── Infrastructure/                  # TẦNG HẠ TẦNG
│   │   ├── DbContext/
│   │   │   └── TaskManagementDbContext.cs ✅ Entity Framework
│   │   ├── Repositories/
│   │   │   ├── Repository.cs            ✅ Generic base
│   │   │   ├── TaskRepository.cs        ✅
│   │   │   ├── ReminderRepository.cs    ✅
│   │   │   ├── ReportRepository.cs      ✅
│   │   │   └── ComplainRepository.cs    ✅
│   │   └── Migrations/                  (Auto-generated)
│   │
│   ├── Properties/
│   ├── Program.cs                       ✅ Entry point + DI
│   ├── appsettings.json                 ✅ Database config
│   └── TaskManagement.Bot.csproj
│
├── tests/TaskManagement.Bot.Tests/      # Unit tests
│
├── docs/                                # Documentation
│   ├── README.md
│   ├── ARCHITECTURE.md                  ✅ Kiến trúc chi tiết
│   ├── HUONG-DAN.md                     (Bạn sẽ update)
│   ├── API.md
│   └── FOLDER-STRUCTURE.md              This file
│
├── .github/workflows/                   # CI/CD
├── .gitignore
├── LICENSE
└── TaskManagement.Bot.sln
```

---

## 🎯 Phân Công Công Việc

### **Tiến Trình Hiện Tại:**

| Phần | Trạng Thái | Ghi Chú |
|------|-----------|--------|
| ✅ Domain Layer | Hoàn thành | Entities, Enums, Interfaces |
| ✅ Infrastructure Layer | Hoàn thành | DbContext, Repositories |
| ✅ Application Layer | Hoàn thành | Services, DTOs |
| ⏳ **Presentation Layer** | **TODO** | Implement controllers |

### **Công Việc Cần Làm:**

#### **1. Presentation Layer - Implement Controllers**

**TaskController (`Presentation/Controllers/TaskController.cs`)**
```
POST /api/tasks                 → CreateAsync()
GET /api/tasks/{id}             → GetByIdAsync()
GET /api/tasks                  → GetAllAsync()
PUT /api/tasks/{id}             → UpdateAsync()
DELETE /api/tasks/{id}          → DeleteAsync()
GET /api/tasks/assignee/{name}  → GetByAssigneeAsync()
GET /api/tasks/status/{status}  → GetByStatusAsync()
GET /api/tasks/overdue          → GetOverdueAsync()
PATCH /api/tasks/{id}/status    → ChangeStatusAsync()
PATCH /api/tasks/{id}/assign    → AssignAsync()
```

**ReminderController** - Tương tự
**ReportController** - Tương tự
**ComplainController** - Tương tự

#### **2. Database Setup**

```bash
# Step 1: Update appsettings.json connection string
nano appsettings.json

# Step 2: Install Entity Framework CLI (nếu chưa có)
dotnet tool install --global dotnet-ef

# Step 3: Tạo initial migration
dotnet ef migrations add InitialCreate

# Step 4: Apply migration -> Tạo database
dotnet ef database update

# Step 5: Verify trong SQL Server Management Studio
```

#### **3. Unit Tests**

```
tests/TaskManagement.Bot.Tests/
├── Services/
│   ├── TaskServiceTests.cs
│   ├── ReminderServiceTests.cs
│   ├── ReportServiceTests.cs
│   └── ComplainServiceTests.cs
└── Repositories/
```

---

## 🚀 Quick Start - Phát Triển Feature Mới

### **Ví dụ: Thêm tính năng mới "Priority Filter"**

**Bước 1: Kiểm tra Domain (nếu cần)**
```
✅ Task.cs đã có Priority field
✅ PriorityLevel enum đã có
```

**Bước 2: Thêm method trong Repository Interface**
```csharp
// Domain/Interfaces/ITaskRepository.cs
Task<IEnumerable<Task>> GetByPriorityAsync(PriorityLevel priority);
```

**Bước 3: Implement Repository**
```csharp
// Infrastructure/Repositories/TaskRepository.cs
public async Task<IEnumerable<Task>> GetByPriorityAsync(PriorityLevel priority)
{
    return await _dbSet
        .Where(t => t.Priority == priority)
        .OrderBy(t => t.DueDate)
        .ToListAsync();
}
```

**Bước 4: Thêm method trong Service Interface**
```csharp
// Application/Services/TaskService.cs
public async Task<IEnumerable<TaskDto>> GetByPriorityAsync(PriorityLevel priority)
{
    var tasks = await _repository.GetByPriorityAsync(priority);
    return tasks.Select(MapToDto);
}
```

**Bước 5: Implement Service**
```csharp
// (Implement method)
```

**Bước 6: Thêm Controller Endpoint**
```csharp
// Presentation/Controllers/TaskController.cs
[HttpGet("priority/{priority}")]
public async Task<ActionResult<IEnumerable<TaskDto>>> GetByPriority(PriorityLevel priority)
{
    var tasks = await _taskService.GetByPriorityAsync(priority);
    return Ok(tasks);
}
```

**Bước 7: Test**
```bash
dotnet test
```

---

## 📐 Dependency Injection - Cách thêm Service mới

**Trong Program.cs:**

```csharp
// Thêm repository
services.AddScoped<IMyRepository, MyRepository>();

// Thêm service
services.AddScoped<IMyService, MyService>();
```

**Sử dụng trong Controller:**
```csharp
public class MyController
{
    private readonly IMyService _service;
    
    public MyController(IMyService service)
    {
        _service = service; // Tự động inject bởi DI container
    }
}
```

---

## 💾 Entity Framework Migrations

### **Workflow:**

```bash
# Sau khi thay đổi entities
dotnet ef migrations add <MigrationName>

# Review file migration được create
ls Infrastructure/Migrations/

# Apply migration xuống database
dotnet ef database update

# Nếu muốn rollback
dotnet ef database update <PreviousMigrationName>

# Xóa migration (nếu chưa apply)
dotnet ef migrations remove
```

### **Ví dụ:**
```bash
dotnet ef migrations add AddTaskPriorityField
dotnet ef database update
```

---

## 🔗 Quan Hệ Entities

### **Task ← Reminder**
```
Task (1 -- Many) Reminder
one task có many reminders
```

**Cấu hình trong DbContext:**
```csharp
entity.HasMany(e => e.Reminders)
    .WithOne(r => r.Task)
    .HasForeignKey(r => r.TaskId)
    .OnDelete(DeleteBehavior.Cascade);
```

### **Task ← Complain**
```
Task (1 -- Many) Complain
one task có many complains
```

### **Soft Delete**
Tất cả entities có `IsDeleted` field:
```csharp
entity.HasQueryFilter(e => !e.IsDeleted); // Tự động loại trừ deleted records
```

---

## ✅ Checklist - Trước Khi Commit

```
[ ] Código compile không error
[ ] Entities định nghĩa đúng (Domain)
[ ] Repository implement đư đủ logic (Infrastructure)
[ ] Service handle business logic (Application)
[ ] DTO mapping đúng (Application)
[ ] Controller endpoints có (Presentation)
[ ] DI registered trong Program.cs
[ ] Unit tests viết & pass
[ ] Migration created & applied
[ ] Git commit với conventional message
```

---

## 📚 Tài Liệu Liên Quan

- **ARCHITECTURE.md** - Chi tiết kiến trúc layered
- **HUONG-DAN.md** - Setup development environment
- **API.md** - Tài liệu API endpoints

---

**Tiếp theo:** Xem [HUONG-DAN.md](HUONG-DAN.md) để setup database & development environment.
