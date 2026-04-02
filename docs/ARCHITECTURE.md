# Kiến Trúc TaskManagement.Bot

## Tổng Quan Hệ Thống

Dự án được xây dựng **modular** với 4 tính năng độc lập, mỗi tính năng được một người phụ trách.

```
┌──────────────────────────────────────────────┐
│    Mezon Client (WebSocket + REST API)       │
└────────────────┬─────────────────────────────┘
                 │
          ┌──────┴──────┐
          │  Handlers   │ (Parse commands)
          └──────┬──────┘
                 │
    ┌────────────┼────────────┬─────────────┬─────────────┐
    │            │            │             │             │
    ▼            ▼            ▼             ▼             ▼
┌────────┐ ┌─────────┐ ┌───────────┐ ┌────────────┐ ┌─────────┐
│Feature1│ │Feature2 │ │ Feature3  │ │ Feature4   │ │ Shared  │
│ Task   │ │ Search  │ │ Reminder  │ │ Thread     │ │ Models  │
│ CRUD   │ │ & Filter│ │ Scheduler │ │ Context    │ │ / DB    │
└────┬───┘ └────┬────┘ └─────┬─────┘ └─────┬──────┘ └────┬────┘
     │          │            │             │             │
     └──────────┴────────────┴─────────────┴─────────────┘
                       │
                       ▼
            ┌─────────────────────┐
            │  Entity Framework   │
            │   DbContext         │
            └──────────┬──────────┘
                       │
                       ▼
            ┌─────────────────────┐
            │   SQL Database      │
            │ (Server/PostgreSQL) │
            └─────────────────────┘
```

## Các Tầng Kiến Trúc

### 1. **Commands Layer** - Xử Lý Lệnh
- Parse thông điệp từ user
- Validate input
- Gọi Service phù hợp
- Format response

Ví dụ:
```
User: "/task create Mua hàng ngày Thứ 6"
    ↓
TaskCreateCommand → Parse input
    ↓
TaskService.CreateTask(dto)
    ↓
Response: "✅ Task tạo: Mua hàng - Hạn: Thứ 6"
```

### 2. **Services Layer** - Business Logic
Mỗi feature có service riêng:

**Feature 1: TaskService**
- Create, Update, Delete task
- Change status (todo → doing → done)

**Feature 2: TaskSearchService**
- List tasks với filters
- Get task details
- Search & pagination

**Feature 3: ReminderService**
- Create, Snooze, Delete reminders
- Schedule reminders
- Handle repeat patterns

**Feature 4: ThreadContextService**
- Bind task to thread
- List tasks by thread
- Send context-aware reminders

### 3. **Persistence Layer** - Truy Cập DB

```
IRepository<T>
  ├── CreateAsync(entity)
  ├── UpdateAsync(entity)
  ├── DeleteAsync(id)
  ├── GetByIdAsync(id)
  └── ListAsync(filters)
```

### 4. **Models** - Dữ Liệu Chia Sẻ

```
Models/
  ├── Task.cs           (Entity)
  ├── Reminder.cs       (Entity)
  └── TaskContext.cs    (Entity)
```

## Flow Ví Dụ: Tạo Task

```
1. User gửi: /task create "Mua sữa" --hạn ngày-mai --gán @john

2. TaskCreateCommand
   → Parse input
   → Lấy: title, deadline, assignee
   → Validate

3. TaskService.CreateTask(dto)
   → Check business rules
   → Check permissions
   → Create entity

4. Repository.CreateAsync(task)
   → Insert DB
   → Return entity

5. Response:
   "✅ Task tạo: Mua sữa
    Gán cho: @john
    Hạn: Ngày mai"
```

## Database Schema

```
Tasks
  ├── id
  ├── title
  ├── description
  ├── status (todo/doing/done)
  ├── assignee_id
  ├── deadline
  └── created_at

TaskContexts
  ├── task_id
  ├── thread_id
  ├── message_id
  └── channel_id

Reminders
  ├── id
  ├── task_id
  ├── remind_time
  ├── repeat_type
  └── is_active
```

## Design Patterns

**1. Repository Pattern** - Truy cập DB
**2. Dependency Injection** - DI container
**3. Command Handler Pattern** - Parse commands
**4. Event-Driven** - Trigger reminders

## Phụ Thuộc Giữa Features

```
Feature 1 (Quản lý task)
  ↓ Tạo/cập nhật task
  │
Feature 3 (Nhắc nhở) ← Đọc deadline
  ↓ Fire reminder
  │
Feature 4 (Thread) ← Gửi nhắc đúng thread
  │
Feature 2 (Tìm kiếm) ← Task xuất hiện kết quả
```

## Testing

```
Tests/
  ├── Feature1Tests/
  ├── Feature2Tests/
  ├── Feature3Tests/
  └── Feature4Tests/
```
