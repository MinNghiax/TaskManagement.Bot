using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public static class TaskFormBuilder
{
    public static ChannelMessageContent BuildTaskForm(List<string> teamMembers)
    {
        var interactive = new
        {
            title = "📝 Tạo Task mới",
            description = "Vui lòng điền thông tin task:",
            color = "#FEE75C",
            fields = new object[]
            {
                new
                {
                    name = "📌 Tiêu đề",
                    value = "",
                    inputs = new
                    {
                        id = "task_title",
                        type = 3,
                        component = new
                        {
                            id = "task_title_input",
                            placeholder = "Nhập tiêu đề task (tối đa 100 ký tự)",
                            defaultValue = "",
                            type = "text",
                            textarea = false,
                            maxLength = 100
                        }
                    }
                },
                new
                {
                    name = "📝 Mô tả",
                    value = "",
                    inputs = new
                    {
                        id = "task_description",
                        type = 3,
                        component = new
                        {
                            id = "task_description_input",
                            placeholder = "Nhập mô tả (không bắt buộc)",
                            defaultValue = "",
                            type = "text",
                            textarea = true
                        }
                    }
                },
                new
                {
                    name = "⚡ Độ ưu tiên",
                    value = "Chọn mức độ ưu tiên",
                    inputs = new
                    {
                        id = "task_priority",
                        type = 5,
                        component = new object[]
                        {
                            new { label = "🔴 Cao", value = "High" },
                            new { label = "🟡 Trung bình", value = "Medium" },
                            new { label = "🟢 Thấp", value = "Low" }
                        }
                    }
                },
                new
                {
                    name = "⏰ Deadline",
                    value = "",
                    inputs = new
                    {
                        id = "task_deadline",
                        type = 3,
                        component = new
                        {
                            id = "task_deadline_input",
                            placeholder = "YYYY-MM-DD HH:MM (ví dụ: 2024-12-31 23:59)",
                            defaultValue = "",
                            type = "text",
                            textarea = false
                        }
                    }
                },
                new
                {
                    name = "👤 Giao cho",
                    value = "Chọn người thực hiện",
                    inputs = new
                    {
                        id = "task_assignee",
                        type = 2,
                        component = new
                        {
                            placeholder = "Chọn thành viên",
                            options = teamMembers.Select(m => new
                            {
                                label = m.StartsWith("@") ? m : $"@{m}",
                                value = m
                            }).ToArray()
                        }
                    }
                }
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = "SUBMIT_TASK",
                            type = 1,
                            component = new
                            {
                                label = "✅ Tạo Task",
                                style = 3
                            }
                        },
                        new
                        {
                            id = "CANCEL_TASK",
                            type = 1,
                            component = new
                            {
                                label = "❌ Hủy",
                                style = 4
                            }
                        }
                    }
                }
            }
        };
    }

    // Form cập nhật task cho Mentor
    public static ChannelMessageContent BuildUpdateTaskFormForMentor(TaskDto task, List<string> teamMembers)
    {
        var interactive = new
        {
            title = $"✏️ Cập nhật Task #{task.Id}",
            description = "Vui lòng sửa thông tin task:",
            color = "#5865F2",
            fields = new object[]
            {
                new
                {
                    name = "📌 Tiêu đề",
                    value = task.Title,
                    inputs = new
                    {
                        id = "task_title",
                        type = 3,
                        component = new
                        {
                            id = "task_title_input",
                            placeholder = "Nhập tiêu đề task (tối đa 100 ký tự)",
                            defaultValue = task.Title,
                            type = "text",
                            textarea = false,
                            maxLength = 100
                        }
                    }
                },
                new
                {
                    name = "📝 Mô tả",
                    value = task.Description ?? "",
                    inputs = new
                    {
                        id = "task_description",
                        type = 3,
                        component = new
                        {
                            id = "task_description_input",
                            placeholder = "Nhập mô tả (không bắt buộc)",
                            defaultValue = task.Description ?? "",
                            type = "text",
                            textarea = true
                        }
                    }
                },
                new
                {
                    name = "⚡ Độ ưu tiên",
                    value = GetPriorityText(task.Priority),
                    inputs = new
                    {
                        id = "task_priority",
                        type = 5,
                        component = new object[]
                        {
                            new { label = "🔴 Cao", value = "High", @default = task.Priority == EPriorityLevel.High },
                            new { label = "🟡 Trung bình", value = "Medium", @default = task.Priority == EPriorityLevel.Medium },
                            new { label = "🟢 Thấp", value = "Low", @default = task.Priority == EPriorityLevel.Low }
                        }
                    }
                },
                new
                {
                    name = "📊 Trạng thái",
                    value = GetStatusText(task.Status),
                    inputs = new
                    {
                        id = "task_status",
                        type = 5,
                        component = new object[]
                        {
                            new { label = "📋 ToDo", value = "ToDo", @default = task.Status == ETaskStatus.ToDo },
                            new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                            new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review },
                            new { label = "✔️ Completed", value = "Completed", @default = task.Status == ETaskStatus.Completed },
                            new { label = "❌ Cancelled", value = "Cancelled", @default = task.Status == ETaskStatus.Cancelled }
                        }
                    }
                },
                new
                {
                    name = "⏰ Deadline",
                    value = task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    inputs = new
                    {
                        id = "task_deadline",
                        type = 3,
                        component = new
                        {
                            id = "task_deadline_input",
                            placeholder = "YYYY-MM-DD HH:MM",
                            defaultValue = task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                            type = "text",
                            textarea = false
                        }
                    }
                },
                new
                {
                    name = "👤 Giao cho",
                    value = task.AssignedTo ?? "",
                    inputs = new
                    {
                        id = "task_assignee",
                        type = 2,
                        component = new
                        {
                            placeholder = "Chọn thành viên",
                            options = teamMembers.Select(m => new
                            {
                                label = m.StartsWith("@") ? m : $"@{m}",
                                value = m,
                                @default = m == task.AssignedTo
                            }).ToArray()
                        }
                    }
                }
            },
            footer = new
            {
                text = $"Task ID: {task.Id} | Người tạo: {task.CreatedBy}"
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"UPDATE_TASK|{task.Id}",
                            type = 1,
                            component = new { label = "💾 Lưu cập nhật", style = 3 }
                        },
                        new
                        {
                            id = $"CANCEL_UPDATE|{task.Id}",
                            type = 1,
                            component = new { label = "❌ Hủy", style = 4 }
                        }
                    }
                }
            }
        };
    }

    // Form cập nhật trạng thái cho Member
    public static ChannelMessageContent BuildUpdateTaskStatusFormForMember(TaskDto task)
    {
        var interactive = new
        {
            title = $"✏️ Cập nhật trạng thái Task #{task.Id}",
            description = $"📌 {task.Title}",
            color = "#FEE75C",
            fields = new object[]
            {
                new
                {
                    name = "📊 Trạng thái mới",
                    value = $"Hiện tại: {GetStatusText(task.Status)}",
                    inputs = new
                    {
                        id = "task_status",
                        type = 5,
                        component = new object[]
                        {
                            new { label = "📋 ToDo", value = "ToDo", @default = task.Status == ETaskStatus.ToDo },
                            new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                            new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review },
                            new { label = "✔️ Completed", value = "Completed", @default = task.Status == ETaskStatus.Completed }
                        }
                    }
                }
            },
            footer = new
            {
                text = $"Task ID: {task.Id} | Giao cho: {task.AssignedTo}"
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"UPDATE_TASK_STATUS|{task.Id}",
                            type = 1,
                            component = new { label = "💾 Cập nhật", style = 3 }
                        },
                        new
                        {
                            id = $"CANCEL_UPDATE|{task.Id}",
                            type = 1,
                            component = new { label = "❌ Hủy", style = 4 }
                        }
                    }
                }
            }
        };
    }

    // Form danh sách task
    public static ChannelMessageContent BuildTaskListForm(List<TaskDto> tasks, string role, string filter = "")
    {
        var taskList = tasks.Count == 0
            ? "Không có task nào."
            : string.Join("\n\n", tasks.Select((t, i) =>
                $"{i + 1}. **{t.Title}**\n" +
                $"   🆔 ID: `{t.Id}`\n" +
                $"   📊 Trạng thái: {GetStatusText(t.Status)}\n" +
                $"   ⚡ Ưu tiên: {GetPriorityText(t.Priority)}\n" +
                $"   ⏰ Deadline: {t.DueDate:dd/MM/yyyy HH:mm}\n" +
                $"   👤 Giao cho: {t.AssignedTo}\n" +
                $"   👤 Người tạo: {t.CreatedBy}"));

        var interactive = new
        {
            title = $"📋 Danh sách Task ({tasks.Count})",
            description = role == "PM" ? "Với tư cách Mentor, bạn có thể quản lý tất cả task." : "Với tư cách Member, bạn chỉ thấy task của mình.",
            color = "#57F287",
            fields = new object[]
            {
                new
                {
                    name = "📝 Danh sách",
                    value = taskList,
                    inline = false
                }
            }
        };

        var components = new List<object>();

        // Thêm các nút lọc (chỉ cho Mentor)
        if (role == "PM")
        {
            components.Add(new
            {
                id = "FILTER_BY_STATUS",
                type = 1,
                component = new { label = "🔍 Lọc theo trạng thái", style = 2 }
            });
            components.Add(new
            {
                id = "FILTER_BY_DEADLINE",
                type = 1,
                component = new { label = "⏰ Lọc theo deadline", style = 2 }
            });
        }

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = components.ToArray()
                }
            }
        };
    }

    // Form xác nhận xóa task
    public static ChannelMessageContent BuildDeleteTaskConfirmForm(TaskDto task)
    {
        var interactive = new
        {
            title = $"⚠️ Xác nhận xóa Task",
            description = $"Bạn có chắc chắn muốn xóa task này?",
            color = "#ED4245",
            fields = new object[]
            {
                new
                {
                    name = "📌 Tiêu đề",
                    value = task.Title,
                    inline = false
                },
                new
                {
                    name = "🆔 ID",
                    value = task.Id.ToString(),
                    inline = true
                },
                new
                {
                    name = "👤 Người tạo",
                    value = task.CreatedBy,
                    inline = true
                }
            },
            footer = new
            {
                text = "Hành động này không thể hoàn tác!"
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"CONFIRM_DELETE_TASK|{task.Id}",
                            type = 1,
                            component = new { label = "✅ Xác nhận xóa", style = 4 }
                        },
                        new
                        {
                            id = $"CANCEL_DELETE|{task.Id}",
                            type = 1,
                            component = new { label = "❌ Hủy", style = 2 }
                        }
                    }
                }
            }
        };
    }

    private static string GetPriorityText(EPriorityLevel priority)
    {
        return priority switch
        {
            EPriorityLevel.High => "🔴 Cao",
            EPriorityLevel.Medium => "🟡 Trung bình",
            EPriorityLevel.Low => "🟢 Thấp",
            _ => "⚪ Không xác định"
        };
    }

    private static string GetStatusText(ETaskStatus status)
    {
        return status switch
        {
            ETaskStatus.ToDo => "📋 ToDo",
            ETaskStatus.Doing => "🔄 Doing",
            ETaskStatus.Review => "✅ Review",
            ETaskStatus.Completed => "✔️ Completed",
            ETaskStatus.Cancelled => "❌ Cancelled",
            _ => "❓ Unknown"
        };
    }


    public static (bool isValid, string message) ValidateTaskForm(string title, string deadline, string assignee)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "❌ Tiêu đề không được để trống");
        }

        if (title.Length > 100)
        {
            return (false, "❌ Tiêu đề tối đa 100 ký tự");
        }

        if (string.IsNullOrWhiteSpace(deadline))
        {
            return (false, "❌ Deadline không được để trống");
        }

        if (!DateTime.TryParse(deadline, out var deadlineDate))
        {
            return (false, "❌ Định dạng deadline không hợp lệ. Dùng: YYYY-MM-DD HH:MM");
        }

        if (deadlineDate <= DateTime.Now)
        {
            return (false, "❌ Deadline phải lớn hơn thời gian hiện tại");
        }

        if (string.IsNullOrWhiteSpace(assignee))
        {
            return (false, "❌ Vui lòng chọn người được giao");
        }

        return (true, "✅ Hợp lệ");
    }
}