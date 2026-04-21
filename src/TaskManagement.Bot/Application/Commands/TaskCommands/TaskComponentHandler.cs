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
        var prefixes = new[] { "NEXT_STEP_1", "NEXT_STEP_2", "VIEW_STEP_1", "VIEW_SUBMIT", "UPDATE_STEP_1", "UPDATE_STEP_2", "UPDATE_SELECT_TASK", "UPDATE_SUBMIT",
            "SUBMIT", "UPDATE", "UPDATE_STATUS", "CONFIRM_DELETE", "CANCEL", "CLOSE", "FILTER_STATUS", "FILTER_USER", "SELECT_PROJECT", "SELECT_TEAM" };
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
            "SUBMIT" => await HandleSubmitAsync(context, parts, ct),
            "UPDATE" => await HandleUpdateAsync(context, parts, ct),
            "UPDATE_STATUS" => await HandleUpdateStatusAsync(context, parts, ct),
            "CONFIRM_DELETE" => await HandleConfirmDeleteAsync(context, parts, ct),
            "CANCEL" => HandleCancel(context),
            "CLOSE" => HandleCancel(context),
            //"FILTER_STATUS" => await HandleFilterStatusAsync(context, ct),
            //"FILTER_USER" => await HandleFilterUserAsync(context, ct),
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

        return ReplaceForm(context, TaskFormBuilder.BuildSelectTeam(projectId, teams, originalMessageId));
    }

    private async Task<ComponentResponse> HandleNextStep2Async(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var projectId))
            return BuildTextResponse(context, "❌ Dữ liệu không hợp lệ");

        var teamIdStr = GetSelectedValue(context.Payload, "team");
        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Vui lòng chọn Team");

        var rawMembers = await _teamService.GetMembers(teamId);

        //var clanUsers = _client.Clans.Get(context.ClanId!)?.Users.GetAll();
        //_logger.LogInformation($"Total users: {clanUsers?.Count()}");
        //var members = rawMembers
        //    .Select(userId =>
        //    {
        //        var user = clanUsers?.FirstOrDefault(u => u.Id == userId);

        //        var name = user?.DisplayName
        //                   ?? user?.ClanNick
        //                   ?? user?.Username
        //                   ?? GetDisplayName(userId, context.ClanId!); 

        //        return (Id: userId, Name: name);
        //    })
        //    .ToList();

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
                originalMessageId
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
            TaskFormBuilder.BuildViewSelectTeam(projectId, teams, originalMessageId));
    }

    private async Task<ComponentResponse> HandleViewSubmit(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var projectId = int.Parse(parts[1]);
        var teamIdStr = GetSelectedValue(context.Payload, "team");

        if (!int.TryParse(teamIdStr, out var teamId))
            return BuildTextResponse(context, "❌ Chọn team");

        //  reuse logic cũ của bạn
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

        //return ReplaceForm(context,
        //    TaskFormBuilder.BuildUpdateSelectTeam(projectId, teams, parts[1]));
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

        //  reuse logic cũ
        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        // check role
        var isMentor = await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value);

        // lấy member (để hiển thị dropdown)
        var members = await _teamService.GetMembersWithDisplay(task.TeamId.Value, context.ClanId!);

        //  HIỂN THỊ FORM
        var content = isMentor
            ? TaskFormBuilder.BuildUpdateFormForMentor(task, members)
            : TaskFormBuilder.BuildUpdateFormForMember(task);

        return ReplaceForm(context, content);
    }

    private async Task<ComponentResponse> HandleUpdateSubmit(
    ComponentContext context,
    string[] parts,
    CancellationToken ct)
    {
        // ⚠️ DEBUG payload
        _logger.LogInformation($"[UPDATE_SUBMIT] Payload: {context.Payload}");

        var taskIdStr = GetSelectedValue(context.Payload, "task");

        if (string.IsNullOrWhiteSpace(taskIdStr))
            return BuildTextResponse(context, "❌ Bạn chưa chọn task");

        if (!int.TryParse(taskIdStr, out var taskId))
            return BuildTextResponse(context, "❌ Task không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return BuildTextResponse(context, "❌ Không tìm thấy task");

        var isMentor = await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value);

        var members = await _teamService.GetMembersWithDisplay(task.TeamId.Value, context.ClanId!);

        var content = isMentor
            ? TaskFormBuilder.BuildUpdateFormForMentor(task, members)
            : TaskFormBuilder.BuildUpdateFormForMember(task);

        return ReplaceForm(context, content);
    }

    private async Task<ComponentResponse> HandleSubmitAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        var originalMessageId = parts.Length >= 4 ? parts[3] : null;
        if (parts.Length < 3 || !int.TryParse(parts[1], out var projectId) || !int.TryParse(parts[2], out var teamId))
            return BuildTextResponse(context, "❌ Dữ liệu không hợp lệ");

        var title = ReadValue(context.Payload, "title");
        var description = ReadValue(context.Payload, "description");
        var priorityStr = ReadValue(context.Payload, "priority");
        var deadlineStr = ReadValue(context.Payload, "deadline");
        var assignee = ReadValue(context.Payload, "assignee");

        var (isValid, message) = TaskFormBuilder.ValidateTaskForm(title, deadlineStr, assignee);
        if (!isValid) return BuildTextResponse(context, message);

        var members = await _teamService.GetMembersWithDisplay(teamId, context.ClanId!);
        if (!members.Any(x => x.Id == assignee))
            return BuildTextResponse(context, "❌ Người được giao phải thuộc Team");

        var priority = priorityStr switch { "High" => EPriorityLevel.High, "Low" => EPriorityLevel.Low, _ => EPriorityLevel.Medium };
        DateTime.TryParse(deadlineStr, out var deadline);

        var dto = new CreateTaskDto
        {
            Title = title,
            Description = description,
            AssignedTo = assignee,
            CreatedBy = context.CurrentUserId!,
            DueDate = deadline,
            Priority = priority,
            TeamId = teamId,
            ClanIds = new List<string> { context.ClanId! },
            ChannelIds = new List<string> { context.ChannelId! }
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

        if (!await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value))
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền cập nhật");

        var title = ReadValue(context.Payload, "title");
        var description = ReadValue(context.Payload, "description");
        var priorityStr = ReadValue(context.Payload, "priority");
        var statusStr = ReadValue(context.Payload, "status");
        var deadlineStr = ReadValue(context.Payload, "deadline");
        var assignee = ReadValue(context.Payload, "assignee");

        var newStatus = ParseStatus(statusStr);

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
            AssignedTo = string.IsNullOrWhiteSpace(assignee) ? null : assignee
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
        if (task.AssignedTo != context.CurrentUserId) return BuildTextResponse(context, "❌ Chỉ được cập nhật task của mình");

        var statusStr = ReadValue(context.Payload, "status");
        var newStatus = ParseStatus(statusStr);
        if (newStatus == null)
            return BuildTextResponse(context, "❌ Trạng thái không hợp lệ");

        if (!IsValidMemberTransition(task.Status, newStatus.Value))
            return BuildTextResponse(context, $"❌ Không thể chuyển từ {task.Status} → {newStatus}");

        await _taskService.ChangeStatusAsync(taskId, newStatus.Value, ct);
        return HandleCancel(context, $"✅ Đã cập nhật trạng thái task #{taskId}");
    }

    private async Task<ComponentResponse> HandleConfirmDeleteAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var taskId))
            return BuildTextResponse(context, "❌ Task ID không hợp lệ");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null) return BuildTextResponse(context, "❌ Không tìm thấy task");

        if (!await _teamService.IsPM(context.CurrentUserId!, task.TeamId!.Value))
            return BuildTextResponse(context, "❌ Chỉ Mentor mới có quyền xóa");

        await _taskService.DeleteAsync(taskId, ct);
        return HandleCancel(context, $"✅ Đã xóa task #{taskId}");
    }

    //private async Task<ComponentResponse> HandleFilterStatusAsync(ComponentContext context, CancellationToken ct)
    //{
    //    var (tasks, isMentor) = await GetUserTasksAsync(context.CurrentUserId!, ct);
    //    var filtered = tasks.Where(t => t.Status == ETaskStatus.Doing || t.Status == ETaskStatus.Review).ToList();
    //    return ReplaceForm(context, TaskFormBuilder.BuildTaskList(filtered, isMentor));
    //}

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

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithSelectedProject(
                projects,
                projectId,
                teams,
                members
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

        return ReplaceForm(context,
            TaskFormBuilder.BuildCreateFormWithSelectedProject(
                projects,
                projectId,
                teams,
                members,
                teamId
            ));
    }

    //private async Task<ComponentResponse> HandleFilterUserAsync(ComponentContext context, CancellationToken ct)
    //{
    //    var (tasks, isMentor) = await GetUserTasksAsync(context.CurrentUserId!, ct);
    //    var filtered = tasks.Where(t => t.AssignedTo == context.CurrentUserId).ToList();
    //    return ReplaceForm(context, TaskFormBuilder.BuildTaskList(filtered, isMentor));
    //}

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
        var originalMessageId = parts.Length >= 2 ? parts[^1] : null;

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
                Id = originalMessageId,
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
        var originalMessageId = context.MessageId;

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
                    Id = originalMessageId,
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
        var originalMessageId = parts.Length >= 2 ? parts[^1] : null;

        return ComponentResponse.FromText(
            context.ClanId!,
            context.ChannelId!,
            text,
            context.Mode,
            context.IsPublic,

            //  reply vào message gốc
            originalMessageId,

            new ChannelMessage
            {
                Id = originalMessageId,
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
                Id = originalMessageId,
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
            AssignedTo = GetDisplayName(task.AssignedTo, clanId), 
            CreatedBy = GetDisplayName(task.CreatedBy, clanId),
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
                    return val.Value.GetString();
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

    private static string ReadValue(JsonElement payload, string key)
    {
        var valuesNode = ComponentPayloadHelper.GetValues(payload);
        var value = ComponentPayloadHelper.GetPropertyIgnoreCase(valuesNode, key)?.GetString();
        if (!string.IsNullOrWhiteSpace(value)) return value;
        var extraData = ComponentPayloadHelper.GetExtraData(payload);
        if (string.IsNullOrWhiteSpace(extraData) || !extraData.TrimStart().StartsWith("{")) return string.Empty;
        try
        {
            using var json = JsonDocument.Parse(extraData);
            return ComponentPayloadHelper.GetPropertyIgnoreCase(json.RootElement, key)?.GetString() ?? string.Empty;
        }
        catch { return string.Empty; }
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
            (ETaskStatus.ToDo, ETaskStatus.Doing) => true,
            (ETaskStatus.Doing, ETaskStatus.Review) => true,
            (ETaskStatus.Review, ETaskStatus.Completed) => true,
            _ => false
        };
    }

    private static EPriorityLevel? ParsePriority(string? value) => value switch { "High" => EPriorityLevel.High, "Medium" => EPriorityLevel.Medium, "Low" => EPriorityLevel.Low, _ => null };
    private static ETaskStatus? ParseStatus(string? value) => value switch { "ToDo" => ETaskStatus.ToDo, "Doing" => ETaskStatus.Doing, "Review" => ETaskStatus.Review, "Completed" => ETaskStatus.Completed, "Cancelled" => ETaskStatus.Cancelled, _ => null };

}
