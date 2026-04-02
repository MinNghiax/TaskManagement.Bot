namespace TaskManagement.Bot.Shared.Events;

/// <summary>
/// Base event handler for Mezon SDK events
/// </summary>
public abstract class EventHandler
{
    public abstract string EventType { get; }
    public abstract Task HandleAsync(dynamic eventData);
}
