using System.Collections.Concurrent;
using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Services;

public class ReportStateService
{
    private readonly ConcurrentDictionary<string, ReportState> _states = new();
    private readonly TimeSpan _stateExpiration = TimeSpan.FromMinutes(10);

    public void SetSelectedProject(string userId, int projectId, string projectName, string originalMessageId, ChannelMessage? originalMessage = null)
    {
        var existingState = GetState(userId);
        _states[userId] = new ReportState
        {
            ProjectId = projectId,
            ProjectName = projectName,
            OriginalMessageId = originalMessageId,
            OriginalMessage = originalMessage ?? existingState?.OriginalMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    public void InitializeState(string userId, string originalMessageId, ChannelMessage? originalMessage = null)
    {
        _states[userId] = new ReportState
        {
            OriginalMessageId = originalMessageId,
            OriginalMessage = originalMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    public ReportState? GetState(string userId)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            if (DateTime.UtcNow - state.Timestamp < _stateExpiration)
            {
                return state;
            }

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
    public string OriginalMessageId { get; set; } = string.Empty;
    public ChannelMessage? OriginalMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
