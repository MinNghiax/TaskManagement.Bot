using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public static class TaskFormBuilder
{
    private const string EmptyOptionsPlaceholder = "[]";

    public static ChannelMessageContent BuildCreateForm(List<Project> projects)
    {
        var projectOptions = projects.Select(p => new { label = p.Name, value = p.Id.ToString() }).ToArray();

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task mới",
                    description = "Vui lòng điền thông tin task:",
                    color = "#FEE75C",
                    fields = BuildFormFields(
                        projectOptions: projectOptions,
                        teamOptions: Array.Empty<object>(),
                        assigneeOptions: Array.Empty<object>()
                    ),
                    footer = new { text = "Trạng thái mặc định: ToDo" }
                }
            },
            Components = BuildFormButtons("SELECT_PROJECT", "➡️ Tiếp tục")
        };
    }

    public static ChannelMessageContent BuildFullCreateForm(
    List<Project> projects,
    List<Team> teams,
    List<string> members,
    TaskReminderFieldState? reminderState = null)
    {
        reminderState ??= TaskReminderFieldState.Default();

        var projectOptions = projects.Select(p => new
        {
            label = p.Name,
            value = p.Id.ToString()
        }).ToArray();

        var teamOptions = teams.Select(t => new
        {
            label = t.Name,
            value = t.Id.ToString()
        }).ToArray();

        var memberOptions = members.Select(m => new
        {
            label = m,
            value = m
        }).ToArray();

        var fields = new List<object>
        {
            BuildSelectField("📁 Project", "task_project", "Chọn project", projectOptions),
            BuildSelectField("👥 Team", "task_team", "Chọn team", teamOptions),
            BuildSelectField("👤 Giao cho", "task_assignee", "Chọn thành viên", memberOptions),
            BuildTextField("📌 Tiêu đề", "task_title", "Nhập tiêu đề", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "task_description", "Nhập mô tả"),
            BuildTextField("⏰ Deadline", "task_deadline", "YYYY-MM-DD HH:MM"),
            BuildRadioField("⚡ Độ ưu tiên", "task_priority", new object[]
            {
                new { label = "🔴 Cao", value = "High" },
                new { label = "🟡 Trung bình", value = "Medium", @default = true },
                new { label = "🟢 Thấp", value = "Low" }
            })
        };

        fields.AddRange(BuildReminderFields(reminderState));

        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "📝 Tạo Task mới",
                color = "#FEE75C",
                fields = fields.ToArray(),
                footer = new { text = "Trạng thái mặc định: ToDo" }
            }
        },
            Components = BuildFormButtons("SUBMIT_TASK")
        };
    }

    public static ChannelMessageContent BuildCreateFormWithTeams(int projectId, List<Team> teams)
    {
        var teamOptions = teams.Select(t => new { label = t.Name, value = t.Id.ToString() }).ToArray();

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task mới",
                    description = "Đã chọn project. Vui lòng chọn team:",
                    color = "#FEE75C",
                    fields = BuildTeamOnlyFields(teamOptions),
                    footer = new { text = "Trạng thái mặc định: ToDo" }
                }
            },
            Components = BuildFormButtons($"SELECT_TEAM|{projectId}", "Tiếp tục")
        };
    }

    public static ChannelMessageContent BuildCreateFormWithMembers(int projectId, int teamId, List<string> members)
    {
        var assigneeOptions = members.Select(m => new { label = m, value = m }).ToArray();

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task mới",
                    description = "Vui lòng điền thông tin task:",
                    color = "#FEE75C",
                    fields = BuildDetailFields(assigneeOptions),
                    footer = new { text = "Trạng thái mặc định: ToDo" }
                }
            },
            Components = BuildFormButtons($"SUBMIT_TASK|{projectId}|{teamId}")
        };
    }

    public static ChannelMessageContent BuildUpdateFormForMentor(TaskDto task, List<string> members)
    {
        var memberOptions = members.Select(m => new
        {
            label = m,
            value = m,
            @default = m == task.AssignedTo
        }).ToArray();

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = $"✏️ Cập nhật Task #{task.Id}",
                    description = $"TeamId: {task.TeamId ?? 0}",
                    color = "#5865F2",
                    fields = BuildUpdateFields(task, memberOptions),
                    footer = new { text = $"Người tạo: {task.CreatedBy}" }
                }
            },
            Components = BuildFormButtons($"UPDATE_TASK|{task.Id}", "💾 Lưu", "CANCEL_UPDATE", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildUpdateFormForMember(TaskDto task)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = $"📊 Cập nhật trạng thái Task #{task.Id}",
                    description = $"📌 {task.Title}",
                    color = "#FEE75C",
                    fields = BuildStatusUpdateFields(task),
                    footer = new { text = $"Giao cho: {task.AssignedTo}" }
                }
            },
            Components = BuildFormButtons($"UPDATE_STATUS|{task.Id}", "💾 Cập nhật", "CANCEL_UPDATE", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildTaskList(List<TaskDto> tasks, bool isMentor, string? filterBy = null)
    {
        var description = isMentor
            ? "Với tư cách Mentor, bạn có thể quản lý tất cả task."
            : "Danh sách task được giao cho bạn.";

        var taskList = tasks.Count == 0
            ? "Không có task nào."
            : string.Join("\n\n", tasks.Select((t, i) =>
                $"**{i + 1}. {t.Title}**\n" +
                $"🆔 `{t.Id}` | {GetStatusText(t.Status)} | {GetPriorityText(t.Priority)}\n" +
                $"⏰ {t.DueDate:dd/MM/yyyy HH:mm} | 👤 {t.AssignedTo}"));

        var components = new List<object>();

        if (isMentor)
        {
            components.Add(new { id = "FILTER_STATUS", type = 1, component = new { label = "🔍 Lọc theo trạng thái", style = 2 } });
            components.Add(new { id = "FILTER_USER", type = 1, component = new { label = "👤 Lọc theo người", style = 2 } });
            components.Add(new { id = "FILTER_DEADLINE", type = 1, component = new { label = "⏰ Lọc theo deadline", style = 2 } });
        }

        components.Add(new { id = "CLOSE_LIST", type = 1, component = new { label = "❌ Đóng", style = 4 } });

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = $"📋 Danh sách Task ({tasks.Count})",
                    description = description,
                    color = "#57F287",
                    fields = new object[]
                    {
                        new { name = "📝 Tasks", value = taskList, inline = false }
                    }
                }
            },
            Components = new[] { new { components = components.ToArray() } }
        };
    }

    public static ChannelMessageContent BuildDeleteConfirm(TaskDto task)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "⚠️ Xác nhận xóa Task",
                    description = "Hành động này không thể hoàn tác!",
                    color = "#ED4245",
                    fields = new object[]
                    {
                        new { name = "📌 Tiêu đề", value = task.Title, inline = false },
                        new { name = "🆔 ID", value = task.Id.ToString(), inline = true },
                        new { name = "👤 Người tạo", value = task.CreatedBy, inline = true }
                    }
                }
            },
            Components = new[]
            {
                new
                {
                    components = new object[]
                    {
                        new { id = $"CONFIRM_DELETE|{task.Id}", type = 1, component = new { label = "✅ Xác nhận xóa", style = 4 } },
                        new { id = "CANCEL_DELETE", type = 1, component = new { label = "❌ Hủy", style = 2 } }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildTaskResult(TaskDto task)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "✅ Task đã được tạo!",
                    description = task.Title,
                    color = "#57F287",
                    fields = new object[]
                    {
                        new { name = "🆔 ID", value = task.Id.ToString(), inline = true },
                        new { name = "📊 Trạng thái", value = GetStatusText(task.Status), inline = true },
                        new { name = "⚡ Ưu tiên", value = GetPriorityText(task.Priority), inline = true },
                        new { name = "⏰ Deadline", value = task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Không có", inline = true },
                        new { name = "👤 Giao cho", value = task.AssignedTo ?? "Chưa giao", inline = true },
                        new { name = "👤 Người tạo", value = task.CreatedBy ?? "Unknown", inline = true }
                    },
                    footer = new { text = $"Tạo lúc: {task.CreatedAt:dd/MM/yyyy HH:mm}" }
                }
            },
            Components = Array.Empty<object>()
        };
    }


    private static object[] BuildFormFields(object[] projectOptions, object[] teamOptions, object[] assigneeOptions)
    {
        return new object[]
        {
            BuildSelectField("📁 Project", "task_project", "Chọn project", projectOptions),
            BuildSelectField("👥 Team", "task_team", "Chọn team", teamOptions),
            BuildSelectField("👤 Giao cho", "task_assignee", "Chọn thành viên", assigneeOptions),
            BuildTextField("📌 Tiêu đề", "task_title", "Nhập tiêu đề (tối đa 100 ký tự)", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "task_description", "Nhập mô tả (không bắt buộc)"),
            BuildTextField("⏰ Deadline", "task_deadline", "YYYY-MM-DD HH:MM"),
            BuildRadioField("⚡ Độ ưu tiên", "task_priority", new object[]
            {
                new { label = "🔴 Cao", value = "High" },
                new { label = "🟡 Trung bình", value = "Medium", @default = true },
                new { label = "🟢 Thấp", value = "Low" }
            })
        };
    }

    private static object[] BuildTeamOnlyFields(object[] teamOptions)
    {
        return new object[]
        {
            BuildSelectField("👥 Team", "task_team", "Chọn team", teamOptions)
        };
    }

    private static object[] BuildDetailFields(object[] assigneeOptions)
    {
        var fields = new List<object>
        {
            BuildTextField("📌 Tiêu đề", "task_title", "Nhập tiêu đề (tối đa 100 ký tự)", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "task_description", "Nhập mô tả (không bắt buộc)"),
            BuildRadioField("⚡ Độ ưu tiên", "task_priority", new object[]
            {
                new { label = "🔴 Cao", value = "High" },
                new { label = "🟡 Trung bình", value = "Medium", @default = true },
                new { label = "🟢 Thấp", value = "Low" }
            }),
            BuildTextField("⏰ Deadline", "task_deadline", "YYYY-MM-DD HH:MM"),
            BuildSelectField("👤 AssignedTo", "task_assignee", "Chọn thành viên", assigneeOptions)
        };

        fields.AddRange(BuildReminderFields(TaskReminderFieldState.Default()));

        return fields.ToArray();
    }

    private static object[] BuildUpdateFields(TaskDto task, object[] memberOptions)
    {
        var fields = new List<object>
        {
            BuildTextField("📌 Tiêu đề", "task_title", "Nhập tiêu đề", defaultValue: task.Title ?? "", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "task_description", "Nhập mô tả", defaultValue: task.Description ?? ""),
            BuildRadioField("⚡ Độ ưu tiên", "task_priority", new[]
            {
                new { label = "🔴 Cao", value = "High", @default = task.Priority == EPriorityLevel.High },
                new { label = "🟡 Trung bình", value = "Medium", @default = task.Priority == EPriorityLevel.Medium },
                new { label = "🟢 Thấp", value = "Low", @default = task.Priority == EPriorityLevel.Low }
            }),
            BuildRadioField("📊 Trạng thái", "task_status", new[]
            {
                new { label = "📋 ToDo", value = "ToDo", @default = task.Status == ETaskStatus.ToDo },
                new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review },
                new { label = "✔️ Completed", value = "Completed", @default = task.Status == ETaskStatus.Completed },
                new { label = "❌ Cancelled", value = "Cancelled", @default = task.Status == ETaskStatus.Cancelled }
            }),
            BuildTextField("⏰ Deadline", "task_deadline", "YYYY-MM-DD HH:MM", defaultValue: task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? ""),
            BuildSelectField("👤 AssignedTo", "task_assignee", "Chọn thành viên", memberOptions)
        };

        fields.AddRange(BuildReminderFields(TaskReminderFieldState.FromRules(task.ReminderRules)));

        return fields.ToArray();
    }

    private static object[] BuildStatusUpdateFields(TaskDto task)
    {
        return new object[]
        {
            BuildRadioField("📊 Trạng thái mới", "task_status", new[]
            {
                new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review }
            })
        };
    }

    private static object[] BuildReminderFields(TaskReminderFieldState state)
    {
        var disabled = !state.IsEnabled;

        return new object[]
        {
            BuildBooleanSelectField("🔔 Bật reminder", "task_reminder_enabled", state.IsEnabled),
            BuildNumberField("🔔 Trước deadline", "task_reminder_before_value", "Số thời gian", state.BeforeValue, disabled),
            BuildTimeUnitSelectField("Đơn vị trước deadline", "task_reminder_before_unit", state.BeforeUnit, disabled),
            BuildNumberField("🔔 Sau deadline", "task_reminder_after_value", "Số thời gian", state.AfterValue, disabled),
            BuildTimeUnitSelectField("Đơn vị sau deadline", "task_reminder_after_unit", state.AfterUnit, disabled),
            BuildBooleanSelectField("Lặp lại sau deadline", "task_reminder_after_repeat", state.IsAfterRepeatEnabled, disabled),
            BuildNumberField("🔁 Báo lặp", "task_reminder_repeat_value", "Số thời gian", state.RepeatValue, disabled),
            BuildTimeUnitSelectField("Đơn vị báo lặp", "task_reminder_repeat_unit", state.RepeatUnit, disabled)
        };
    }

    private static object BuildBooleanSelectField(string name, string id, bool selectedValue, bool disabled = false)
    {
        var options = new object[]
        {
            new { label = "Có", value = bool.TrueString },
            new { label = "Không", value = bool.FalseString }
        };

        var selected = selectedValue
            ? new { label = "Có", value = bool.TrueString }
            : new { label = "Không", value = bool.FalseString };

        return BuildSelectField(name, id, "Chọn", options, selected, disabled);
    }

    private static object BuildTimeUnitSelectField(string name, string id, ETimeUnit? selectedUnit, bool disabled)
    {
        var options = new object[]
        {
            new { label = "Phút", value = ETimeUnit.Minutes.ToString() },
            new { label = "Giờ", value = ETimeUnit.Hours.ToString() },
            new { label = "Ngày", value = ETimeUnit.Days.ToString() },
            new { label = "Tuần", value = ETimeUnit.Weeks.ToString() }
        };

        var selected = selectedUnit.HasValue
            ? new { label = GetTimeUnitLabel(selectedUnit.Value), value = selectedUnit.Value.ToString() }
            : null;

        return BuildSelectField(name, id, "Chọn đơn vị", options, selected, disabled);
    }

    private static object BuildSelectField(
        string name,
        string id,
        string placeholder,
        object[] options,
        object? valueSelected = null,
        bool disabled = false)
    {
        var component = new Dictionary<string, object?>
        {
            ["placeholder"] = placeholder,
            ["options"] = options
        };

        if (valueSelected != null)
            component["valueSelected"] = valueSelected;

        if (disabled)
            component["disabled"] = true;

        return new
        {
            name,
            inputs = new
            {
                id,
                type = 2,
                component
            }
        };
    }

    private static object BuildTextField(string name, string id, string placeholder, string defaultValue = "", int? maxLength = null, bool disabled = false)
    {
        var component = new Dictionary<string, object?>
        {
            ["id"] = $"{id}_input",
            ["placeholder"] = placeholder,
            ["defaultValue"] = defaultValue,
            ["type"] = "text",
            ["textarea"] = false
        };

        if (maxLength.HasValue)
            component["maxLength"] = maxLength.Value;

        if (disabled)
            component["disabled"] = true;

        return new
        {
            name,
            inputs = new
            {
                id,
                type = 3,
                component
            }
        };
    }

    private static object BuildTextAreaField(string name, string id, string placeholder, string defaultValue = "", bool disabled = false)
    {
        var component = new Dictionary<string, object?>
        {
            ["id"] = $"{id}_input",
            ["placeholder"] = placeholder,
            ["defaultValue"] = defaultValue,
            ["type"] = "text",
            ["textarea"] = true
        };

        if (disabled)
            component["disabled"] = true;

        return new
        {
            name,
            inputs = new
            {
                id,
                type = 3,
                component
            }
        };
    }

    private static object BuildNumberField(string name, string id, string placeholder, string defaultValue, bool disabled)
    {
        var component = new Dictionary<string, object?>
        {
            ["id"] = $"{id}_input",
            ["placeholder"] = placeholder,
            ["defaultValue"] = defaultValue,
            ["type"] = "number",
            ["textarea"] = false,
            ["min"] = 1,
            ["step"] = 1
        };

        if (disabled)
            component["disabled"] = true;

        return new
        {
            name,
            inputs = new
            {
                id,
                type = 3,
                component
            }
        };
    }

    private static string GetTimeUnitLabel(ETimeUnit unit) => unit switch
    {
        ETimeUnit.Minutes => "Phút",
        ETimeUnit.Hours => "Giờ",
        ETimeUnit.Days => "Ngày",
        ETimeUnit.Weeks => "Tuần",
        _ => unit.ToString()
    };

    private static object BuildRadioField(string name, string id, object[] options)
    {
        return new
        {
            name,
            inputs = new
            {
                id,
                type = 5,
                component = options
            }
        };
    }

    private static object[] BuildFormButtons(string submitId, string submitLabel = "✅ Tạo Task", string cancelId = "CANCEL_TASK", string cancelLabel = "❌ Hủy")
    {
        return new[]
        {
            new
            {
                components = new object[]
                {
                    new { id = submitId, type = 1, component = new { label = submitLabel, style = 3 } },
                    new { id = cancelId, type = 1, component = new { label = cancelLabel, style = 4 } }
                }
            }
        };
    }

    public static (bool IsValid, string Message) ValidateTaskForm(string title, string deadline, string assignee)
    {
        if (string.IsNullOrWhiteSpace(title))
            return (false, "❌ Tiêu đề không được để trống");

        if (title.Length > 100)
            return (false, "❌ Tiêu đề tối đa 100 ký tự");

        if (string.IsNullOrWhiteSpace(deadline))
            return (false, "❌ Deadline không được để trống");

        if (!DateTime.TryParse(deadline, out var deadlineDate))
            return (false, "❌ Định dạng deadline không hợp lệ. Dùng: YYYY-MM-DD HH:MM");

        if (deadlineDate <= DateTime.Now)
            return (false, "❌ Deadline phải lớn hơn thời gian hiện tại");

        if (string.IsNullOrWhiteSpace(assignee))
            return (false, "❌ Vui lòng chọn người được giao");

        return (true, "✅ Hợp lệ");
    }

    public static string GetStatusText(ETaskStatus status) => status switch
    {
        ETaskStatus.ToDo => "📋 ToDo",
        ETaskStatus.Doing => "🔄 Doing",
        ETaskStatus.Review => "✅ Review",
        ETaskStatus.Completed => "✔️ Completed",
        ETaskStatus.Cancelled => "❌ Cancelled",
        _ => "❓ Unknown"
    };

    public static string GetPriorityText(EPriorityLevel priority) => priority switch
    {
        EPriorityLevel.High => "🔴 Cao",
        EPriorityLevel.Medium => "🟡 Trung bình",
        EPriorityLevel.Low => "🟢 Thấp",
        _ => "⚪ Unknown"
    };
}
