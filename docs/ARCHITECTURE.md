# Kiến Trúc TaskManagement.Bot - Layered Architecture

## 📖 Tổng Quan

Dự án TaskManagement.Bot sử dụng **Layered Architecture** (kiến trúc phân tầng) - một kiến trúc phổ biến, dễ hiểu và dễ bảo trì. Kiến trúc này chia hệ thống thành **4 tầng độc lập**:

1. **Presentation Layer** - Xử lý giao diện & nhất liệu
2. **Application Layer** - Xử lý business logic
3. **Domain Layer** - Các entities & interfaces
4. **Infrastructure Layer** - Truy cập dữ liệu & cấu hình

---

## 🏗️ Cấu Trúc Dự Án

```
src/TaskManagement.Bot/
│
├── Presentation/                        # Tầng trình bày (API, Controllers)
│   └── Controllers/
│       ├── TaskController
│       ├── ReminderController
│       ├── ReportController
│       └── ComplainController
│
├── Application/                         # Tầng ứng dụng (Business Logic)
│   ├── Services/
│   │   ├── ITaskService.cs
│   │   ├── IReminderService.cs
│   │   ├── IReportService.cs
│   │   └── IComplainService.cs
│   ├── DTOs/                            # Data Transfer Objects
│   │   ├── TaskDto.cs
│   │   ├── ReminderDto.cs
│   │   ├── ReportDto.cs
│   │   └── ComplainDto.cs
│   └── Interfaces/
│
├── Domain/                              # Tầng miền (Entities, Interfaces)
│   ├── Entities/
│   │   ├── BaseEntity.cs                # Base entity cho tất cả entities
│   │   ├── Task.cs                      # Task entity
│   │   ├── Reminder.cs                  # Reminder entity
│   │   ├── Report.cs                    # Report entity
│   │   └── Complain.cs                  # Complain entity
│   ├── Enums/
│   │   ├── TaskStatus.cs                # ToDo, InProgress, Completed, Cancelled
│   │   ├── ComplainStatus.cs            # Pending, InProgress, Resolved, Rejected
│   │   └── PriorityLevel.cs             # Low, Medium, High, Critical
│   └── Interfaces/
│       ├── IRepository.cs               # Generic repository interface
│       ├── ITaskRepository.cs
│       ├── IReminderRepository.cs
│       ├── IReportRepository.cs
│       └── IComplainRepository.cs
│
├── Infrastructure/                      # Tầng hạ tầng (Database, Repositories)
│   ├── DbContext/
│   │   └── TaskManagementDbContext.cs   # Entity Framework DbContext
│   ├── Repositories/
│   │   ├── Repository.cs                # Generic repository (base)
│   │   ├── TaskRepository.cs
│   │   ├── ReminderRepository.cs
│   │   ├── ReportRepository.cs
│   │   └── ComplainRepository.cs
│   └── Migrations/                      # EF Core migrations folder
│
├── Program.cs                           # Entry point, DI configuration
├── appsettings.json                     # Configuration file
└── TaskManagement.Bot.csproj
```

---

## 🔄 Quy Trình Xử Lý Request (Closed Layer)

Request đi qua cả 4 tầng theo thứ tự:

```
Presentation (API Request)
    ↓
Application (Business Logic)
    ↓
Domain (Interface)
    ↓
Infrastructure (Database)
```

**Ví dụ: Tạo một Task**

```
1. POST /api/tasks (Presentation Layer)
   └─ TaskController.CreateAsync(CreateTaskDto)

2. Application Layer
   └─ TaskService.CreateAsync(CreateTaskDto)
       • Validate dữ liệu
       • Xử lý business logic
       • Gọi repository

3. Domain Layer
   └─ ITaskRepository interface

4. Infrastructure Layer
   └─ TaskRepository.AddAsync(Task)
       • Thêm entity vào DbContext
       • Lưu xuống database
       • Return created entity

5. Application Layer
   └─ Map Task → TaskDto

6. Presentation Layer
   └─ Return TaskDto (JSON response)
```

---

## 📚 Chi Tiết Từng Tầng

### 1️⃣ **Presentation Layer** (Tầng trình bày)

**Trách nhiệm:**
- Xử lý HTTP requests từ client
- Validate input từ user
- Gọi application services
- Return responses (JSON)

**Controllers hiện có (placeholder):**
- `TaskController` - Endpoints cho CRUD task
- `ReminderController` - Endpoints cho reminder
- `ReportController` - Endpoints cho report
- `ComplainController` - Endpoints cho complain

**TODO:** Implement chi tiết các endpoints

---

### 2️⃣ **Application Layer** (Tầng ứng dụng)

**Trách nhiệm:**
- Xử lý toàn bộ business logic
- Mapping entities ↔ DTOs
- Validate data
- Coordinate giữa các repositories

**Services:**
```
TaskService
├─ CreateAsync() - Tạo task
├─ UpdateAsync() - Cập nhật task
├─ DeleteAsync() - Xóa task
├─ ChangeStatusAsync() - Đổi trạng thái
├─ AssignAsync() - Giao task cho người khác
└─ GetByIdAsync(), GetAllAsync(), GetByAssigneeAsync(), ...

ReminderService
├─ CreateAsync() - Tạo nhắc nhở
├─ MarkAsSentAsync() - Đánh dấu đã gửi
└─ GetPendingAsync(), GetDueAsync(), ...

ReportService
├─ CreateAsync() - Tạo báo cáo
├─ GetByReportTypeAsync()
└─ GetByCreatorAsync()

ComplainService
├─ CreateAsync() - Tạo yêu cầu sửa chữa
├─ UpdateStatusAsync() - Cập nhật trạng thái
├─ AddSupportAsync() - Thêm support votes
└─ GetPendingAsync()
```

**DTOs (Data Transfer Objects):**
- Không expose domain entities trực tiếp
- Chỉ chứa dữ liệu cần thiết cho API
- Tách biệt database model từ API model

**Ví dụ:**
```csharp
// DTO (Application layer)
public class CreateTaskDto
{
    public string Title { get; set; }
    public string AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

// Entity (Domain layer)
public class Task : BaseEntity
{
    public string Title { get; set; }
    public string AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... thêm nhiều field khác
}
```

---

### 3️⃣ **Domain Layer** (Tầng miền)

**Trách nhiệm:**
- Định nghĩa entities (mô hình dữ liệu)
- Định nghĩa interfaces (contracts)
- Không phụ thuộc vào implementation details

**Entities:**

1. **Task** - Công việc cần làm
   ```
   • Title: string
   • Description: string
   • AssignedTo: string (người được giao)
   • DueDate: DateTime?
   • Status: TaskStatus (enum)
   • Priority: PriorityLevel (enum)
   • ChannelId: string (Mezon channel)
   • CreatedBy, CreatedAt, UpdatedAt, IsDeleted
   ```

2. **Reminder** - Nhắc nhở
   ```
   • TaskId: int (FK)
   • ReminderTime: DateTime
   • Message: string
   • IsSent: bool
   • SentAt: DateTime?
   • MezonUserId: string
   ```

3. **Report** - Báo cáo, thống kê
   ```
   • Title: string
   • ReportType: string
   • Content: string (JSON)
   • StartDate, EndDate: DateTime?
   • TotalTasks, CompletedTasks, InProgressTasks, ...
   • CreatedBy
   ```

4. **Complain** - Yêu cầu sửa chữa task
   ```
   • TaskId: int (FK)
   • Title: string
   • Content: string
   • ComplainType: string
   • Status: ComplainStatus (enum)
   • Response: string
   • RespondedBy, RespondedAt
   • SupportCount: int
   ```

**Enums:**
- `TaskStatus`: ToDo, InProgress, Completed, Cancelled
- `ComplainStatus`: Pending, InProgress, Resolved, Rejected
- `PriorityLevel`: Low, Medium, High, Critical

**Interfaces:**
```
IRepository<T> - Generic repository
├─ GetByIdAsync(id)
├─ GetAllAsync()
├─ AddAsync(entity)
├─ UpdateAsync(entity)
└─ DeleteAsync(id)

ITaskRepository : IRepository<Task>
├─ GetByAssigneeAsync(assignedTo)
├─ GetByStatusAsync(status)
└─ GetOverdueAsync()

IReminderRepository : IRepository<Reminder>
├─ GetByTaskIdAsync(taskId)
├─ GetDueAsync(beforeTime)
└─ GetByUserAsync(mezonUserId)

... (tương tự cho Report & Complain)
```

---

### 4️⃣ **Infrastructure Layer** (Tầng hạ tầng)

**Trách nhiệm:**
- Implement repository interfaces
- Quản lý database connection
- Thực hiện migrations
- Cấu hình Entity Framework

**DbContext:**
```csharp
TaskManagementDbContext
├─ DbSet<Task>
├─ DbSet<Reminder>
├─ DbSet<Report>
└─ DbSet<Complain>
```

**Features:**
- ✅ **Soft Delete**: Không xóa thực, chỉ set `IsDeleted = true`
- ✅ **Audit Fields**: `CreatedAt`, `UpdatedAt`
- ✅ **Relationships**: Task ← Reminder (One-to-Many)
- ✅ **Indices**: Tối ưu queries phổ biến

**Repositories:**
```
Repository<T> - Generic base class
├─ FindAsync(predicate)
├─ CountAsync()
└─ SaveChangesAsync()

TaskRepository : Repository<Task>
├─ GetByAssigneeAsync()
├─ GetOverdueAsync()
└─ GetByPriorityAsync()

... (tương tự cho Reminder, Report, Complain)
```

---

## 🔧 Dependency Injection (Program.cs)

**Setup trong Program.cs:**

```csharp
// Database
services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Repositories
services.AddScoped<ITaskRepository, TaskRepository>();
services.AddScoped<IReminderRepository, ReminderRepository>();
services.AddScoped<IReportRepository, ReportRepository>();
services.AddScoped<IComplainRepository, ComplainRepository>();

// Services
services.AddScoped<ITaskService, TaskService>();
services.AddScoped<IReminderService, ReminderService>();
services.AddScoped<IReportService, ReportService>();
services.AddScoped<IComplainService, ComplainService>();
```

**Lifetime Scopes:**
- `AddScoped`: Tạo instance mới cho mỗi HTTP request
- Phù hợp với web applications

---

## 💾 Code-First Database Setup

**Bước 1: Define entities** ✅ (Đã làm)

**Bước 2: Update appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementBot;Trusted_Connection=true;"
  }
}
```

**Bước 3: Tạo migration**
```bash
dotnet ef migrations add InitialCreate
```

**Bước 4: Apply migration**
```bash
dotnet ef database update
```

**Bước 5: Verify database**
- Mở SQL Server Object Explorer
- Kiểm tra database `TaskManagementBot`
- Verify tất cả tables được tạo

---

## 📊 Quan Hệ Entities

```
Task (1) ──────────────┬────────────── (Many) Reminder
        │              │
        │              └─── Người nhắc nhở (MezonUserId)
        │
        └─────────────────── (Many) Complain
                             Yêu cầu sửa chữa

Report - Độc lập (thống kê từ Tasks)
```

**Soft Delete:**
- Tất cả entities có `IsDeleted` field
- Query tự động loại trừ deleted records
- Có thể recover data nếu cần

---

## 🎯 Khi Nào Thêm Tính Năng Mới

1. **Tạo Entity** → Domain/Entities/
2. **Tạo Repository** → Domain/Interfaces/ & Infrastructure/Repositories/
3. **Tạo Service** → Application/Services/
4. **Tạo DTOs** → Application/DTOs/
5. **Tạo Controller** → Presentation/Controllers/
6. **Register DI** → Program.cs
7. **Tạo Migration** → `dotnet ef migrations add`
8. **Apply Migration** → `dotnet ef database update`

---

## ⚠️ Separation of Concern

**Nguyên tắc:**
```
Presentation: Chỉ xử lý HTTP
Application: Chỉ xử lý business logic
Domain: Chỉ định nghĩa models
Infrastructure: Chỉ tương tác DB
```

**❌ SAI - Presentation tương tác DB trực tiếp:**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult> Get(int id)
{
    var task = dbContext.Tasks.Find(id); // ❌ KHÔNG!
    return Ok(task);
}
```

**✅ ĐÚNG - Qua Service:**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<TaskDto>> Get(int id)
{
    var task = await _taskService.GetByIdAsync(id); // ✅ ĐÚNG!
    return Ok(task);
}
```

---

## 📚 Tài Liệu Tham Khảo

- Layered Architecture: https://viblo.asia/p/layered-architecture-kien-truc-quoc-dan-zXRJ8OPw4Gq
- Entity Framework Core: https://docs.microsoft.com/en-us/ef/core/
- Dependency Injection: https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

---

**Tiếp theo:** Xem [HUONG-DAN.md](HUONG-DAN.md) để setup development environment.
