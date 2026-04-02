namespace TaskManagement.Bot.Features.Task.Commands;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.Task.Services;
using TaskManagement.Bot.Features.Task.Models;

/// <summary>
/// Task Create Command - Người 1: /task create command
/// </summary>
public class TaskCreateCommand : ICommand
{
    private readonly ITaskService _taskService;

    public string CommandName => "task-create";

    public TaskCreateCommand(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public async Task<string> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
            return "Usage: /task create <title>";

        var dto = new CreateTaskDto { Title = string.Join(" ", args) };
        var task = await _taskService.CreateAsync(dto);
        return $"✅ Task created: {task.Title} (ID: {task.Id})";
    }
}
