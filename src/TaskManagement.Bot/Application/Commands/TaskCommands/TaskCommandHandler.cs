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
            "view" => await HandleViewAsync(message, message.SenderId, ct),
            "update" => await HandleUpdateEntry(parts, message, ct),
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

    private async Task<CommandResponse> HandleListAsync(
    string[] parts,
    string? userId,
    string? clanId,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định user");

        var teams = await _teamService.GetTeamsByMemberAsync(userId);

        var tasks = new List<TaskDto>();

        foreach (var team in teams)
        {
            // chỉ lấy task của user
            var myTasks = await _taskService.GetByAssigneeAndTeamAsync(userId, team.Id, ct);
            tasks.AddRange(myTasks);
        }

        // remove duplicate
        tasks = tasks
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();

        //  FILTER STATUS 
        if (parts.Length >= 4 && parts[2].ToLower() == "status")
        {
            var status = ParseStatusFilter(parts[3]);

            if (status == null)
                return new CommandResponse("❌ Status không hợp lệ (todo | doing | review | completed | cancelled)");

            tasks = tasks
                .Where(t => t.Status == status.Value)
                .ToList();
        }

        // map displayName
        var mappedTasks = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            AssignedTo = GetDisplayName(t.AssignedTo, clanId!),
            CreatedBy = GetDisplayName(t.CreatedBy, clanId!),
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            TeamId = t.TeamId
        }).ToList();

        var content = TaskFormBuilder.BuildTaskList(mappedTasks, userId, teams);

        return new CommandResponse(content);
    }

    private async Task<CommandResponse> HandleViewAsync(ChannelMessage message, string? userId, CancellationToken ct)
    {
        var projects = await _projectService.GetProjectsByMemberAsync(userId);

        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project");

        return new CommandResponse(
            TaskFormBuilder.BuildViewSelectProject(projects, message.Id)
        );
    }

    private async Task<CommandResponse> HandleUpdateEntry(
        string[] parts,
        ChannelMessage message,
        CancellationToken ct)
    {
        var userId = message.SenderId;

        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định user");

        var mode = parts.Length >= 3 ? parts[2].ToLower() : "auto";

        var teams = await _teamService.GetTeamsByMemberAsync(userId);

        if (teams.Count == 0)
            return new CommandResponse("❌ Bạn chưa thuộc team nào");

        if (mode == "pm")
        {
            bool isPM = false;

            foreach (var team in teams)
            {
                if (await _teamService.IsPM(userId, team.Id))
                {
                    isPM = true;
                    break;
                }
            }

            if (!isPM)
                return new CommandResponse("❌ Bạn không phải PM");

            var projects = await _projectService.GetProjectsByUserAsync(userId);

            if (projects.Count == 0)
                return new CommandResponse("❌ Bạn chưa có project");

            return new CommandResponse(
                TaskFormBuilder.BuildUpdateSelectProject(projects, message.Id)
            );
        }

        if (mode == "member")
        {
            var tasks = new List<TaskDto>();

            foreach (var team in teams)
            {
                var myTasks = await _taskService.GetByAssigneeAndTeamAsync(userId, team.Id, ct);
                tasks.AddRange(myTasks);
            }

            tasks = tasks
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            if (tasks.Count == 0)
                return new CommandResponse("❌ Bạn chưa có task nào");

            return new CommandResponse(
                TaskFormBuilder.BuildMemberUpdateTaskSelect(tasks, message.Id)
            );
        }

        if (mode == "my")
        {
            var tasks = new List<TaskDto>();

            foreach (var team in teams)
            {
                var teamTasks = await _taskService.GetTasksByTeamAsync(team.Id, ct);

                //  chỉ lấy task do PM tạo
                var myCreatedTasks = teamTasks
                    .Where(t => t.CreatedBy == userId)
                    .ToList();

                tasks.AddRange(myCreatedTasks);
            }

            tasks = tasks
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            if (tasks.Count == 0)
                return new CommandResponse("❌ Bạn chưa tạo task nào");

            return new CommandResponse(
                TaskFormBuilder.BuildMemberUpdateTaskSelect(tasks, message.Id)
            );
        }

        return new CommandResponse("❗ Dùng:\n- !task update pm\n- !task update member");
    }

    private async Task<CommandResponse> HandleDeletePrompt(string[] parts, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CommandResponse("❌ Không xác định user");

        // lấy project của PM
        var projects = await _projectService.GetProjectsByUserAsync(userId);

        if (projects.Count == 0)
            return new CommandResponse("❌ Bạn chưa có project nào");

        return new CommandResponse(
            TaskFormBuilder.BuildDeleteSelectProject(projects, Guid.NewGuid().ToString())
        );
    }

    private static string GetHelpText() => """
        📝 **Quản lý Task**
        
        `!task create` - Tạo task mới
        `!task list status <status>` - Xem danh sách task theo trạng thái
        `!task list` - Xem danh sách task theo user
        `!task update` - Cập nhật task
            `!task update pm` - Cập nhật task theo pm
            `!task update member` - Cập nhật task theo member
        `!task delete` - Xóa task (Mentor)
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

    private static ETaskStatus? ParseStatusFilter(string? value) => value?.ToLower() switch
    {
        "todo" => ETaskStatus.ToDo,
        "doing" => ETaskStatus.Doing,
        "review" => ETaskStatus.Review,
        "completed" => ETaskStatus.Completed,
        "cancelled" => ETaskStatus.Cancelled,
        _ => null
    };
}
