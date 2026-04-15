using Mezon.Sdk.Domain;

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