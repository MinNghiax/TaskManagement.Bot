using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Entities;
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

    public bool CanHandle(string customId)
    {
        var prefixes = new[] { "NEXT_STEP_1", "NEXT_STEP_2", "VIEW_STEP_1", "VIEW_SUBMIT", "UPDATE_STEP_1", "UPDATE_STEP_2",
            "UPDATE_SELECT_TASK", "UPDATE_SUBMIT", "DELETE_STEP_1", "DELETE_STEP_2", "DELETE_CONFIRM", "OPEN_UPDATE_FORM", "SUBMIT",
            "UPDATE", "UPDATE_STATUS", "CONFIRM_DELETE", "CANCEL", "CLOSE", "SELECT_PROJECT", "SELECT_TEAM" };
        return prefixes.Any(p => customId.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.ClanId) || string.IsNullOrWhiteSpace(context.ChannelId))
            return new ComponentResponse();

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length == 0) return new ComponentResponse();

        return parts[0].ToUpperInvariant() switch
        {
            "NEXT_STEP_1" => await HandleNextStep1Async(context, parts, ct),
            "NEXT_STEP_2" => await HandleNextStep2Async(context, parts, ct),
            "VIEW_STEP_1" => await HandleViewStep1(context, parts, ct),
            "VIEW_SUBMIT" => await HandleViewSubmit(context, parts, ct),
            "UPDATE_STEP_1" => await HandleUpdateStep1(context, parts, ct),
            "UPDATE_STEP_2" => await HandleUpdateStep2(context, parts, ct),
            "UPDATE_SELECT_TASK" => await HandleUpdateSelectTask(context, parts, ct),
            "UPDATE_SUBMIT" => await HandleUpdateSubmit(context, parts, ct),
            "DELETE_STEP_1" => await HandleDeleteStep1(context, parts, ct),
            "DELETE_STEP_2" => await HandleDeleteStep2(context, parts, ct),
            "DELETE_CONFIRM" => await HandleDeleteSelectTask(context, parts, ct),
            "OPEN_UPDATE_FORM" => await HandleOpenUpdateForm(context, ct),
            "SUBMIT" => await HandleSubmitAsync(context, parts, ct),
            "UPDATE" => await HandleUpdateAsync(context, parts, ct),
            "UPDATE_STATUS" => await HandleUpdateStatusAsync(context, parts, ct),
            "UPDATE_STATUS_MEMBER" => await HandleMemberUpdate(context, ct),
            "CONFIRM_DELETE" => await HandleConfirmDeleteAsync(context, parts, ct),
            "CANCEL" => HandleCancel(context),
            "CLOSE" => HandleCancel(context),
            "SELECT_PROJECT" => await HandleSelectProjectAutoAsync(context, ct),
            "SELECT_TEAM" => await HandleSelectTeamAutoAsync(context, ct),
            _ => new ComponentResponse()
        };
    }

    private async Task<ComponentResponse> HandleNextStep1Async(ComponentContext context, string[] parts, CancellationToken ct)
    {
        _logger.LogInformation($"PAYLOAD: {context.Payload}");
        _logger.LogInformation($"VALUES: {ComponentPayloadHelper.GetValues(context.Payload)}");
        var projectIdStr = GetSelectedValue(context.Payload, "project");
        if (!int.TryParse(projectIdStr, out var projectId))
            return BuildTextResponse(context, "❌ Vui lòng chọn Project");

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);
        if (teams.Count == 0)
            return BuildTextResponse(context, "❌ Project này chưa có Team nào");

        var originalMessageId = parts.Length >= 2 ? parts[1] : null;

        return ReplaceForm(context, TaskFormBuilder.BuildSelectTeam(projectId, teams, originalMessageId ?? context.MessageId!));
    }

    private async Task<ComponentResponse> HandleNextStep2Async(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var projectId))
            return BuildTextResponse(context, "❌ Dữ liệu không hợp lệ");

        var teamIdStr = GetSelectedValue(context.Payload, "team");
        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Vui lòng chọn Team");

        var rawMembers = await _teamService.GetMembers(teamId);

        var members = rawMembers
            .Select(userId =>
            {
                var user = _client.Clans.Get(context.ClanId!)?.Users.Get(userId);

                var name = user?.DisplayName
                           ?? user?.ClanNick
                           ?? user?.Username
                           ?? $"User-{userId.Substring(0, 4)}";

                return (Id: userId, Name: name);
            })
            .ToList();
        if (members.Count == 0)
            return BuildTextResponse(context, "❌ Team này chưa có thành viên nào");

        //  lấy ProjectName
        var projects = await _projectService.GetAllProjectsAsync();
        var projectName = projects.FirstOrDefault(p => p.Id == projectId)?.Name ?? $"#{projectId}";

        //  lấy TeamName
        var teams = await _teamService.GetTeamsByProjectAsync(projectId);
        var teamName = teams.FirstOrDefault(t => t.Id == teamId)?.Name ?? $"#{teamId}";

        //  lấy PM ID
        var pmId = await _teamService.GetPMIdAsync(teamId);

        //  convert sang DisplayName
        var pmName = !string.IsNullOrWhiteSpace(pmId)
            ? GetDisplayName(pmId, context.ClanId!)
            : "Unknown";

        var originalMessageId = parts.Length >= 3 ? parts[2] : null;

        return ReplaceForm(context,
            TaskFormBuilder.BuildEnterDetails(
                projectName,
                teamName,
                pmName,
                projectId,
                teamId,
                members,
                originalMessageId ?? context.MessageId!
            ));
    }

    private async Task<ComponentResponse> HandleViewStep1(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectIdStr = GetSelectedValue(context.Payload, "project");

        if (!int.TryParse(projectIdStr, out var projectId))
            return BuildTextResponse(context, "❌ Chọn project");

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        if (teams.Count == 0)
            return BuildTextResponse(context, "❌ Project chưa có team");

        var originalMessageId = parts.Length >= 2 ? parts[1] : null;

        return ReplaceForm(context,
            TaskFormBuilder.BuildViewSelectTeam(projectId, teams, originalMessageId ?? context.MessageId!));
    }

    private async Task<ComponentResponse> HandleViewSubmit(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectId = int.Parse(parts[1]);
        var teamIdStr = GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Chọn team");

        var tasks = await _taskService.GetTasksByTeamAsync(teamId, ct);

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        var content = TaskFormBuilder.BuildTaskList(tasks, context.CurrentUserId!, teams);

        return ReplaceForm(context, content);
    }

    private async Task<ComponentResponse> HandleUpdateStep1(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectIdStr = GetSelectedValue(context.Payload, "project");

        if (!int.TryParse(projectIdStr, out var projectId))
            return BuildTextResponse(context, "❌ Chọn project");

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        var projects = await _projectService.GetAllProjectsAsync();
        var projectName = projects.FirstOrDefault(p => p.Id == projectId)?.Name ?? $"#{projectId}";

        return ReplaceForm(context,
            TaskFormBuilder.BuildUpdateSelectTeam(projectName, projectId, teams, parts[1]));
    }

    private async Task<ComponentResponse> HandleUpdateStep2(ComponentContext context, string[] parts, CancellationToken ct)
    {
        // lấy projectId từ parts
        if (parts.Length < 3 || !int.TryParse(parts[1], out var projectId))
            return BuildTextResponse(context, "❌ Dữ liệu project không hợp lệ");
        var teamIdStr = GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Chọn team");

        var tasks = await _taskService.GetTasksByTeamAsync(teamId, ct);

        var projects = await _projectService.GetAllProjectsAsync();
        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        // lấy name
        var projectName = projects.FirstOrDefault(p => p.Id == projectId)?.Name ?? $"#{projectId}";
        var team = teams.FirstOrDefault(t => t.Id == teamId);
        var teamName = team?.Name ?? $"#{teamId}";

        // lấy PM
        var pmId = await _teamService.GetPMIdAsync(teamId);
        var pmName = !string.IsNullOrWhiteSpace(pmId)
            ? GetDisplayName(pmId, context.ClanId!)
            : "Unknown";

        return ReplaceForm(context,
            TaskFormBuilder.BuildUpdateSelectTask(
                projectName,
                teamName,
                pmName,
                teamId,
                tasks,
                parts[2]
            ));
    }

    private async Task<ComponentResponse> HandleUpdateSelectTask(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Chọn task");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        // lấy member (để hiển thị dropdown)
        var members = await _teamService.GetMembersWithDisplay(task.TeamId.Value, context.ClanId!);

        var isMentor = await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value);

        var isOwner = string.Equals(
            task.AssignedTo?.Trim(),
            context.CurrentUserId?.Trim(),
            StringComparison.Ordinal
        );

        ChannelMessageContent content;

        var originalMessageId = parts.LastOrDefault();

        if (isMentor)
        {
            content = TaskFormBuilder.BuildUpdateFormForMentor(task, members, originalMessageId ?? context.MessageId!);
        }
        else
        {
            content = TaskFormBuilder.BuildUpdateFormForMember(task);
        }

        return ReplaceForm(context, content);
    }

    private async Task<ComponentResponse> HandleUpdateSubmit(
        ComponentContext context,
        string[] parts,
        CancellationToken ct)
    {
        _logger.LogInformation($"[UPDATE_SUBMIT] Payload: {context.Payload}");

        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (string.IsNullOrWhiteSpace(taskIdStr))
            return BuildTextResponse(context, "❌ Bạn chưa chọn task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Task không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        var members = await _teamService.GetMembersWithDisplay(task.TeamId.Value, context.ClanId!);

        var isMentor = await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value);

        var isOwner = string.Equals(
            task.AssignedTo?.Trim(),
            context.CurrentUserId?.Trim(),
            StringComparison.Ordinal
        );

        ChannelMessageContent content;

        var originalMessageId = parts.LastOrDefault();

        if (isMentor)
        {
            content = TaskFormBuilder.BuildUpdateFormForMentor(task, members, originalMessageId ?? context.MessageId!);
        }
        else
        {
            content = TaskFormBuilder.BuildUpdateFormForMember(task);
        }

        return ReplaceForm(context, content);
    }

    private async Task<ComponentResponse> HandleDeleteStep1(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectIdStr = GetSelectedValue(context.Payload, "project");

        if (!int.TryParse(projectIdStr, out var projectId))
            return BuildTextResponse(context, "❌ Chọn project");

        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        return ReplaceForm(context,
            TaskFormBuilder.BuildDeleteSelectTeam(projectId, teams, parts[1]));
    }

    private async Task<ComponentResponse> HandleDeleteStep2(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectId = int.Parse(parts[1]);
        var teamIdStr = GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Chọn team");

        var tasks = await _taskService.GetTasksByTeamAsync(teamId, ct);

        //  CHỈ lấy task do PM tạo
        tasks = tasks
            .Where(t => t.CreatedBy == context.CurrentUserId)
            .ToList();

        if (!tasks.Any())
            return BuildTextResponse(context, "❌ Không có task nào để xóa");

        return ReplaceForm(context,
            TaskFormBuilder.BuildDeleteSelectTask(teamId, tasks, parts[2]));
    }

    private async Task<ComponentResponse> HandleDeleteSelectTask(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Chọn task");

        var task = await _taskService.GetByIdAsync(taskId, ct);

        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (!await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value))
            return BuildTextResponse(context, "❌ Chỉ PM được xóa");

        if (task.CreatedBy != context.CurrentUserId)
            return BuildTextResponse(context, "❌ Không thể xóa task của người khác");

        return ReplaceForm(context,
            TaskFormBuilder.BuildDeleteConfirm(task));
    }

    private async Task<ComponentResponse> HandleOpenUpdateForm(ComponentContext context, CancellationToken ct)
    {
        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Chọn task");

        var task = await _taskService.GetByIdAsync(taskId, ct);

        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        return ReplaceForm(context,
            TaskFormBuilder.BuildUpdateFormForMember(task));
    }

    private async Task<ComponentResponse> HandleSubmitAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var originalMessageId = parts.Length >= 4 ? parts[3] : null;
        var projectIdStr = parts.Length >= 2 ? parts[1] : GetSelectedValue(context.Payload, "project");
        var teamIdStr = parts.Length >= 3 ? parts[2] : GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(projectIdStr, out var projectId) || !int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Dữ liệu không hợp lệ");

        var title = ReadValue(context.Payload, "title");
        var description = ReadValue(context.Payload, "description");
        var priorityStr = ReadValue(context.Payload, "priority");
        var deadlineStr = ReadValue(context.Payload, "deadline");
        var assignee = ReadValue(context.Payload, "assignee");
        var reminderState = ReadReminderState(context.Payload);

        var (isValid, message) = TaskFormBuilder.ValidateTaskForm(title, deadlineStr, assignee);
        if (!isValid) return BuildTextResponse(context, message);

        var reminderValidation = reminderState.Validate();
        if (!reminderValidation.IsValid)
            return BuildTextResponse(context, reminderValidation.Message);

        var members = await _teamService.GetMembersWithDisplay(teamId, context.ClanId!);
        if (!members.Any(x => x.Id == assignee))
            return BuildTextResponse(context, "❌ Người được giao phải thuộc Team");

        var priority = priorityStr switch { "High" => EPriorityLevel.High, "Low" => EPriorityLevel.Low, _ => EPriorityLevel.Medium };
        DateTime.TryParse(deadlineStr, out var deadline);

        // convert VN → UTC trước khi lưu DB
        var utcDeadline = TimeZoneInfo.ConvertTimeToUtc(
            deadline,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
        );

        var dto = new CreateTaskDto
        {
            Title = title,
            Description = description,
            AssignedTo = assignee,
            CreatedBy = context.CurrentUserId!,
            DueDate = utcDeadline,
            Priority = priority,
            TeamId = teamId,
            ClanIds = new List<string> { context.ClanId! },
            ChannelIds = new List<string> { context.ChannelId! },
            ReminderRules = reminderValidation.Rules.ToList()
        };

        var task = await _taskService.CreateAsync(dto, ct);
        if (task == null) return BuildTextResponse(context, "❌ Không thể tạo task");

        var result = MapToDisplayTask(task, context.ClanId!);

        return BuildSuccessResponse(context, TaskFormBuilder.BuildTaskResult(result), originalMessageId);
    }

    private async Task<ComponentResponse> HandleUpdateAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null) return BuildTextResponse(context, "❌ Không tìm thấy task");

        var isPM = await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value);

        var title = ReadValue(context.Payload, "title");
        var description = ReadValue(context.Payload, "description");
        var priorityStr = ReadValue(context.Payload, "priority");
        var statusStr = ReadValue(context.Payload, "status");
        var deadlineStr = ReadValue(context.Payload, "deadline");
        var assignee = ReadValue(context.Payload, "assignee");
        var hasReminderState = TryReadReminderState(context.Payload, out var reminderState);
        TaskReminderValidationResult? reminderValidation = null;
        if (hasReminderState)
        {
            reminderValidation = reminderState.Validate();
            if (!reminderValidation.IsValid)
                return BuildTextResponse(context, reminderValidation.Message);
        }

        var newStatus = ParseStatus(statusStr);

        //  MEMBER → chỉ update status của chính mình
        if (!isPM)
        {
            var assignedTo = task.AssignedTo?.Trim();
            var currentUser = context.CurrentUserId?.Trim();

            if (!string.Equals(assignedTo, currentUser, StringComparison.Ordinal))
                return BuildTextResponse(context, "❌ Chỉ được update task của mình");

            if (newStatus == null)
                return BuildTextResponse(context, "❌ Trạng thái không hợp lệ");

            if (!IsValidMemberTransition(task.Status, newStatus.Value))
                return BuildTextResponse(context, $"❌ Không thể chuyển từ {task.Status} → {newStatus}");

            await _taskService.ChangeStatusAsync(taskId, newStatus.Value, ct);
            return HandleCancel(context, $"✅ Đã cập nhật trạng thái task #{taskId}");
        }

        //  PM → full quyền
        if (newStatus != null && !IsValidMentorTransition(task.Status, newStatus.Value))
        {
            return BuildTextResponse(context, $"❌ Không thể chuyển từ {task.Status} → {newStatus}");
        }

        var updateDto = new UpdateTaskDto
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Description = description,
            Priority = ParsePriority(priorityStr),
            Status = newStatus,
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
        if (task == null) return BuildTextResponse(context, "❌ Không tìm thấy task");

        _logger.LogWarning($"AssignedTo: [{task.AssignedTo}] - CurrentUser: [{context.CurrentUserId}]");

        var assignedTo = task.AssignedTo?.Trim();
        var currentUser = context.CurrentUserId?.Trim();

        if (!string.Equals(assignedTo, currentUser, StringComparison.Ordinal))
            return BuildTextResponse(context, "❌ Chỉ được cập nhật task của mình");

        var statusStr = ReadValue(context.Payload, "status");
        var newStatus = ParseStatus(statusStr);
        if (newStatus == null)
            return BuildTextResponse(context, "❌ Trạng thái không hợp lệ");

        if (!IsValidMemberTransition(task.Status, newStatus.Value))
            return BuildTextResponse(context, $"❌ Không thể chuyển từ {task.Status} → {newStatus}");

        await _taskService.ChangeStatusAsync(taskId, newStatus.Value, ct);
        return HandleCancel(context, $"✅ Đã cập nhật trạng thái task #{taskId}");
    }

    private async Task<ComponentResponse> HandleMemberUpdate(ComponentContext context, CancellationToken ct)
    {
        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Chọn task");

        var task = await _taskService.GetByIdAsync(taskId, ct);

        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        _logger.LogWarning($"AssignedTo: [{task.AssignedTo}] - CurrentUser: [{context.CurrentUserId}]");

        var assignedTo = task.AssignedTo?.Trim();
        var currentUser = context.CurrentUserId?.Trim();

        if (!string.Equals(assignedTo, currentUser, StringComparison.Ordinal))
            return BuildTextResponse(context, "❌ Không phải task của bạn");

        return ReplaceForm(context,
            TaskFormBuilder.BuildUpdateStatusForm(task));
    }

    private async Task<ComponentResponse> HandleConfirmDeleteAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null) return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (!await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value))
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền xóa");

        if (task.CreatedBy != context.CurrentUserId)
            return BuildTextResponse(context, "❌ Bạn không thể xóa task của người khác");

        await _taskService.DeleteAsync(taskId, ct);
        return HandleCancel(context, $"✅ Đã xóa task #{taskId}");
    }

    private async Task<ComponentResponse> HandleSelectProjectAutoAsync(ComponentContext context, CancellationToken ct)
    {
        var projectIdStr = GetSelectedValue(context.Payload, "project");

        _logger.LogInformation($"[AUTO] Project selected = {projectIdStr}");

        if (!int.TryParse(projectIdStr, out var projectId))
            return new ComponentResponse(); // không spam lỗi

        var projects = await _projectService.GetAllProjectsAsync();
        var teams = await _teamService.GetTeamsByProjectAsync(projectId);

        if (teams.Count == 0)
            return BuildTextResponse(context, "❌ Project chưa có team");

        // load ALL member của project
        var members = new List<(string Id, string Name)>();
        foreach (var t in teams)
        {
            var m = await _teamService.GetMembersWithDisplay(t.Id, context.ClanId!);
            members.AddRange(m);
        }

        members = members.Distinct().ToList();

        var parts = context.CustomId?.Split('|') ?? [];
        var originalMessageId = parts.LastOrDefault();

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithSelectedProject(
                projects,
                projectId,
                teams,
                members,
                originalMessageId ?? context.MessageId!
            ));
    }

    private async Task<ComponentResponse> HandleSelectTeamAutoAsync(ComponentContext context, CancellationToken ct)
    {
        var projectIdStr = GetSelectedValue(context.Payload, "project");
        var teamIdStr = GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(projectIdStr, out var projectId))
            return new ComponentResponse();

        if (!int.TryParse(teamIdStr, out var teamId))
            return new ComponentResponse();

        var projects = await _projectService.GetAllProjectsAsync();
        var teams = await _teamService.GetTeamsByProjectAsync(projectId);
        var members = await _teamService.GetMembersWithDisplay(teamId, context.ClanId!);

        var parts = context.CustomId?.Split('|') ?? [];
        var originalMessageId = parts.LastOrDefault();

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithSelectedProject(
                projects,
                projectId,
                teams,
                members, 
                originalMessageId ?? context.MessageId!,
                teamId
            ));
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
                ReplyToMessageId = null
            });
        }

        //  LẤY originalMessageId từ customId
        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        //var originalMessageId = parts.Length >= 2 ? parts[^1] : null;
        // luôn lấy cái CUỐI nếu có
        var originalMessageId = parts.LastOrDefault() ?? context.MessageId;

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = content,
            Mode = context.Mode,
            IsPublic = context.IsPublic,

            //  reply vào message gốc
            ReplyToMessageId = originalMessageId,

            OriginalMessage = new ChannelMessage
            {
                Id = originalMessageId ?? context.MessageId!,
                ChannelId = context.ChannelId!,
                ClanId = context.ClanId!,
                SenderId = context.CurrentUserId,
                ChannelLabel = ""
            }
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
                ReplyToMessageId = null
            });
        }

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        //var originalMessageId = context.MessageId;
        // luôn lấy cái CUỐI nếu có
        var originalMessageId = parts.LastOrDefault() ?? context.MessageId;

        if (!string.IsNullOrWhiteSpace(message))
        {
            response.Messages.Add(new ComponentMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                Text = message,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = originalMessageId,
                OriginalMessage = new ChannelMessage
                {
                    Id = originalMessageId ?? context.MessageId!,
                    ChannelId = context.ChannelId!,
                    ClanId = context.ClanId!,
                    SenderId = context.CurrentUserId,
                    ChannelLabel = ""
                }
            });
        }
        return response;
    }

    private static ComponentResponse BuildTextResponse(ComponentContext context, string text)
    {
        //  LẤY originalMessageId từ customId
        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        
        // luôn lấy cái CUỐI nếu có
        var originalMessageId = parts.LastOrDefault() ?? context.MessageId;

        return ComponentResponse.FromText(
            context.ClanId!,
            context.ChannelId!,
            text,
            context.Mode,
            context.IsPublic,

            //  reply vào message gốc
            originalMessageId ?? context.MessageId!,

            new ChannelMessage
            {
                Id = originalMessageId ?? context.MessageId!,
                ChannelId = context.ChannelId!,
                ClanId = context.ClanId!,
                SenderId = context.CurrentUserId,
                Username = context.CurrentUserId,
                DisplayName = context.CurrentUserId,
                Content = new ChannelMessageContent
                {
                    Text = ""
                },

                ChannelLabel = ""
            }
        );
    }

    private static ComponentResponse BuildSuccessResponse(ComponentContext context, ChannelMessageContent content, string? originalMessageId)
    {
        var response = new ComponentResponse();

        // CHỈ delete khi submit xong
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = null
            });
        }

        // gửi result (KHÔNG reply vào message đã bị xóa)
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = content,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = new ChannelMessage
            {
                Id = originalMessageId ?? context.MessageId!,
                ChannelId = context.ChannelId!,
                ClanId = context.ClanId!,
                SenderId = context.CurrentUserId,
                Username = "",
                DisplayName = "",
                ChannelLabel = ""
            }
        });
        return response;
    }

    private TaskDto MapToDisplayTask(TaskDto task, string clanId)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            AssignedTo = GetDisplayName(task.AssignedTo!, clanId),
            CreatedBy = GetDisplayName(task.CreatedBy!, clanId),
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt
        };
    }

    private string GetDisplayName(string userId, string clanId)
    {
        var user = _client.Clans.Get(clanId)?.Users.Get(userId);

        if (user == null)
            return $"User-{userId.Substring(0, 4)}";

        return user.DisplayName
            ?? user.ClanNick
            ?? user.Username
            ?? $"User-{userId.Substring(0, 4)}";
    }

    private static string? GetSelectedValue(JsonElement payload, string key)
    {
        //  Ưu tiên đọc từ ExtraData
        var extra = ComponentPayloadHelper.GetExtraData(payload);

        if (!string.IsNullOrWhiteSpace(extra) && extra.TrimStart().StartsWith("{"))
        {
            try
            {
                using var json = JsonDocument.Parse(extra);
                var val = ComponentPayloadHelper
                    .GetPropertyIgnoreCase(json.RootElement, key);

                if (val.HasValue)
                    return ConvertElementToString(val.Value);
            }
            catch { }
        }

        //  fallback: đọc từ Values
        var values = ComponentPayloadHelper.GetValues(payload);

        if (values.ValueKind != JsonValueKind.Object)
            return null;

        var node = ComponentPayloadHelper.GetPropertyIgnoreCase(values, key);

        if (!node.HasValue)
            return null;

        var el = node.Value;

        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty("values", out var arr) &&
            arr.ValueKind == JsonValueKind.Array &&
            arr.GetArrayLength() > 0)
        {
            return arr[0].GetString();
        }

        if (el.ValueKind == JsonValueKind.String)
            return el.GetString();

        return null;
    }

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
            JsonValueKind.Object when ComponentPayloadHelper.GetPropertyIgnoreCase(element, "values") is { } values =>
                ConvertElementToString(values),
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

    private static bool IsValidMemberTransition(ETaskStatus current, ETaskStatus next)
    {
        return (current, next) switch
        {
            (ETaskStatus.ToDo, ETaskStatus.Doing) => true,
            (ETaskStatus.Doing, ETaskStatus.Review) => true,
            _ => false
        };
    }

    private static bool IsValidMentorTransition(ETaskStatus current, ETaskStatus next)
    {
        return (current, next) switch
        {
            (ETaskStatus.Doing, ETaskStatus.Review) => true,
            (ETaskStatus.Review, ETaskStatus.Completed) => true,
            _ => false
        };
    }

    private static EPriorityLevel? ParsePriority(string? value) => value switch { "High" => EPriorityLevel.High, "Medium" => EPriorityLevel.Medium, "Low" => EPriorityLevel.Low, _ => null };
    private static ETaskStatus? ParseStatus(string? value) => value switch { "ToDo" => ETaskStatus.ToDo, "Doing" => ETaskStatus.Doing, "Review" => ETaskStatus.Review, "Completed" => ETaskStatus.Completed, "Cancelled" => ETaskStatus.Cancelled, _ => null };

}
