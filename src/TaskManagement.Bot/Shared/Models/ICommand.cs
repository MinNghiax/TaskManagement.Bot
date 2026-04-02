namespace TaskManagement.Bot.Shared.Models;

/// <summary>
/// Base command interface
/// </summary>
public interface ICommand
{
    string CommandName { get; }
    Task<string> ExecuteAsync(string[] args);
}
