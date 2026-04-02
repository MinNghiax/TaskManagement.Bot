namespace TaskManagement.Bot.Features.ThreadContext.Commands;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.ThreadContext.Services;
using TaskManagement.Bot.Features.ThreadContext.Models;

/// <summary>
/// Task Create Here Command - Pessoa 4: /task here command
/// </summary>
public class TaskCreateHereCommand : ICommand
{
    private readonly IThreadContextService _threadContextService;

    public string CommandName => "task-create-here";

    public TaskCreateHereCommand(IThreadContextService threadContextService)
    {
        _threadContextService = threadContextService;
    }

    public async Task<string> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
            return "Usage: /task here <task_id>";

        if (!int.TryParse(args[0], out int taskId))
            return "Invalid task ID";

        // This would normally get the current chat context
        var dto = new CreateTaskContextDto
        {
            TaskId = taskId,
            ThreadId = "current_thread",
            ChannelId = "current_channel"
        };

        await _threadContextService.BindTaskToThreadAsync(dto);
        return $"✅ Task {taskId} bound to this thread";
    }
}
