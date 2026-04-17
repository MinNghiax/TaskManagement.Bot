using System.Collections.Concurrent;

namespace TaskManagement.Bot.Application.Services;

/// <summary>
/// Service to manage temporary state for report flow (Project/Team selection)
/// </summary>
public class ReportStateService
{
    private readonly ConcurrentDictionary<string, ReportState> _states = new();
    private readonly TimeSpan _stateExpiration = TimeSpan.FromMinutes(10);

    public void SetSelectedProject(string userId, int projectId, string projectName)
    {
        _states[userId] = new ReportState
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Timestamp = DateTime.UtcNow
        };
    }

    public ReportState? GetState(string userId)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            // Check if state is expired
            if (DateTime.UtcNow - state.Timestamp < _stateExpiration)
            {
                return state;
            }

            // Remove expired state
            _states.TryRemove(userId, out _);
        }

        return null;
    }

    public void ClearState(string userId)
    {
        _states.TryRemove(userId, out _);
    }

    public void CleanupExpiredStates()
    {
        var expiredKeys = _states
            .Where(kvp => DateTime.UtcNow - kvp.Value.Timestamp >= _stateExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _states.TryRemove(key, out _);
        }
    }
}

public class ReportState
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? TeamId { get; set; }
    public DateTime Timestamp { get; set; }
}
