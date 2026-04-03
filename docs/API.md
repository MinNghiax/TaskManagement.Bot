# Danh Sách Lệnh Bot

Tất cả lệnh bắt đầu bằng `/` trong chat.

---

## Feature 1: Quản Lý Task (Người 1)

### Tạo Task
```
/task create "Tiêu đề" --hạn ngày --gán @user
```

### Sửa Task
```
/task update [id] --tiêu-đề "New" --hạn "ngày mai"
```

### Xóa Task
```
/task delete [id]
```

### Đổi Trạng Thái
```
/task status [id] todo|doing|completed|late|delay
```

---

## Feature 2: Danh Sách & Tìm Kiếm (Người 2)

### Xem Danh Sách
```
/task list --trạng-thái todo --sắp-xếp deadline --trang 1
```

### Xem Chi Tiết
```
/task details [id]
```

### Tìm Kiếm
```
/task search "từ khóa"
```

### Lọc
```
/task filter
```

---

## Feature 3: Nhắc Nhở (Người 3)

### Đặt Nhắc
```
/reminder set [id] 1d --lặp never
```

### Xem Nhắc
```
/reminder list --task [id]
```

### Dời Nhắc
```
/reminder snooze [id] 1h
```

### Xóa Nhắc
```
/reminder delete [id]
```

---

## Feature 4: Task theo Thread (Người 4)

### Tạo Task trong Thread
```
/task create-here "Tiêu đề" --hạn "tuần sau"
```

### Xem Tasks của Thread
```
/task thread-list
```

### Gắn Task với Thread
```
/task bind [id]
```

---

**Chi tiết tại [KIEN-TRUC.md](KIEN-TRUC.md)**
