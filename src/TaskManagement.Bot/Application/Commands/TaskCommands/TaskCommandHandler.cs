using Mezon.Sdk;
using Mezon.Sdk.Domain;
using System.Linq;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Entities;
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
            "delete" => await HandleDeletePrompt(message, ct),
            _ => new CommandResponse(GetHelpText())
        };
    }

    private async Task<CommandResponse> HandleCreateAsync(ChannelMessage message, string? userId, CancellationToken ct)
    {
        var projects = await _projectService.GetProjectsByUserAsync(userId!);
        if (projects.Count == 0)
            return new CommandResponse("❌ Chưa có project nào. Hãy tạo project bằng `!team init`");

        Console.WriteLine($"[HandleCreateAsync] message.Id = {message.Id}");
        Console.WriteLine($"[HandleCreateAsync] Is GUID? {Guid.TryParse(message.Id, out _)}");

        return new CommandResponse(TaskFormBuilder.BuildSelectProject(projects, message.Id, userId!, "create"));
    }

    private async Task<CommandResponse> HandleListAsync(
    string[] parts,
    string? userId,
    string? clanId,
    CancellationToken ct)
    {
        var teams = await _teamService.GetTeamsByMemberAsync(userId!);

        if (teams.Count == 0)
            return new CommandResponse("❌ Bạn chưa thuộc team nào");

        //  Lấy tất cả tasks của user trong 1 query thay vì loop
        var teamIds = teams.Select(t => t.Id).ToList();
        var tasks = await _taskService.GetByAssigneeAndTeamsAsync(userId!, teamIds, ct);

        //  FILTER STATUS 
        if (parts.Length >= 4 && parts[2].ToLower() == "status")
        {
            var status = ParseStatusFilter(parts[3]);

            if (status == null)
                return new CommandResponse("❌ Status không hợp lệ (todo | doing | review | late | completed | cancelled)");

            tasks = tasks
                .Where(t => t.Status == status.Value)
                .ToList();
        }

        //  Cache displayName để tránh gọi nhiều lần
        var displayNameCache = new Dictionary<string, string>();
        
        string GetCachedDisplayName(string uid)
        {
            if (!displayNameCache.TryGetValue(uid, out var name))
            {
                name = GetDisplayName(uid, clanId!);
                displayNameCache[uid] = name;
            }
            return name;
        }

        // map displayName
        var mappedTasks = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            AssignedTo = GetCachedDisplayName(t.AssignedTo!),
            CreatedBy = GetCachedDisplayName(t.CreatedBy!),
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            TeamId = t.TeamId
        }).ToList();

        //  Chỉ lấy projects liên quan đến tasks
        var projectIds = teams.Select(t => t.ProjectId).Distinct().ToList();
        var projects = await _projectService.GetProjectsByIdsAsync(projectIds);
        var displayName = GetCachedDisplayName(userId!);

        var content = TaskFormBuilder.BuildTaskList(mappedTasks, displayName, clanId!, teams, projects);

        return new CommandResponse(content);
    }

    private async Task<CommandResponse> HandleViewAsync(ChannelMessage message, string? userId, CancellationToken ct)
    {
        var projects = await _projectService.GetProjectsByMemberAsync(userId!);

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

        var mode = parts.Length >= 3 ? parts[2].ToLower() : "";

        if (mode != "pm" && mode != "member")
        {
            return new CommandResponse("""
                ❗ Dùng:
                - !task update pm
                - !task update member
                """);
        }

        var teams = await _teamService.GetTeamsByMemberAsync(userId!);

        if (teams.Count == 0)
            return new CommandResponse("❌ Bạn chưa thuộc team nào");

        if (mode == "pm")
        {
            // 1 query duy nhất thay vì N queries
            var pmTeams = await _teamService.GetPMTeamsByUserAsync(userId!);

            if (pmTeams.Count == 0)
                return new CommandResponse("❌ Bạn không phải PM của team nào");

            // Lấy projects từ các team mà user là PM
            var projectIds = pmTeams.Select(t => t.ProjectId).Distinct().ToList();
            //  Query trực tiếp từ database
            var projects = await _projectService.GetProjectsByIdsAsync(projectIds);

            if (projects.Count == 0)
                return new CommandResponse("❌ Bạn chưa có project");

            return new CommandResponse(
                TaskFormBuilder.BuildUpdateSelectProject(projects, message.Id, userId)
            );
        }

        // mode == "member"
        var memberProjects = await _projectService.GetProjectsByMemberAsync(userId!);

        if (memberProjects.Count == 0)
            return new CommandResponse("❌ Bạn chưa có project nào");

        return new CommandResponse(
            TaskFormBuilder.BuildMemberUpdateSelectProject(memberProjects, message.Id, userId)
        );
    }

    private async Task<CommandResponse> HandleDeletePrompt(ChannelMessage message, CancellationToken ct)
    {
        var userId = message.SenderId;

        // lấy project của PM
        var projects = await _projectService.GetProjectsByUserAsync(userId!);

        if (projects.Count == 0)
            return new CommandResponse("❌ Bạn chưa có project nào");

        return new CommandResponse(
            TaskFormBuilder.BuildDeleteSelectProject(projects, message.Id, userId)
        );
    }

    private static string GetHelpText() => """
        📝 **Quản lý Task**
        
        `!team init` - Tạo Project và Team
        `!task create` - Tạo task mới
        `!task list` - Xem danh sách task theo user
        `!task list status <status>` - Xem danh sách task theo trạng thái
            Status: todo | doing | review | late | completed | cancelled 
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
        "late" => ETaskStatus.Late,
        "completed" => ETaskStatus.Completed,
        "cancelled" => ETaskStatus.Cancelled,
        _ => null
    };
}
