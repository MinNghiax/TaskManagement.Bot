using Mezon.Sdk;
using Mezon.Sdk.Domain;
using System.Linq;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands;

public class TaskCommandHandler : ICommandHandler
{
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly ITaskService _taskService;
    private readonly MezonClient _client;

    public TaskCommandHandler(
        IProjectService projectService,
        ITeamService teamService,
        ITaskService taskService,
        MezonClient client)
    {
        _projectService = projectService;
        _teamService = teamService;
        _taskService = taskService;
        _client = client;
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
            "create" => await HandleCreateAsync(message, message.SenderId, ct),
            "list" => await HandleListAsync(parts, message.SenderId, message.ClanId, ct),
            "view" => await HandleViewAsync(message, ct),
            "update" => await HandleUpdateAsync(parts, message.SenderId, message.ClanId, ct),
            "delete" => await HandleDeletePrompt(parts, message.SenderId, ct),
            _ => new CommandResponse(GetHelpText())
        };
    }

    private async Task<CommandResponse> HandleCreateAsync(ChannelMessage message, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định được người dùng");

        var projects = await _projectService.GetProjectsByUserAsync(userId);
        var teams = await _teamService.GetAllAsync();
        var members = await _teamService.GetAllMembersAsync();
        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project nào. Hãy tạo project bằng `!team init`");

        return new CommandResponse(TaskFormBuilder.BuildSelectProject(projects, message.Id));
    }

    private async Task<CommandResponse> HandleListAsync(string[] parts, string? userId, string? clanId, CancellationToken ct)
    {
        // Lấy team của user
        var teams = await _teamService.GetTeamsByMemberAsync(userId);

        // Check role

        var tasks = new List<TaskDto>();

        foreach (var team in teams)
        {
            //  luôn chỉ lấy task của chính user
            var myTasks = await _taskService.GetByAssigneeAsync(userId, team.Id.ToString(), ct);
            tasks.AddRange(myTasks);
        }

        // remove duplicate
        tasks = tasks
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();

        var content = TaskFormBuilder.BuildTaskList(tasks, userId, teams);

        return new CommandResponse(content);
    }

    private async Task<CommandResponse> HandleViewAsync(ChannelMessage message, CancellationToken ct)
    {
        var projects = await _projectService.GetAllProjectsAsync();

        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project");

        return new CommandResponse(
            TaskFormBuilder.BuildViewSelectProject(projects, message.Id)
        );
    }

    private async Task<CommandResponse> HandleUpdateAsync(
    string[] parts,
    string? userId,
    string? clanId,
    CancellationToken ct)
    {
        //var projects = await _projectService.GetAllProjectsAsync();
        var projects = await _projectService.GetProjectsByUserAsync(userId);

        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project");

        return new CommandResponse(
            TaskFormBuilder.BuildUpdateSelectProject(projects, Guid.NewGuid().ToString())
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

        //  CHECK ROLE NGAY TỪ COMMAND
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
        `!task list` - Xem danh sách tất cả task
        `!task list status <status>` - Xem danh sách task theo trạng thái
        `!task list user <id>` - Xem danh sách task theo user
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
