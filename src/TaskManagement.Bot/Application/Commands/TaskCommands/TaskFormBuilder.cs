using Google.Protobuf.WellKnownTypes;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public static class TaskFormBuilder
{
    private static readonly TimeZoneInfo VN_TIME_ZONE =
    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    public static ChannelMessageContent BuildSelectProject(List<Project> projects, string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task ",
                    description = "Chọn Project:",
                    color = "#FEE75C",
                    fields = new object[]
                    {
                        BuildSelectField("📁 Project", "project", projects.Select(p => new
                        {
                            label = p.Name,
                            value = p.Id.ToString()
                        }).ToArray())
                    }
                }
            },
            Components = BuildNavigationButtons($"NEXT_STEP_1|{originalMessageId}", "➡️ Tiếp", "CANCEL", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildSelectTeam(int projectId, List<Team> teams, string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task ",
                    description = $"Project: {projectId}\nChọn Team:",
                    color = "#FEE75C",
                    fields = new object[]
                    {
                        BuildSelectField("👥 Team", "team", teams.Select(t => new
                        {
                            label = t.Name,
                            value = t.Id.ToString()
                        }).ToArray())
                    }
                }
            },
            Components = BuildNavigationButtons($"NEXT_STEP_2|{projectId}|{originalMessageId}", "➡️ Tiếp", "CANCEL", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildEnterDetails(
    string projectName,
    string teamName,
    string pmName,
    int projectId,
    int teamId,
    List<(string Id, string Name)> members,
    string originalMessageId)
    {
        var fields = new List<object>
        {
            BuildTextField("📌 Tiêu đề", "title", "Nhập tiêu đề", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "description", "Nhập mô tả"),
            BuildTextField("⏰ Deadline", "deadline", "YYYY-MM-DD HH:MM"),
            BuildRadioField("⚡ Độ ưu tiên", "priority", new object[]
            {
                new { label = "🔴 Cao", value = "High" },
                new { label = "🟡 Trung bình", value = "Medium", @default = true },
                new { label = "🟢 Thấp", value = "Low" }
            }),
            BuildSelectField("👤 Giao cho", "assignee", members.Select(m => new
            {
                label = m.Name,
                value = m.Id
            }).ToArray())
        };

        fields.AddRange(BuildReminderFields(TaskReminderFieldState.Default()));

        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "📝 Tạo Task ",
                description =
                    $"📁 Project: {projectName}\n" +
                    $"👥 Team: {teamName}\n" +
                    $"👑 PM: {pmName}",
                color = "#FEE75C",
                fields = fields.ToArray()
            }
        },
            Components = BuildNavigationButtons($"SUBMIT|{projectId}|{teamId}|{originalMessageId}", "✅ Tạo", "CANCEL", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildCreateFormWithSelectedProject(
        List<Project> projects,
        int selectedProjectId,
        List<Team> teams,
        List<(string Id, string Name)> members,
        string originalMessageId,
        int? selectedTeamId = null)
    {
        var projectOptions = projects.Select(p => new
        {
            label = p.Name,
            value = p.Id.ToString(),
            @default = p.Id == selectedProjectId
        }).ToArray();

        var teamOptions = teams.Select(t => new
        {
            label = t.Name,
            value = t.Id.ToString(),
            @default = selectedTeamId.HasValue && t.Id == selectedTeamId.Value
        }).ToArray();

        var memberOptions = members.Select(m => new
        {
            label = m.Name,
            value = m.Id
        }).ToArray();

        var fields = new List<object>
        {
            BuildSelectField("📁 Project", "project", projectOptions),
            BuildSelectField("👥 Team", "team", teamOptions),
            BuildSelectField("👤 Giao cho", "assignee", memberOptions),
            BuildTextField("📌 Tiêu đề", "title", "Nhập tiêu đề", maxLength: 100),
            BuildTextAreaField("📝 Mô tả", "description", "Nhập mô tả"),
            BuildTextField("⏰ Deadline", "deadline", "YYYY-MM-DD HH:MM"),
            BuildRadioField("⚡ Độ ưu tiên", "priority", new object[]
            {
                new { label = "🔴 Cao", value = "High" },
                new { label = "🟡 Trung bình", value = "Medium", @default = true },
                new { label = "🟢 Thấp", value = "Low" }
            })
        };

        fields.AddRange(BuildReminderFields(TaskReminderFieldState.Default()));

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📝 Tạo Task",
                    color = "#FEE75C",
                    fields = fields.ToArray(),
                    footer = new { text = "Trạng thái mặc định: ToDo" }
                }
            },
            Components = BuildNavigationButtons(
                $"SUBMIT|{selectedProjectId}|{selectedTeamId}|{originalMessageId}",
                "✅ Tạo Task",
                $"CANCEL|{originalMessageId}",
                "❌ Hủy"
            )
        };
    }

    public static ChannelMessageContent BuildTaskResult(TaskDto task)
    {
        var assigned = string.IsNullOrWhiteSpace(task.AssignedTo)
            ? "Chưa giao"
            : task.AssignedTo;

        var created = string.IsNullOrWhiteSpace(task.CreatedBy)
            ? "Unknown"
            : task.CreatedBy;

        var deadlineText = task.DueDate != null
            ? TimeZoneInfo.ConvertTimeFromUtc(task.DueDate.Value, VN_TIME_ZONE)
                .ToString("dd/MM/yyyy HH:mm")
            : "Không có";

        var createdText = TimeZoneInfo.ConvertTimeFromUtc(task.CreatedAt, VN_TIME_ZONE)
            .ToString("dd/MM/yyyy HH:mm");

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
                        new { name = "⚡ Độ ưu tiên", value = GetPriorityText(task.Priority), inline = true },
                        new { name = "⏰ Deadline", value = deadlineText, inline = true },
                        new { name = "👤 Giao cho", value = assigned, inline = true },
                        new { name = "👤 Người tạo", value = created, inline = true }
                    },
                    footer = new
                    {
                        text = $"Tạo lúc: {createdText}"
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildUpdateFormForMentor(TaskDto task, List<(string Id, string Name)> members, string originalMessageId)
    {
        var memberOptions = members.Select(m => new
        {
            label = m.Name,
            value = m.Id,
            @default = m.Id == task.AssignedTo
        }).ToArray();

        var fields = new List<object>
        {
            BuildTextField("📌 Tiêu đề", "title", "Nhập tiêu đề", task.Title ?? "", 100),
            BuildTextAreaField("📝 Mô tả", "description", "Nhập mô tả", task.Description ?? ""),
            BuildRadioField("⚡ Độ ưu tiên", "priority", new[]
            {
                new { label = "🔴 Cao", value = "High", @default = task.Priority == EPriorityLevel.High },
                new { label = "🟡 Trung bình", value = "Medium", @default = task.Priority == EPriorityLevel.Medium },
                new { label = "🟢 Thấp", value = "Low", @default = task.Priority == EPriorityLevel.Low }
            }),
            BuildRadioField("📊 Trạng thái", "status", new[]
            {
                new { label = "📋 ToDo", value = "ToDo", @default = task.Status == ETaskStatus.ToDo },
                new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review },
                new { label = "✔️ Completed", value = "Completed", @default = task.Status == ETaskStatus.Completed },
                new { label = "❌ Cancelled", value = "Cancelled", @default = task.Status == ETaskStatus.Cancelled }
            }),
            BuildTextField("⏰ Deadline", "deadline", "YYYY-MM-DD HH:MM", task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? ""),
            BuildSelectField("👤 Giao cho", "assignee", memberOptions)
        };

        fields.AddRange(BuildReminderFields(TaskReminderFieldState.FromRules(task.ReminderRules)));

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = $"✏️ Cập nhật Task #{task.Id}",
                    color = "#5865F2",
                    fields = fields.ToArray()
                }
            },
            Components = BuildNavigationButtons($"UPDATE|{task.Id}|{originalMessageId}", "💾 Lưu", $"CANCEL|{originalMessageId}", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildMemberUpdateTaskSelect(
    List<TaskDto> tasks,
    string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "✏️ Cập nhật Task (Member)",
                description = "Chọn task của bạn:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("📋 Task", "task",
                        tasks.Select(t => new
                        {
                            label = $"{t.Title} | {t.Status}",
                            value = t.Id.ToString()
                        }).ToArray())
                }
            }
        },
            Components = new[]
            {
                new
                {
                    components = new object[]
                    {
                        new
                        {
                            id = $"OPEN_UPDATE_FORM|{originalMessageId}",
                            type = 1,
                            component = new
                            {
                                label = "💾 Cập nhật",
                                style = 3
                            }
                        },
                        new
                        {
                            id = "CANCEL",
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

    public static ChannelMessageContent BuildUpdateFormForMember(TaskDto task)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = $"📊 Cập nhật Task #{task.Id}",
                    description = $"📌 {task.Title}",
                    color = "#FEE75C",
                    fields = new object[]
                    {
                        BuildRadioField("📊 Trạng thái mới", "status", new[]
                        {
                            new { label = "🔄 Doing", value = "Doing", @default = task.Status == ETaskStatus.Doing },
                            new { label = "✅ Review", value = "Review", @default = task.Status == ETaskStatus.Review }
                        })
                    }
                }
            },
            Components = BuildNavigationButtons($"UPDATE_STATUS|{task.Id}", "💾 Cập nhật", "CANCEL", "❌ Hủy")
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
                        new { id = $"CONFIRM_DELETE|{task.Id}", type = 1, component = new { label = "✅ Xóa", style = 4 } },
                        new { id = "CANCEL", type = 1, component = new { label = "❌ Hủy", style = 2 } }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildTaskList(
        List<TaskDto> tasks,
        string username,
        List<Team> teams)
    {
        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == ETaskStatus.Completed);
        var rate = total == 0 ? 0 : (double)completed / total * 100;

        var teamDict = teams.ToDictionary(t => t.Id);

        var projectGroups = tasks
            .GroupBy(t => teamDict[t.TeamId!.Value].ProjectId)
            .ToList();

        var embeds = new List<object>();

        foreach (var projectGroup in projectGroups)
        {
            var fields = new List<object>();

            var teamGroups = projectGroup.GroupBy(t => t.TeamId);

            foreach (var teamGroup in teamGroups)
            {
                var team = teamDict[teamGroup.Key!.Value];
                var teamTasks = teamGroup.ToList();

                var done = teamTasks.Count(t => t.Status == ETaskStatus.Completed);
                var percent = teamTasks.Count == 0 ? 0 : (double)done / teamTasks.Count * 100;

                //var taskLines = teamTasks.Count == 0
                //    ? "_Không có task_"
                //    : string.Join("\n\n", teamTasks.Select((t, i) =>
                //        $"\n{i + 1}. **{t.Title}**\n" +
                //        $"   {GetStatusText(t.Status)} | {GetPriorityText(t.Priority)} |  👤 {t.AssignedTo} | 📅 {t.DueDate:dd/MM}"
                //    ));
                var taskLines = teamTasks.Count == 0
                    ? "_Không có task_"
                    : string.Join("\n\n", teamTasks.Select((t, i) =>
                        $"\n{i + 1}. **{t.Title}**\n" +
                        $"   {GetStatusText(t.Status)} | {GetPriorityText(t.Priority)} |  👤 {t.AssignedTo} " +
                        $"📅 {
                            (t.DueDate != null
                                ? TimeZoneInfo.ConvertTimeFromUtc(
                                    t.DueDate.Value,
                                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                                ).ToString("dd/MM")
                                : "N/A")}"
                    ));

                fields.Add(new
                {
                    name = $"👥 {team.Name} ({teamTasks.Count} | ✅ {done} | {percent:0}%)",
                    value = taskLines,
                    inline = false
                });
            }

            embeds.Add(new
            {
                title = $"📁 Project {projectGroup.Key}",
                color = "#5865F2",
                fields = fields
            });
        }

        // Embed tổng
        embeds.Insert(0, new
        {
            title = $"📊 Task Dashboard - {username}",
            color = "#57F287",
            fields = new object[]
            {
            new { name = "📌 Total", value = total.ToString(), inline = true },
            new { name = "✅ Done", value = completed.ToString(), inline = true },
            new { name = "📈 Rate", value = $"{rate:0.0}%", inline = true }
            }
        });

        return new ChannelMessageContent
        {
            Embed = embeds.ToArray()
        };
    }

    public static ChannelMessageContent BuildViewSelectProject(List<Project> projects, string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "📊 Xem Task",
                description = "Chọn Project:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("📁 Project", "project", projects.Select(p => new
                    {
                        label = p.Name,
                        value = p.Id.ToString()
                    }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons($"VIEW_STEP_1|{originalMessageId}", "➡️ Tiếp", "CANCEL", "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildViewSelectTeam(int projectId, List<Team> teams, string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "📊 Xem Task",
                description = $"Project: {projectId}\nChọn Team:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("👥 Team", "team", teams.Select(t => new
                    {
                        label = t.Name,
                        value = t.Id.ToString()
                    }).ToArray())
                }
            }
        },
            Components = new[]
            {
                new
                {
                    components = new object[]
                    {
                        new { id = $"VIEW_SUBMIT|{projectId}|{originalMessageId}", type = 1, component = new { label = "👀 Xem", style = 3 } },
                        new { id = "CANCEL", type = 1, component = new { label = "❌ Hủy", style = 4 } }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildUpdateSelectTask(
    string projectName,
    string teamName,
    string pmName,
    int teamId,
    List<TaskDto> tasks,
    string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "✏️ Cập nhật Task",
                description =
                    $"📁 Project: {projectName}\n" +
                    $"👥 Team: {teamName}\n" +
                    $"👑 PM: {pmName}\n\n" +
                    $"Chọn Task:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("📋 Task", "task", tasks.Select(t => new
                    {
                        label =
                            $"📁 {projectName} | 👥 {teamName}\n" +
                            $"📌 {t.Title} | {GetStatusText(t.Status)}",
                        value = t.Id.ToString()
                    }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"UPDATE_SUBMIT|{teamId}|{originalMessageId}",
                "✏️ Sửa",
                "CANCEL",
                "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildUpdateSelectTeam(string projectName, int projectId, List<Team> teams, string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "✏️ Cập nhật Task",
                description = $"Project: {projectName}\nChọn Team:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("👥 Team", "team", teams.Select(t => new
                    {
                        label = t.Name,
                        value = t.Id.ToString()
                    }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"UPDATE_STEP_2|{projectId}|{originalMessageId}",
                "➡️ Tiếp",
                "CANCEL",
                "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildUpdateSelectProject(
    List<Project> projects,
    string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "✏️ Cập nhật Task",
                description = "Chọn Project:",
                color = "#5865F2",
                fields = new object[]
                {
                    BuildSelectField("📁 Project", "project", projects.Select(p => new
                    {
                        label = p.Name,
                        value = p.Id.ToString()
                    }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"UPDATE_STEP_1|{originalMessageId}",
                "➡️ Tiếp",
                "CANCEL",
                "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildUpdateStatusForm(TaskDto task)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "🔄 Cập nhật trạng thái",
                    description = $"📌 {task.Title}",
                    color = "#FEE75C",
                    fields = new object[] { }
                }
            },
            Components = new[]
            {
                new
                {
                    components = new object[]
                    {
                        new
                        {
                            id = $"UPDATE_STATUS_DOING|{task.Id}",
                            type = 1,
                            component = new
                            {
                                label = "🔄 Doing",
                                style = 1
                            }
                        },
                        new
                        {
                            id = $"UPDATE_STATUS_REVIEW|{task.Id}",
                            type = 1,
                            component = new
                            {
                                label = "✅ Review",
                                style = 3
                            }
                        },
                        new
                        {
                            id = "CANCEL",
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

    public static ChannelMessageContent BuildDeleteSelectProject(
        List<Project> projects,
        string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "🗑️ Xóa Task",
                description = "Chọn Project:",
                color = "#ED4245",
                fields = new object[]
                {
                    BuildSelectField("📁 Project", "project",
                        projects.Select(p => new
                        {
                            label = p.Name,
                            value = p.Id.ToString()
                        }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"DELETE_STEP_1|{originalMessageId}",
                "➡️ Tiếp",
                "CANCEL",
                "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildDeleteSelectTeam(
        int projectId,
        List<Team> teams,
        string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "🗑️ Xóa Task",
                description = $"Project: {projectId}\nChọn Team:",
                color = "#ED4245",
                fields = new object[]
                {
                    BuildSelectField("👥 Team", "team",
                        teams.Select(t => new
                        {
                            label = t.Name,
                            value = t.Id.ToString()
                        }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"DELETE_STEP_2|{projectId}|{originalMessageId}",
                "➡️ Tiếp",
                "CANCEL",
                "❌ Hủy")
        };
    }

    public static ChannelMessageContent BuildDeleteSelectTask(
        int teamId,
        List<TaskDto> tasks,
        string originalMessageId)
    {
        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "🗑️ Xóa Task",
                description = "Chọn task:",
                color = "#ED4245",
                fields = new object[]
                {
                    BuildSelectField("📋 Task", "task",
                        tasks.Select(t => new
                        {
                            label = $"{t.Title} | {GetStatusText(t.Status)}",
                            value = t.Id.ToString()
                        }).ToArray())
                }
            }
        },
            Components = BuildNavigationButtons(
                $"DELETE_CONFIRM|{teamId}|{originalMessageId}",
                "🗑️ Xóa",
                "CANCEL",
                "❌ Hủy")
        };
    }

    private static object BuildSelectField(string name, string id, object[] options)
    {
        return new
        {
            name,
            inputs = new
            {
                id,
                type = 2,
                component = new
                {
                    placeholder = $"Chọn {name.ToLower()}",
                    options,
                    custom_id = id,
                    trigger_on_change = true
                }
            }
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
        if (maxLength.HasValue) component["maxLength"] = maxLength.Value;
        if (disabled) component["disabled"] = true;

        return new { name, value = defaultValue, inputs = new { id, type = 3, component } };
    }

    private static object BuildTextAreaField(string name, string id, string placeholder, string defaultValue = "")
    {
        return new
        {
            name,
            value = defaultValue,
            inputs = new
            {
                id,
                type = 3,
                component = new { id = $"{id}_input", placeholder, defaultValue, type = "text", textarea = true }
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
        return new { name, inputs = new { id, type = 5, component = options } };
    }

    private static object[] BuildNavigationButtons(string actionId, string actionLabel, string cancelId, string cancelLabel)
    {
        return new[]
        {
            new
            {
                components = new object[]
                {
                    new { id = actionId, type = 1, component = new { label = actionLabel, style = 3 } },
                    new { id = cancelId, type = 1, component = new { label = cancelLabel, style = 4 } }
                }
            }
        };
    }

    public static (bool IsValid, string Message) ValidateTaskForm(string title, string deadline, string assignee)
    {
        if (string.IsNullOrWhiteSpace(title)) return (false, "❌ Tiêu đề không được để trống");
        if (title.Length > 100) return (false, "❌ Tiêu đề tối đa 100 ký tự");
        if (string.IsNullOrWhiteSpace(deadline)) return (false, "❌ Deadline không được để trống");
        if (!DateTime.TryParse(deadline, out var deadlineDate)) return (false, "❌ Định dạng deadline không hợp lệ. Dùng: YYYY-MM-DD HH:MM");
        //if (deadlineDate <= DateTime.Now) return (false, "❌ Deadline phải lớn hơn thời gian hiện tại");
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
        );

        if (deadlineDate <= vnNow) return (false, "❌ Deadline phải lớn hơn thời gian hiện tại");
        if (string.IsNullOrWhiteSpace(assignee)) return (false, "❌ Vui lòng chọn người được giao");
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
