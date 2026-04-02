namespace TaskManagement.Bot.Features.TaskQuery.Commands;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.TaskQuery.Services;

/// <summary>
/// Task List Command - Người 2: /task list command
/// </summary>
public class TaskListCommand : ICommand
{
    private readonly ITaskSearchService _searchService;

    public string CommandName => "task-list";

    public TaskListCommand(ITaskSearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<string> ExecuteAsync(string[] args)
    {
        int page = args.Length > 0 && int.TryParse(args[0], out int p) ? p : 1;
        var tasks = await _searchService.ListAsync(page);

        if (!tasks.Any())
            return "No tasks found.";

        var result = "📋 Tasks:\n";
        foreach (var task in tasks)
        {
            result += $"  • [{task.Id}] {task.Title} - {task.Status}\n";
        }
        return result;
    }
}
