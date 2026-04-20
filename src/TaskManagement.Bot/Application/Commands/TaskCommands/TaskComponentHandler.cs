using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public class TaskComponentHandler : IComponentHandler
{
    private readonly ILogger<TaskComponentHandler> _logger;
    private readonly ITaskService _taskService;
    private readonly ITeamService _teamService;
    private readonly IProjectService _projectService;
    private readonly MezonClient _client;

    public TaskComponentHandler(
        ILogger<TaskComponentHandler> logger,
        ITaskService taskService,
        ITeamService teamService,
        IProjectService projectService,
        MezonClient client)
    {
        _logger = logger;
        _taskService = taskService;
        _teamService = teamService;
        _projectService = projectService;
        _client = client;
    }

    public bool CanHandle(string customId) =>
        customId.StartsWith("SUBMIT_TASK", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("CANCEL_TASK", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("UPDATE_TASK", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("UPDATE_STATUS", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("EDIT_TASK", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("DELETE_TASK", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("CONFIRM_DELETE", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("CANCEL", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("LIST_TASKS", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("CLOSE_LIST", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("FILTER", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("SELECT_TEAM", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("SELECT_PROJECT", StringComparison.OrdinalIgnoreCase);

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.ClanId) || string.IsNullOrWhiteSpace(context.ChannelId))
            return new ComponentResponse();

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length == 0)
            return new ComponentResponse();

        return parts[0].ToUpperInvariant() switch
        {
            "SUBMIT_TASK" => await HandleSubmitAsync(context, parts, ct),
            "CANCEL_TASK" or "CANCEL_UPDATE" or "CANCEL_DELETE" => HandleCancel(context),
            "EDIT_TASK" => await HandleEditFormAsync(context, parts, ct),
            "UPDATE_TASK" => await HandleUpdateAsync(context, parts, ct),
            "UPDATE_STATUS" => await HandleUpdateStatusAsync(context, parts, ct),
            "DELETE_TASK" => await HandleDeletePromptAsync(context, parts, ct),
            "CONFIRM_DELETE" => await HandleConfirmDeleteAsync(context, parts, ct),
            "LIST_TASKS" => await HandleListAsync(context, ct),
            "CLOSE_LIST" => HandleCancel(context),
            "FILTER_STATUS" => await HandleFilterStatusAsync(context, ct),
            "FILTER_USER" => await HandleFilterUserAsync(context, ct),
            "FILTER_DEADLINE" => await HandleFilterDeadlineAsync(context, ct),
            "SELECT_TEAM" => await HandleSelectTeamAsync(context, parts, ct),
            "SELECT_PROJECT" => await HandleSelectProjectAsync(context, ct),
            _ => new ComponentResponse()
        };
    }

    private async Task<ComponentResponse> HandleSubmitAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var title = ReadValue(context.Payload, "task_title");
        var description = ReadValue(context.Payload, "task_description");
        var priorityStr = ReadValue(context.Payload, "task_priority");
        var deadlineStr = ReadValue(context.Payload, "task_deadline");
        var assignee = ReadValue(context.Payload, "task_assignee");

        var projectIdStr = ReadValue(context.Payload, "task_project");
        var teamIdStr = ReadValue(context.Payload, "task_team");
        var reminderState = ReadReminderState(context.Payload);

        // Validate
        var (isValid, message) = TaskFormBuilder.ValidateTaskForm(title, deadlineStr, assignee);
        if (!isValid)
            return BuildTextResponse(context, message);

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người tạo");

        if (!int.TryParse(projectIdStr, out var projectId) &&
            (parts.Length < 2 || !int.TryParse(parts[1], out projectId)))
        {
            return BuildTextResponse(context, "❌ Vui lòng chọn project");
        }

        if (!int.TryParse(teamIdStr, out var teamId) &&
            (parts.Length < 3 || !int.TryParse(parts[2], out teamId)))
        {
            return BuildTextResponse(context, "❌ Vui lòng chọn team");
        }


        //  Validate team thuộc project 
        var teams = await _teamService.GetTeamsByProjectAsync(projectId);
        if (!teams.Any(t => t.Id == teamId))
            return BuildTextResponse(context, "❌ Team không thuộc project đã chọn");

        //  Validate member thuộc team
        var members = await _teamService.GetMembers(teamId);
        if (!members.Contains(assignee))
            return BuildTextResponse(context, "❌ Người được giao phải thuộc team");

        var reminderValidation = reminderState.Validate();
        if (!reminderValidation.IsValid)
            return BuildTextResponse(context, reminderValidation.Message);

        //  Parse data
        var priority = priorityStr switch
        {
            "High" => EPriorityLevel.High,
            "Low" => EPriorityLevel.Low,
            _ => EPriorityLevel.Medium
        };

        DateTime.TryParse(deadlineStr, out var deadline);

        var dto = new CreateTaskDto
        {
            Title = title,
            Description = description,
            AssignedTo = assignee,
            CreatedBy = context.CurrentUserId,
            DueDate = deadline,
            Priority = priority,
            TeamId = teamId,
            ClanIds = new List<string> { context.ClanId! },
            ChannelIds = new List<string> { context.ChannelId! },
            ReminderRules = reminderValidation.Rules.ToList()
        };

        //  Save
        var task = await _taskService.CreateAsync(dto, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không thể tạo task");

        //  Close form + show result
        return BuildSuccessResponse(context, TaskFormBuilder.BuildTaskResult(task));
    }

    private async Task<ComponentResponse> HandleEditFormAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người dùng");

        // Check if user is PM or assignee
        var isPM = task.TeamId.HasValue && await _teamService.IsPM(context.CurrentUserId, task.TeamId.Value);
        var isAssignee = task.AssignedTo == context.CurrentUserId;

        if (!isPM && !isAssignee)
            return BuildTextResponse(context, "❌ Bạn không có quyền sửa task này");

        if (isPM)
        {
            var members = task.TeamId.HasValue ? await _teamService.GetMembers(task.TeamId.Value) : new List<string>();
            return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!,
                TaskFormBuilder.BuildUpdateFormForMentor(task, members), context.Mode, context.IsPublic, context.MessageId!, null);
        }

        // Member can only update status
        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!,
            TaskFormBuilder.BuildUpdateFormForMember(task), context.Mode, context.IsPublic, context.MessageId!, null);
    }

    private async Task<ComponentResponse> HandleUpdateAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        // Check PM permission
        if (task.TeamId.HasValue && !await _teamService.IsPM(context.CurrentUserId!, task.TeamId.Value))
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền cập nhật đầy đủ");

        var title = ReadValue(context.Payload, "task_title");
        var description = ReadValue(context.Payload, "task_description");
        var priorityStr = ReadValue(context.Payload, "task_priority");
        var statusStr = ReadValue(context.Payload, "task_status");
        var deadlineStr = ReadValue(context.Payload, "task_deadline");
        var assignee = ReadValue(context.Payload, "task_assignee");
        var hasReminderState = TryReadReminderState(context.Payload, out var reminderState);
        TaskReminderValidationResult? reminderValidation = null;
        if (hasReminderState)
        {
            reminderValidation = reminderState.Validate();
            if (!reminderValidation.IsValid)
                return BuildTextResponse(context, reminderValidation.Message);
        }

        var updateDto = new UpdateTaskDto
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Description = description,
            Priority = ParsePriority(priorityStr),
            Status = ParseStatus(statusStr),
            DueDate = DateTime.TryParse(deadlineStr, out var d) ? d : null,
            AssignedTo = string.IsNullOrWhiteSpace(assignee) ? null : assignee,
            ReminderRules = hasReminderState ? reminderValidation!.Rules.ToList() : null
        };

        await _taskService.UpdateAsync(taskId, updateDto, ct);

        return HandleCancel(context, $"✅ Đã cập nhật task #{taskId}");
    }

    private async Task<ComponentResponse> HandleUpdateStatusAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định user");

        //  chỉ cho update task của mình
        if (task.AssignedTo != context.CurrentUserId)
            return BuildTextResponse(context, "❌ Bạn chỉ có thể cập nhật task của mình");

        var oldStatus = task.Status.ToString();

        var statusStr = ReadValue(context.Payload, "task_status");
        var newStatusEnum = ParseStatus(statusStr);

        if (newStatusEnum == null)
            return BuildTextResponse(context, "❌ Trạng thái không hợp lệ");

        var newStatus = newStatusEnum.Value.ToString();

        //  chỉ cho Doing + Review
        if (newStatusEnum != ETaskStatus.Doing && newStatusEnum != ETaskStatus.Review)
            return BuildTextResponse(context, "❌ Bạn chỉ được chuyển sang Doing hoặc Review");

        // update
        await _taskService.ChangeStatusAsync(taskId, newStatusEnum.Value, ct);

        //  chỉ notify khi status thay đổi
        if (oldStatus != newStatus)
        {
            await NotifyMentorAsync(
                task,
                oldStatus,
                newStatus,
                context.CurrentUserId,
                context,
                ct);
        }

        return HandleCancel(context, $"✅ Đã cập nhật trạng thái task #{taskId}");
    }

    private async Task<ComponentResponse> HandleDeletePromptAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người dùng");

        // Check PM permission
        if (task.TeamId.HasValue && !await _teamService.IsPM(context.CurrentUserId!, task.TeamId.Value))
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền xóa task");

        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!,
            TaskFormBuilder.BuildDeleteConfirm(task), context.Mode, context.IsPublic, context.MessageId!, null);
    }

    private async Task<ComponentResponse> HandleConfirmDeleteAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người dùng");

        // CHECK QUYỀN 
        var isMentor = task.TeamId.HasValue &&
                       await _teamService.IsPM(context.CurrentUserId, task.TeamId.Value);

        if (!isMentor)
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền xóa task");

        await _taskService.DeleteAsync(taskId, ct);

        return HandleCancel(context, $"✅ Đã xóa task #{taskId}");
    }

    private async Task<ComponentResponse> HandleListAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người dùng");

        // Get user's teams
        var teams = await _teamService.GetTeamsByMemberAsync(context.CurrentUserId);
        if (teams.Count == 0)
            return BuildTextResponse(context, "❌ Bạn chưa tham gia team nào");

        var allTasks = new List<TaskDto>();
        var isMentor = false;

        foreach (var team in teams)
        {
            if (await _teamService.IsPM(context.CurrentUserId, team.Id))
            {
                isMentor = true;
                var teamTasks = await _taskService.GetTasksByTeamAsync(team.Id, ct);
                allTasks.AddRange(teamTasks);
            }
            else
            {
                var myTasks = await _taskService.GetByAssigneeAsync(context.CurrentUserId, null, ct);
                allTasks.AddRange(myTasks.Where(t => t.TeamId == team.Id));
            }
        }

        var distinctTasks = allTasks.DistinctBy(t => t.Id).ToList();

        return ComponentResponse.FromContent(context.ClanId!, context.ChannelId!,
            TaskFormBuilder.BuildTaskList(distinctTasks, isMentor), context.Mode, context.IsPublic, context.MessageId!, null);
    }

    private async Task<ComponentResponse> HandleFilterStatusAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định user");

        var (tasks, isMentor) = await GetUserTasksAsync(context.CurrentUserId, ct);

        // ví dụ filter: Doing + Review
        var filtered = tasks
            .Where(t => t.Status == ETaskStatus.Doing || t.Status == ETaskStatus.Review)
            .ToList();

        return ReplaceForm(context,
            TaskFormBuilder.BuildTaskList(filtered, isMentor, "status"));
    }

    private async Task<ComponentResponse> HandleFilterUserAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định user");

        var (tasks, isMentor) = await GetUserTasksAsync(context.CurrentUserId, ct);

        // filter chính user hiện tại
        var filtered = tasks
            .Where(t => t.AssignedTo == context.CurrentUserId)
            .ToList();

        return ReplaceForm(context,
            TaskFormBuilder.BuildTaskList(filtered, isMentor, "user"));
    }

    private async Task<ComponentResponse> HandleFilterDeadlineAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định user");

        var (tasks, isMentor) = await GetUserTasksAsync(context.CurrentUserId, ct);

        var now = DateTime.Now;

        // task sắp hết hạn (<= 3 ngày)
        var filtered = tasks
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate.Value <= now.AddDays(3))
            .OrderBy(t => t.DueDate)
            .ToList();

        return ReplaceForm(context,
            TaskFormBuilder.BuildTaskList(filtered, isMentor, "deadline"));
    }

    private async Task<(List<TaskDto> Tasks, bool IsMentor)> GetUserTasksAsync(string userId, CancellationToken ct)
    {
        var teams = await _teamService.GetTeamsByMemberAsync(userId);
        var allTasks = new List<TaskDto>();
        var isMentor = false;

        foreach (var team in teams)
        {
            if (await _teamService.IsPM(userId, team.Id))
            {
                isMentor = true;
                var teamTasks = await _taskService.GetTasksByTeamAsync(team.Id, ct);
                allTasks.AddRange(teamTasks);
            }
            else
            {
                var myTasks = await _taskService.GetByAssigneeAsync(userId, null, ct);
                allTasks.AddRange(myTasks.Where(t => t.TeamId == team.Id));
            }
        }

        return (allTasks.DistinctBy(t => t.Id).ToList(), isMentor);
    }

    private async Task<ComponentResponse> HandleSelectTeamAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var projectId))
            return BuildTextResponse(context, "❌ Project ID không hợp lệ");

        var teamIdStr = ReadValue(context.Payload, "task_team");
        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Vui lòng chọn team");

        var members = await _teamService.GetMembers(teamId);
        if (members.Count == 0)
            return BuildTextResponse(context, "❌ Team không có thành viên");

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithMembers(projectId, teamId, members));
    }

    private async Task<ComponentResponse> HandleSelectProjectAsync(ComponentContext context, CancellationToken ct)
    {
        var projectIdStr = ReadValue(context.Payload, "task_project");

        if (!int.TryParse(projectIdStr, out var projectId))
            return BuildTextResponse(context, "❌ Vui lòng chọn project");

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        if (teams.Count == 0)
            return BuildTextResponse(context, "❌ Project chưa có team");

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithTeams(projectId, teams));
    }

    private static ComponentResponse ReplaceForm(ComponentContext context, ChannelMessageContent content)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = context.MessageId
            });
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = content,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private static ComponentResponse HandleCancel(ComponentContext context, string? message = null)
    {
        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = context.MessageId
            });
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            response.Messages.Add(new ComponentMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                Text = message,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = context.MessageId
            });
        }

        return response;
    }

    private static ComponentResponse BuildTextResponse(ComponentContext context, string text) =>
        ComponentResponse.FromText(context.ClanId!, context.ChannelId!, text, context.Mode, context.IsPublic, context.MessageId!, null);

    private static EPriorityLevel? ParsePriority(string? value) => value switch
    {
        "High" => EPriorityLevel.High,
        "Medium" => EPriorityLevel.Medium,
        "Low" => EPriorityLevel.Low,
        _ => null
    };

    private static ETaskStatus? ParseStatus(string? value) => value switch
    {
        "ToDo" => ETaskStatus.ToDo,
        "Doing" => ETaskStatus.Doing,
        "Review" => ETaskStatus.Review,
        "Completed" => ETaskStatus.Completed,
        "Cancelled" => ETaskStatus.Cancelled,
        _ => null
    };

    private static TaskReminderFieldState ReadReminderState(JsonElement payload)
    {
        return new TaskReminderFieldState
        {
            IsEnabled = ReadBool(payload, "task_reminder_enabled", defaultValue: true),
            BeforeValue = ReadValueOrDefault(payload, "task_reminder_before_value", "30"),
            BeforeUnit = ReadTimeUnit(payload, "task_reminder_before_unit") ?? ETimeUnit.Minutes,
            AfterValue = ReadValueOrDefault(payload, "task_reminder_after_value", "10"),
            AfterUnit = ReadTimeUnit(payload, "task_reminder_after_unit") ?? ETimeUnit.Minutes,
            IsAfterRepeatEnabled = ReadBool(payload, "task_reminder_after_repeat", defaultValue: true),
            RepeatValue = ReadValue(payload, "task_reminder_repeat_value"),
            RepeatUnit = ReadTimeUnit(payload, "task_reminder_repeat_unit")
        };
    }

    private static bool TryReadReminderState(JsonElement payload, out TaskReminderFieldState state)
    {
        if (!TryReadFormElement(payload, "task_reminder_enabled", out _))
        {
            state = TaskReminderFieldState.Default(isEnabled: false);
            return false;
        }

        state = ReadReminderState(payload);
        return true;
    }

    private static ETimeUnit? ReadTimeUnit(JsonElement payload, string key)
    {
        var value = ReadValue(payload, key);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<ETimeUnit>(value, ignoreCase: true, out var unit))
            return unit;

        return int.TryParse(value, out var numericUnit) && Enum.IsDefined(typeof(ETimeUnit), numericUnit)
            ? (ETimeUnit)numericUnit
            : null;
    }

    private static string ReadValueOrDefault(JsonElement payload, string key, string defaultValue) =>
        TryReadFormElement(payload, key, out var element)
            ? ConvertElementToString(element)
            : defaultValue;

    private static string ReadValue(JsonElement payload, string key)
    {
        return TryReadFormElement(payload, key, out var element)
            ? ConvertElementToString(element)
            : string.Empty;
    }

    private static bool ReadBool(JsonElement payload, string key, bool defaultValue)
    {
        if (!TryReadFormElement(payload, key, out var element))
            return defaultValue;

        return ConvertElementToBool(element, defaultValue);
    }

    private static bool TryReadFormElement(JsonElement payload, string key, out JsonElement element)
    {
        var valuesNode = ComponentPayloadHelper.GetValues(payload);
        var valueElement = ComponentPayloadHelper.GetPropertyIgnoreCase(valuesNode, key);
        if (valueElement.HasValue)
        {
            element = valueElement.Value;
            return true;
        }

        var extraData = ComponentPayloadHelper.GetExtraData(payload);
        if (string.IsNullOrWhiteSpace(extraData) || !extraData.TrimStart().StartsWith("{"))
        {
            element = default;
            return false;
        }

        try
        {
            using var json = JsonDocument.Parse(extraData);
            valueElement = ComponentPayloadHelper.GetPropertyIgnoreCase(json.RootElement, key);
            if (valueElement.HasValue)
            {
                element = valueElement.Value.Clone();
                return true;
            }
        }
        catch
        {
        }

        element = default;
        return false;
    }

    private static string ConvertElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Object when ComponentPayloadHelper.GetPropertyIgnoreCase(element, "value") is { } value =>
                ConvertElementToString(value),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElementToString).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
            _ => string.Empty
        };
    }

    private static bool ConvertElementToBool(JsonElement element, bool defaultValue)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt32(out var value) ? value != 0 : defaultValue,
            JsonValueKind.String => ParseBoolString(element.GetString(), defaultValue),
            JsonValueKind.Object when ComponentPayloadHelper.GetPropertyIgnoreCase(element, "checked") is { } checkedValue =>
                ConvertElementToBool(checkedValue, defaultValue),
            JsonValueKind.Object when ComponentPayloadHelper.GetPropertyIgnoreCase(element, "value") is { } value =>
                ConvertElementToBool(value, defaultValue),
            JsonValueKind.Array => element.EnumerateArray().Any(item => ConvertElementToBool(item, false)),
            _ => defaultValue
        };
    }

    private static bool ParseBoolString(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (bool.TryParse(value, out var parsed))
            return parsed;

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "on" or "yes" or "checked" => true,
            "0" or "off" or "no" or "unchecked" => false,
            _ => defaultValue
        };
    }

    private static ComponentResponse BuildSuccessResponse(ComponentContext context, ChannelMessageContent content)
    {
        var response = new ComponentResponse();

        // delete form
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = context.MessageId
            });
        }

        // send result
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = content,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private async Task NotifyMentorAsync(
        TaskDto task,
        string oldStatus,
        string newStatus,
        string updatedBy,
        ComponentContext context,
        CancellationToken ct)
    {
        if (!task.TeamId.HasValue) return;

        var mentorId = await _teamService.GetPMIdAsync(task.TeamId.Value);
        if (string.IsNullOrWhiteSpace(mentorId)) return;

        var message = $"""
            📢 **Task #{task.Id} thay đổi trạng thái**

            👤 Member: <@{updatedBy}>
            🔄 {oldStatus} → {newStatus}
            """;

        await _client.SendEphemeralMessageAsync(
            receiverId: mentorId,
            clanId: context.ClanId!,
            channelId: context.ChannelId!,
            mode: context.Mode,
            isPublic: context.IsPublic,
            content: new ChannelMessageContent { Text = message },
            cancellationToken: ct);
    }
}
