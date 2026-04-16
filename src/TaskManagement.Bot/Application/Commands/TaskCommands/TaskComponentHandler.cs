using System.Text.Json;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.Sessions;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public class TaskComponentHandler : IComponentHandler
{
    private readonly ILogger<TaskComponentHandler> _logger;
    private readonly ITaskService _taskService;
    private readonly ITeamService _teamService;
    private readonly SessionService _sessionService;
    private readonly MezonClient _client;

    public TaskComponentHandler(
        ILogger<TaskComponentHandler> logger,
        ITaskService taskService,
        ITeamService teamService,
        SessionService sessionService,
        MezonClient client)
    {
        _logger = logger;
        _taskService = taskService;
        _teamService = teamService;
        _sessionService = sessionService;
        _client = client;
    }

    public bool CanHandle(string customId)
    {
        return customId == "SUBMIT_TASK"
            || customId == "CANCEL_TASK"
            || customId.StartsWith("UPDATE_TASK", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("UPDATE_TASK_STATUS", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("CANCEL_UPDATE", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("DELETE_TASK", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("CONFIRM_DELETE_TASK", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("CANCEL_DELETE", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("LIST_TASKS", StringComparison.OrdinalIgnoreCase)
            || customId == "CLOSE_TASK_LIST"
            || customId.StartsWith("FILTER_BY_STATUS", StringComparison.OrdinalIgnoreCase)
            || customId.StartsWith("FILTER_BY_DEADLINE", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.ClanId) || string.IsNullOrWhiteSpace(context.ChannelId))
        {
            return new ComponentResponse();
        }

        var customId = context.CustomId;
        if (string.IsNullOrEmpty(customId))
        {
            return new ComponentResponse();
        }

        var parts = customId.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var action = parts[0].ToUpperInvariant();

        return action switch
        {
            "SUBMIT_TASK" => await HandleSubmitTaskAsync(context, cancellationToken),
            "CANCEL_TASK" => BuildCancelTaskResponse(context),
            "UPDATE_TASK" => await HandleUpdateTaskAsync(context, parts, cancellationToken),
            "UPDATE_TASK_STATUS" => await HandleUpdateTaskStatusAsync(context, parts, cancellationToken),
            "CANCEL_UPDATE" => BuildCancelUpdateResponse(context),
            "DELETE_TASK" => await HandleDeleteTaskAsync(context, parts, cancellationToken),
            "CONFIRM_DELETE_TASK" => await HandleConfirmDeleteTaskAsync(context, parts, cancellationToken),
            "CANCEL_DELETE" => BuildCancelDeleteResponse(context),
            "LIST_TASKS" => await HandleListTasksAsync(context, cancellationToken),
            "CLOSE_TASK_LIST" => BuildCloseListResponse(context),
            "FILTER_BY_STATUS" => await HandleFilterByStatusAsync(context, cancellationToken),
            "FILTER_BY_DEADLINE" => await HandleFilterByDeadlineAsync(context, cancellationToken),
            _ => new ComponentResponse()
        };
    }

    // ==================== UPDATE TASK FOR MENTOR ====================
    private async Task<ComponentResponse> HandleUpdateTaskAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 2) return new ComponentResponse();

        var taskId = int.Parse(parts[1]);

        // Kiểm tra quyền (chỉ Mentor mới được update)
        var userId = context.CurrentUserId;
        var session = _sessionService.Get(long.Parse(userId));

        if (session?.TeamId == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Bạn chưa tham gia team nào!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Kiểm tra có phải Mentor (PM) không
        var isPM = await _teamService.IsPM(userId, session.TeamId.Value);
        if (!isPM)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Chỉ Mentor mới có quyền cập nhật task!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Lấy dữ liệu từ form
        var title = ReadValue(context.Payload, "task_title");
        var description = ReadValue(context.Payload, "task_description");
        var priorityStr = ReadValue(context.Payload, "task_priority");
        var statusStr = ReadValue(context.Payload, "task_status");
        var deadlineStr = ReadValue(context.Payload, "task_deadline");
        var assignee = ReadValue(context.Payload, "task_assignee");

        // Parse
        var priority = priorityStr switch
        {
            "High" => EPriorityLevel.High,
            "Medium" => EPriorityLevel.Medium,
            "Low" => EPriorityLevel.Low,
            _ => EPriorityLevel.Medium
        };

        var status = statusStr switch
        {
            "ToDo" => ETaskStatus.ToDo,
            "Doing" => ETaskStatus.Doing,
            "Review" => ETaskStatus.Review,
            "Completed" => ETaskStatus.Completed,
            "Cancelled" => ETaskStatus.Cancelled,
            _ => ETaskStatus.ToDo
        };

        DateTime? deadline = null;
        if (!string.IsNullOrWhiteSpace(deadlineStr) && DateTime.TryParse(deadlineStr, out var parsedDeadline))
        {
            deadline = parsedDeadline;
        }

        // Cập nhật task
        var updateDto = new UpdateTaskDto
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = status,
            DueDate = deadline,
            AssignedTo = assignee
        };

        await _taskService.UpdateAsync(taskId, updateDto);

        var response = new ComponentResponse();

        // Xóa form update
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Gửi thông báo thành công
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = $"✅ Đã cập nhật task #{taskId} thành công!",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    // ==================== UPDATE STATUS FOR MEMBER ====================
    private async Task<ComponentResponse> HandleUpdateTaskStatusAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 2) return new ComponentResponse();

        var taskId = int.Parse(parts[1]);
        var userId = context.CurrentUserId;

        // Lấy task hiện tại
        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Không tìm thấy task!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Kiểm tra quyền: Member chỉ được cập nhật task của mình
        if (task.AssignedTo != userId)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Bạn chỉ có thể cập nhật trạng thái task được giao cho bạn!",
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Lấy trạng thái mới
        var statusStr = ReadValue(context.Payload, "task_status");
        var status = statusStr switch
        {
            "ToDo" => ETaskStatus.ToDo,
            "Doing" => ETaskStatus.Doing,
            "Review" => ETaskStatus.Review,
            "Completed" => ETaskStatus.Completed,
            _ => ETaskStatus.ToDo
        };

        // Cập nhật trạng thái
        await _taskService.ChangeStatusAsync(taskId, status);

        var response = new ComponentResponse();

        // Xóa form update
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Gửi thông báo thành công
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = $"✅ Đã cập nhật trạng thái task #{taskId} thành {GetStatusText(status)}!",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    // ==================== DELETE TASK (MENTOR ONLY) ====================
    private async Task<ComponentResponse> HandleDeleteTaskAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 2) return new ComponentResponse();

        var taskId = int.Parse(parts[1]);
        var userId = context.CurrentUserId;
        var session = _sessionService.Get(long.Parse(userId));

        if (session?.TeamId == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Bạn chưa tham gia team nào!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Kiểm tra quyền Mentor
        var isPM = await _teamService.IsPM(userId, session.TeamId.Value);
        if (!isPM)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Chỉ Mentor mới có quyền xóa task!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Không tìm thấy task!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Hiển thị form xác nhận xóa
        var confirmForm = BuildDeleteTaskConfirmForm(task);

        var response = new ComponentResponse();

        // Xóa tin nhắn cũ
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Gửi form xác nhận
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = confirmForm,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private async Task<ComponentResponse> HandleConfirmDeleteTaskAsync(ComponentContext context, string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 2) return new ComponentResponse();

        var taskId = int.Parse(parts[1]);

        await _taskService.DeleteAsync(taskId);

        var response = new ComponentResponse();

        // Xóa form xác nhận
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Gửi thông báo
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = $"✅ Đã xóa task #{taskId} thành công!",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    // ==================== LIST TASKS ====================
    private async Task<ComponentResponse> HandleListTasksAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        var userId = context.CurrentUserId;
        var session = _sessionService.Get(long.Parse(userId));

        if (session?.TeamId == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Bạn chưa tham gia team nào!", context.Mode, context.IsPublic, context.MessageId, null);
        }

        var isPM = await _teamService.IsPM(userId, session.TeamId.Value);

        List<TaskDto> tasks;
        if (isPM)
        {
            // Mentor: xem tất cả task của team
            tasks = await _taskService.GetTasksByTeamAsync(session.TeamId.Value);
        }
        else
        {
            // Member: chỉ xem task của mình
            tasks = await _taskService.GetByAssigneeAsync(userId, null);
        }

        var taskListForm = BuildTaskListForm(tasks, isPM ? "PM" : "Member");

        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!, taskListForm,
            context.Mode, context.IsPublic, context.MessageId, null);
    }

    // ==================== FILTER FUNCTIONS ====================
    private async Task<ComponentResponse> HandleFilterByStatusAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        // Tạo form chọn trạng thái để lọc
        var filterForm = BuildFilterByStatusForm();

        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!, filterForm,
            context.Mode, context.IsPublic, context.MessageId, null);
    }

    private async Task<ComponentResponse> HandleFilterByDeadlineAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        // Tạo form chọn deadline để lọc
        var filterForm = BuildFilterByDeadlineForm();

        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!, filterForm,
            context.Mode, context.IsPublic, context.MessageId, null);
    }

    // ==================== BUILD FORM METHODS ====================

    private ChannelMessageContent BuildTaskResultForm(TaskDto task)
    {
        var priorityEmoji = task.Priority switch
        {
            EPriorityLevel.High => "🔴",
            EPriorityLevel.Medium => "🟡",
            EPriorityLevel.Low => "🟢",
            _ => "⚪"
        };

        var statusEmoji = task.Status switch
        {
            ETaskStatus.ToDo => "📋",
            ETaskStatus.Doing => "🔄",
            ETaskStatus.Review => "✅",
            ETaskStatus.Completed => "✔️",
            ETaskStatus.Cancelled => "❌",
            _ => "📋"
        };

        var interactive = new
        {
            title = "✅ Task đã được tạo thành công!",
            description = "Thông tin chi tiết của task:",
            color = "#57F287",
            fields = new object[]
            {
                new { name = "📌 Tiêu đề", value = task.Title, inline = false },
                new { name = "📝 Mô tả", value = string.IsNullOrEmpty(task.Description) ? "Không có mô tả" : task.Description, inline = false },
                new { name = "⚡ Độ ưu tiên", value = $"{priorityEmoji} {task.Priority}", inline = true },
                new { name = "📊 Trạng thái", value = $"{statusEmoji} {task.Status}", inline = true },
                new { name = "⏰ Deadline", value = task.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Không có", inline = true },
                new { name = "👤 Người tạo", value = task.CreatedBy ?? "Unknown", inline = true },
                new { name = "👤 Giao cho", value = task.AssignedTo ?? "Chưa giao", inline = true },
                new { name = "📅 Ngày tạo", value = task.CreatedAt.ToString("dd/MM/yyyy HH:mm"), inline = true }
            },
            footer = new { text = $"Task ID: {task.Id}" }
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
                            id = $"CLOSE_TASK_RESULT|{task.Id}",
                            type = 1,
                            component = new { label = "❌ Đóng", style = 4 }
                        }
                    }
                }
            }
        };
    }

    private ChannelMessageContent BuildDeleteTaskConfirmForm(TaskDto task)
    {
        var interactive = new
        {
            title = "⚠️ Xác nhận xóa Task",
            description = $"Bạn có chắc chắn muốn xóa task này?",
            color = "#ED4245",
            fields = new object[]
            {
                new { name = "📌 Tiêu đề", value = task.Title, inline = false },
                new { name = "🆔 ID", value = task.Id.ToString(), inline = true },
                new { name = "👤 Người tạo", value = task.CreatedBy, inline = true }
            },
            footer = new { text = "Hành động này không thể hoàn tác!" }
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

    private ChannelMessageContent BuildTaskListForm(List<TaskDto> tasks, string role)
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
                new { name = "📝 Danh sách", value = taskList, inline = false }
            }
        };

        var components = new List<object>();

        components.Add(new
        {
            id = "CLOSE_TASK_LIST",
            type = 1,
            component = new { label = "❌ Đóng", style = 4 }
        });

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

    private ChannelMessageContent BuildFilterByStatusForm()
    {
        // Tạo form lọc theo trạng thái
        return new ChannelMessageContent { Text = "Filter by status - Coming soon" };
    }

    private ChannelMessageContent BuildFilterByDeadlineForm()
    {
        // Tạo form lọc theo deadline
        return new ChannelMessageContent { Text = "Filter by deadline - Coming soon" };
    }

    // ==================== CLOSE/DELETE RESPONSES ====================

    private static ComponentResponse BuildCancelUpdateResponse(ComponentContext context)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = "❌ Đã hủy cập nhật task.",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private static ComponentResponse BuildCancelDeleteResponse(ComponentContext context)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = "❌ Đã hủy xóa task.",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private static ComponentResponse BuildCloseListResponse(ComponentContext context)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        return response;
    }

    private static ComponentResponse BuildCancelTaskResponse(ComponentContext context)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(context.ClanId!, context.ChannelId!, context.MessageId,
                context.Mode, context.IsPublic, context.MessageId, null);
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = "❌ Đã hủy tạo task.",
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId,
            OriginalMessage = null
        });

        return response;
    }

    private async Task<ComponentResponse> HandleSubmitTaskAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        // Lấy dữ liệu từ form
        var title = ReadValue(context.Payload, "task_title");
        var description = ReadValue(context.Payload, "task_description");
        var priorityStr = ReadValue(context.Payload, "task_priority");
        var deadlineStr = ReadValue(context.Payload, "task_deadline");
        var assignee = ReadValue(context.Payload, "task_assignee");

        _logger.LogInformation($"Task form submitted: Title={title}, Priority={priorityStr}, Deadline={deadlineStr}, Assignee={assignee}");

        // Validate
        var (isValid, message) = TaskFormBuilder.ValidateTaskForm(title, deadlineStr, assignee);
        if (!isValid)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, message, context.Mode, context.IsPublic, context.MessageId, null);
        }

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Không xác định được người tạo task", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Parse priority
        var priority = priorityStr switch
        {
            "High" => EPriorityLevel.High,
            "Medium" => EPriorityLevel.Medium,
            "Low" => EPriorityLevel.Low,
            _ => EPriorityLevel.Medium
        };

        // Parse deadline
        if (!DateTime.TryParse(deadlineStr, out var deadline))
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!, "❌ Định dạng deadline không hợp lệ", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Lấy team của user
        var userId = context.CurrentUserId;
        var session = _sessionService.Get(long.Parse(userId));

        int? teamId = null;
        if (session?.TeamId != null)
        {
            teamId = session.TeamId;
        }
        else
        {
            // Tìm team mà user là thành viên
            var teams = await _teamService.GetTeamsByMemberAsync(userId);
            if (teams.Any())
            {
                teamId = teams.First().Id;
                // Lưu vào session
                if (session != null)
                {
                    session.TeamId = teamId;
                    session.TeamName = teams.First().Name;
                    session.TeamMembers = await _teamService.GetMembers(teamId.Value);
                    _sessionService.Set(long.Parse(userId), session);
                }
            }
        }

        if (teamId == null)
        {
            return ComponentResponse.FromText(context.ClanId!, context.ChannelId!,
                "❌ Bạn chưa tham gia team nào. Hãy tạo team bằng lệnh `!team init`", context.Mode, context.IsPublic, context.MessageId, null);
        }

        // Tạo task
        var taskDto = new CreateTaskDto
        {
            Title = title,
            Description = description,
            AssignedTo = assignee,
            CreatedBy = userId,
            DueDate = deadline,
            Priority = priority,
            TeamId = teamId,
            ClanIds = new List<string> { context.ClanId! },
            ChannelIds = new List<string> { context.ChannelId! }
        };

        var createdTask = await _taskService.CreateAsync(taskDto);

        // Tạo form hiển thị kết quả task đã tạo 
        var resultForm = BuildTaskResultForm(createdTask);

        var response = ComponentResponse.FromContent(
            context.ClanId!,
            context.ChannelId!,
            resultForm,
            context.Mode,
            context.IsPublic,
            context.MessageId,
            null);

        // Xóa form sau khi submit thành công
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessage(
                context.ClanId!,
                context.ChannelId!,
                context.MessageId,
                context.Mode,
                context.IsPublic,
                context.MessageId,
                null);
        }

        return response;
    }

    private static string ReadValue(JsonElement payload, string key)
    {
        var valuesNode = ComponentPayloadHelper.GetValues(payload);
        var fromValues = ComponentPayloadHelper.GetPropertyIgnoreCase(valuesNode, key)?.GetString();
        if (!string.IsNullOrWhiteSpace(fromValues))
        {
            return fromValues;
        }

        var extraData = ComponentPayloadHelper.GetExtraData(payload);
        if (string.IsNullOrWhiteSpace(extraData) || !extraData.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        try
        {
            using var json = JsonDocument.Parse(extraData);
            return ComponentPayloadHelper.GetPropertyIgnoreCase(json.RootElement, key)?.GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
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
}