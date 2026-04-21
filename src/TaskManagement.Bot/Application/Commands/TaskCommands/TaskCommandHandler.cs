using Mezon.Sdk.Domain;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands;

public class TaskCommandHandler : ICommandHandler
{
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly ITaskService _taskService;

    public TaskCommandHandler(
        IProjectService projectService,
        ITeamService teamService,
        ITaskService taskService)
    {
        _projectService = projectService;
        _teamService = teamService;
        _taskService = taskService;
    }

    public bool CanHandle(string command) =>
        command.StartsWith("!task", StringComparison.OrdinalIgnoreCase);

    public async Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken ct)
    {
        var content = ParseContent(message.Content?.Text);
        if (string.IsNullOrWhiteSpace(content))
            return new CommandResponse("❌ Empty command");

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return new CommandResponse(GetHelpText());

        return parts[1].ToLowerInvariant() switch
        {
            "create" or "form" => await HandleCreateAsync(message.SenderId, ct),
            "list" => await HandleListAsync(message.SenderId, ct),
            "update" => await HandleUpdateAsync(parts, message.SenderId, ct),
            "delete" => await HandleDeletePrompt(parts, message.SenderId, ct),
            _ => new CommandResponse(GetHelpText())
        };
    }

    private async Task<CommandResponse> HandleCreateAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định được người dùng");

        var projects = await _projectService.GetAllProjectsAsync();
        var teams = await _teamService.GetAllAsync(); 
        var members = await _teamService.GetAllMembersAsync();
        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project nào. Hãy tạo project bằng `!team init`");

        return new CommandResponse(TaskFormBuilder.BuildFullCreateForm(projects, teams, members));
    }

    private async Task<CommandResponse> HandleListAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định người dùng");

        var allTasks = await _taskService.GetAllAsync(ct);

        if (allTasks.Count == 0)
            return new CommandResponse("📭 Không có task nào");

        var isMentor = await _teamService.IsUserPMInAnyTeam(userId);

        List<TaskDto> tasks;

        if (isMentor)
        {
            tasks = allTasks;
        }
        else
        {
            tasks = allTasks
                .Where(t => t.AssignedTo == userId)
                .ToList();
        }

        var content = TaskFormBuilder.BuildTaskList(tasks, isMentor);

        return new CommandResponse(content);
    }

    private async Task<CommandResponse> HandleUpdateAsync(
    string[] parts,
    string? userId,
    CancellationToken ct)
    {
        if (parts.Length < 3 || !int.TryParse(parts[2], out var taskId))
            return new CommandResponse("❌ Dùng: `!task update <taskId>`");

        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định người dùng");

        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
            return new CommandResponse("❌ Không tìm thấy task");

        var isMentor = task.TeamId.HasValue &&
                       await _teamService.IsPM(userId, task.TeamId.Value);

        var isAssignee = task.AssignedTo == userId;

        if (!isMentor && !isAssignee)
            return new CommandResponse("❌ Bạn không có quyền sửa task này");

        if (isMentor)
        {
            var members = task.TeamId.HasValue
                ? await _teamService.GetMembers(task.TeamId.Value)
                : new List<string>();

            return new CommandResponse(
                TaskFormBuilder.BuildUpdateFormForMentor(task, members)
            );
        }

        return new CommandResponse(
            TaskFormBuilder.BuildUpdateFormForMember(task)
        );
    }

    private async Task<CommandResponse> HandleDeletePrompt(string[] parts, string? userId, CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || !int.TryParse(parts[2], out var taskId))
            return new CommandResponse("❌ Dùng: `!task delete <taskId>`");

        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định được người dùng");

        var task = await _taskService.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
            return new CommandResponse("❌ Không tìm thấy task");

        var isMentor = task.TeamId.HasValue &&
                       await _teamService.IsPM(userId, task.TeamId.Value);

        if (!isMentor)
            return new CommandResponse("❌ Chỉ Mentor mới được dùng lệnh này");

        return new CommandResponse(
            TaskFormBuilder.BuildDeleteConfirm(task)
        );
    }

    private static string GetHelpText() => """
        📝 **Quản lý Task**
        
        `!task create` - Tạo task mới
        `!task list` - Xem danh sách task
        `!task update <id>` - Cập nhật task
        `!task delete <id>` - Xóa task (Mentor)
        """;

    private static string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!raw.StartsWith("{"))
            return raw;

        try
        {
            using var json = JsonDocument.Parse(raw);
            return json.RootElement.GetProperty("t").GetString();
        }
        catch
        {
            return raw;
        }
    }
}
