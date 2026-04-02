namespace TaskManagement.Bot.Features.Reminder.Commands;

using TaskManagement.Bot.Shared.Models;
using TaskManagement.Bot.Features.Reminder.Services;
using TaskManagement.Bot.Features.Reminder.Models;

/// <summary>
/// Reminder Set Command - Người 3: /reminder set command
/// </summary>
public class ReminderSetCommand : ICommand
{
    private readonly IReminderService _reminderService;

    public string CommandName => "reminder-set";

    public ReminderSetCommand(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    public async Task<string> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
            return "Usage: /reminder set <task_id> <time>";

        if (!int.TryParse(args[0], out int taskId))
            return "Invalid task ID";

        var dto = new CreateReminderDto
        {
            TaskId = taskId,
            ReminderTime = DateTime.UtcNow.AddHours(1),
            RepeatType = ReminderRepeatType.Once
        };

        var reminder = await _reminderService.CreateAsync(dto);
        return $"⏰ Reminder set for task {taskId}";
    }
}
