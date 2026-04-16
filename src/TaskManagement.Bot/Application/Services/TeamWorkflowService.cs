using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services;

public class TeamWorkflowService : ITeamWorkflowService
{
    private static readonly TimeSpan RequestLifetime = TimeSpan.FromMinutes(30);

    private readonly ILogger<TeamWorkflowService> _logger;
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly IPendingTeamRequestService _pendingTeamRequestService;

    public TeamWorkflowService(
        ILogger<TeamWorkflowService> logger,
        IProjectService projectService,
        ITeamService teamService,
        IPendingTeamRequestService pendingTeamRequestService)
    {
        _logger = logger;
        _projectService = projectService;
        _teamService = teamService;
        _pendingTeamRequestService = pendingTeamRequestService;
    }

    public async Task<CreateTeamRequestResult> CreateRequestAsync(CreateTeamRequestInput input, CancellationToken cancellationToken = default)
    {
        var projectName = input.ProjectName.Trim();
        var teamName = input.TeamName.Trim();
        var members = input.Members
            .Where(x => !string.Equals(x.UserId, input.PMUserId, StringComparison.Ordinal))
            .DistinctBy(x => x.UserId)
            .ToList();

        if (members.Count == 0)
        {
            return new CreateTeamRequestResult
            {
                Success = false,
                Message = "Khong co thanh vien hop le de tao team"
            };
        }

        if (await _projectService.ExistsByNameAsync(projectName, cancellationToken))
        {
            return new CreateTeamRequestResult
            {
                Success = false,
                Message = $"Project `{projectName}` da ton tai"
            };
        }

        if (await _teamService.ExistsByNameAsync(teamName, cancellationToken))
        {
            return new CreateTeamRequestResult
            {
                Success = false,
                Message = $"Team `{teamName}` da ton tai"
            };
        }

        var requestId = Guid.NewGuid().ToString("N");
        var request = new PendingTeamRequest
        {
            MessageId = requestId,
            ProjectName = projectName,
            TeamName = teamName,
            PMUserId = input.PMUserId,
            MemberUserIds = members.Select(x => x.UserId).ToList(),
            AcceptedUserIds = [],
            CreatedAt = DateTime.UtcNow,
            SenderId = input.PMUserId
        };

        await _pendingTeamRequestService.AddAsync(request, cancellationToken);

        _logger.LogInformation(
            "[TEAM_REQUEST] Created request {RequestId} for project {ProjectName} team {TeamName}",
            requestId,
            projectName,
            teamName);

        return new CreateTeamRequestResult
        {
            Success = true,
            Message = $"Da gui yeu cau tao team `{teamName}`. Cho thanh vien xac nhan.",
            RequestId = requestId,
            ProjectName = projectName,
            TeamName = teamName,
            Members = members
        };
    }

    public Task<TeamRequestActionResult> AcceptAsync(string requestId, string expectedUserId, string currentUserId, CancellationToken cancellationToken = default)
    {
        return ProcessDecisionAsync(requestId, expectedUserId, currentUserId, accepted: true, cancellationToken);
    }

    public Task<TeamRequestActionResult> RejectAsync(string requestId, string expectedUserId, string currentUserId, CancellationToken cancellationToken = default)
    {
        return ProcessDecisionAsync(requestId, expectedUserId, currentUserId, accepted: false, cancellationToken);
    }

    private async Task<TeamRequestActionResult> ProcessDecisionAsync(
        string requestId,
        string expectedUserId,
        string currentUserId,
        bool accepted,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(expectedUserId, currentUserId, StringComparison.Ordinal))
        {
            return new TeamRequestActionResult
            {
                Success = false,
                Message = "Ban khong co quyen xu ly yeu cau nay"
            };
        }

        var request = await _pendingTeamRequestService.GetAsync(requestId, cancellationToken);
        if (request == null)
        {
            return new TeamRequestActionResult
            {
                Success = false,
                Message = "Yeu cau da het han hoac khong ton tai"
            };
        }

        if (request.CreatedAt < DateTime.UtcNow.Subtract(RequestLifetime))
        {
            await _pendingTeamRequestService.RemoveAsync(requestId, cancellationToken);
            return new TeamRequestActionResult
            {
                Success = false,
                Message = "Yeu cau da het han"
            };
        }

        if (!request.MemberUserIds.Contains(currentUserId, StringComparer.Ordinal))
        {
            return new TeamRequestActionResult
            {
                Success = false,
                Message = "Ban khong nam trong danh sach can xac nhan"
            };
        }

        if (!accepted)
        {
            await _pendingTeamRequestService.RemoveAsync(requestId, cancellationToken);
            return new TeamRequestActionResult
            {
                Success = true,
                Message = $"<@{currentUserId}> da tu choi. Yeu cau tao team bi huy."
            };
        }

        if (request.AcceptedUserIds.Contains(currentUserId, StringComparer.Ordinal))
        {
            return new TeamRequestActionResult
            {
                Success = true,
                Message = $"<@{currentUserId}> da xac nhan truoc do."
            };
        }

        request.AcceptedUserIds.Add(currentUserId);

        if (request.AcceptedUserIds.Count < request.MemberUserIds.Count)
        {
            await _pendingTeamRequestService.UpdateAsync(request, cancellationToken);
            return new TeamRequestActionResult
            {
                Success = true,
                Message = $"<@{currentUserId}> da xac nhan ({request.AcceptedUserIds.Count}/{request.MemberUserIds.Count})."
            };
        }

        if (await _projectService.ExistsByNameAsync(request.ProjectName, cancellationToken))
        {
            await _pendingTeamRequestService.RemoveAsync(requestId, cancellationToken);
            return new TeamRequestActionResult
            {
                Success = false,
                Message = $"Project `{request.ProjectName}` da ton tai. Huy yeu cau."
            };
        }

        if (await _teamService.ExistsByNameAsync(request.TeamName, cancellationToken))
        {
            await _pendingTeamRequestService.RemoveAsync(requestId, cancellationToken);
            return new TeamRequestActionResult
            {
                Success = false,
                Message = $"Team `{request.TeamName}` da ton tai. Huy yeu cau."
            };
        }

        var project = await _projectService.CreateProjectAsync(request.ProjectName, string.Empty, request.PMUserId, cancellationToken);
        await _teamService.CreateTeamAsync(
            project.Id,
            request.TeamName,
            request.PMUserId,
            request.MemberUserIds,
            memberStatus: "Accepted",
            cancellationToken: cancellationToken);

        await _pendingTeamRequestService.RemoveAsync(requestId, cancellationToken);

        return new TeamRequestActionResult
        {
            Success = true,
            TeamCreated = true,
            Message = $"Team `{request.TeamName}` da tao thanh cong."
        };
    }
}
