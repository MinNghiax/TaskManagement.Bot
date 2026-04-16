namespace TaskManagement.Bot.Application.Services;

public interface ITeamWorkflowService
{
    Task<CreateTeamRequestResult> CreateRequestAsync(CreateTeamRequestInput input, CancellationToken cancellationToken = default);
    Task<TeamRequestActionResult> AcceptAsync(string requestId, string expectedUserId, string currentUserId, CancellationToken cancellationToken = default);
    Task<TeamRequestActionResult> RejectAsync(string requestId, string expectedUserId, string currentUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateTeamRequestInput
{
    public required string ProjectName { get; init; }
    public required string TeamName { get; init; }
    public required string PMUserId { get; init; }
    public required IReadOnlyList<TeamRequestMember> Members { get; init; }
}

public sealed record TeamRequestMember
{
    public required string UserId { get; init; }
    public required string Handle { get; init; }
}

public sealed record CreateTeamRequestResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public string? RequestId { get; init; }
    public string? ProjectName { get; init; }
    public string? TeamName { get; init; }
    public IReadOnlyList<TeamRequestMember> Members { get; init; } = [];
}

public sealed record TeamRequestActionResult
{
    public bool Success { get; init; }
    public bool TeamCreated { get; init; }
    public required string Message { get; init; }
}
