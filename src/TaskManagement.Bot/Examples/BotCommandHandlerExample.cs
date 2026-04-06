using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mezon.Sdk;
using Mezon.Sdk.Proto;
using TaskManagement.Bot.Application.Services;
using TaskStatus = TaskManagement.Bot.Infrastructure.Enums.TaskStatus;

namespace TaskManagement.Bot.Examples;

/// <summary>
/// Example: Extended BotService with command handling
/// 
/// This example shows how to:
/// - Parse incoming messages for commands
/// - Call TaskService to create/manage tasks
/// - Send bot replies back to the channel
/// - Handle errors gracefully
/// 
/// USAGE: Implement this logic in BotService.OnChannelMessage()
/// </summary>
public class BotCommandHandlerExample
{
    private readonly ILogger<BotCommandHandlerExample> _logger;
    private readonly ITaskService _taskService;
    private readonly IConfiguration _configuration;

    public BotCommandHandlerExample(
        ILogger<BotCommandHandlerExample> logger,
        ITaskService taskService,
        IConfiguration configuration)
    {
        _logger = logger;
        _taskService = taskService;
        _configuration = configuration;
    }

    /// <summary>
    /// Example: Process incoming message and handle commands
    /// 
    /// Supported commands:
    /// - !task create [title] [description] - Create a new task
    /// - !task list - List all tasks
    /// - !task done [taskId] - Mark task as complete
    /// - !task delete [taskId] - Delete a task
    /// - !help - Show available commands
    /// </summary>
    public async Task ProcessMessageAsync(
        ChannelMessage message,
        MezonClient client,
        CancellationToken ct = default)
    {
        try
        {
            // Ignore messages from bot itself
            if (message.Username.Contains("bot", StringComparison.OrdinalIgnoreCase))
                return;

            // Log received message
            _logger.LogInformation($"📥 Message from {message.Username}: {message.Content}");

            // Extract text from JSON format if needed
            var content = message.Content?.TrimStart('{').TrimEnd('}');
            if (content?.Contains("\"t\":") == true)
            {
                var jsonStart = content.IndexOf("\"t\":\"") + 5;
                var jsonEnd = content.LastIndexOf("\"");
                if (jsonStart > 4 && jsonEnd > jsonStart)
                {
                    content = content.Substring(jsonStart, jsonEnd - jsonStart);
                }
            }

            // Check if message starts with command prefix
            if (content?.StartsWith("!") != true)
            {
                _logger.LogDebug($"Not a command message: {content}");
                var defaultReply = "👋 Hello! Type `!help` for available commands.";
                await SendReplyAsync(message, client, defaultReply, ct);
                return;
            }

            // Parse command
            var parts = content!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            _logger.LogInformation($"🎯 Command detected: {command}");

            // Route to command handler
            var response = command switch
            {
                "!task" => await HandleTaskCommand(parts, message, ct),
                "!help" => GetHelpMessage(),
                _ => $"❓ Unknown command: {command}\nType `!help` for available commands."
            };

            // Send response back to channel
            _logger.LogInformation($"📤 Sending response: {response.Substring(0, Math.Min(50, response.Length))}...");
            await SendReplyAsync(message, client, response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message command");
        }
    }

    /// <summary>
    /// Send reply to message
    /// </summary>
    private async Task SendReplyAsync(ChannelMessage message, MezonClient client, string response, CancellationToken ct)
    {
        try
        {
            // Send message using SDK
            await client.SendTextAsync(
                clanId: message.ClanId.ToString(),
                channelId: message.ChannelId.ToString(),
                mode: 2,  // ChannelStreamMode.Channel
                isPublic: true,
                text: response,
                ct: ct);

            _logger.LogInformation($"✅ Reply sent to channel");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending reply");
        }
    }

    /// <summary>
    /// Handle !task commands
    /// </summary>
    private async Task<string> HandleTaskCommand(
        string[] parts,
        ChannelMessage message,
        CancellationToken ct)
    {
        if (parts.Length < 2)
            return "❌ Usage: `!task [action]`\nActions: create, list, done, delete";

        var action = parts[1].ToLower();

        return action switch
        {
            "create" => await HandleTaskCreate(parts, message, ct),
            "list" => await HandleTaskList(message, ct),
            "done" => await HandleTaskDone(parts, message, ct),
            "delete" => await HandleTaskDelete(parts, message, ct),
            _ => $"❌ Unknown task action: {action}\nActions: create, list, done, delete"
        };
    }

    /// <summary>
    /// Handle: !task create [title] [description]
    /// Example: !task create Review report Draft completion review
    /// </summary>
    private async Task<string> HandleTaskCreate(
        string[] parts,
        ChannelMessage message,
        CancellationToken ct)
    {
        try
        {
            if (parts.Length < 3)
                return "❌ Usage: `!task create [title] [description]`\nExample: `!task create Bug Fix Fix login issue`";

            var title = parts[2];
            var description = string.Join(" ", parts.Skip(3));

            // Create DTO for service
            var createTaskDto = new CreateTaskDto
            {
                Title = title,
                Description = description,
                AssignedTo = message.Username,
                CreatedBy = message.Username,
                DueDate = DateTime.UtcNow.AddDays(7),
                ChannelId = message.ChannelId.ToString(),
                MessageId = message.MessageId.ToString()
            };

            // Call service to create task
            var createdTask = await _taskService.CreateAsync(createTaskDto, ct);
            
            if (createdTask == null)
                return "❌ Failed to create task.";

            return $"✅ Task created successfully!\n" +
                   $"📝 **{createdTask.Title}**\n" +
                   $"📌 ID: {createdTask.Id}\n" +
                   $"👤 Assigned to: {createdTask.AssignedTo}\n" +
                   $"📅 Due: {createdTask.DueDate:yyyy-MM-dd}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return $"❌ Failed to create task: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle: !task list
    /// Lists all tasks for the user
    /// </summary>
    private async Task<string> HandleTaskList(
        ChannelMessage message,
        CancellationToken ct)
    {
        try
        {
            var tasks = await _taskService.GetByAssigneeAsync(message.Username, ct);

            if (!tasks.Any())
                return "📭 No tasks assigned to you.";

            var response = "📋 **Your Tasks:**\n";
            foreach (var task in tasks.Take(10)) // Limit to 10 tasks in response
            {
                var status = task.Status.ToString();
                response += $"- [{task.Id}] {task.Title} ({status})\n";
            }

            if (tasks.Count > 10)
                response += $"... and {tasks.Count - 10} more";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tasks");
            return $"❌ Failed to list tasks: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle: !task done [taskId]
    /// Mark a task as completed
    /// </summary>
    private async System.Threading.Tasks.Task<string> HandleTaskDone(
        string[] parts,
        ChannelMessage message,
        CancellationToken ct)
    {
        try
        {
            if (parts.Length < 3 || !Guid.TryParse(parts[2], out var taskId))
                return "❌ Usage: `!task done [taskId]`\nExample: `!task done 550e8400-e29b-41d4-a716-446655440000`";

            // Get task
            var taskDto = await _taskService.GetByIdAsync(taskId, ct);
            if (taskDto == null)
                return $"❌ Task not found: {taskId}";

            // Verify ownership
            if (taskDto.AssignedTo != message.Username)
                return "❌ You can only mark your own tasks as done.";

            // Update status
            await _taskService.ChangeStatusAsync(taskId, TaskStatus.Completed, ct);

            return $"✅ Task marked as completed!\n📝 **{taskDto.Title}**";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking task as done");
            return $"❌ Failed to update task: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle: !task delete [taskId]
    /// Delete a task
    /// </summary>
    private async System.Threading.Tasks.Task<string> HandleTaskDelete(
        string[] parts,
        ChannelMessage message,
        CancellationToken ct)
    {
        try
        {
            if (parts.Length < 3 || !Guid.TryParse(parts[2], out var taskId))
                return "❌ Usage: `!task delete [taskId]`";

            // Get task
            var taskDto = await _taskService.GetByIdAsync(taskId, ct);
            if (taskDto == null)
                return $"❌ Task not found: {taskId}";

            // Verify ownership
            if (taskDto.CreatedBy != message.Username && taskDto.AssignedTo != message.Username)
                return "❌ You can only delete your own tasks.";

            // Delete task
            await _taskService.DeleteAsync(taskId, ct);

            return $"🗑️ Task deleted!\n📝 **{taskDto.Title}**";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task");
            return $"❌ Failed to delete task: {ex.Message}";
        }
    }

    /// <summary>
    /// Show available commands
    /// </summary>
    private string GetHelpMessage()
    {
        return @"
📚 **Available Commands:**

**Task Management:**
• `!task create [title] [description]` - Create new task
• `!task list` - List your tasks
• `!task done [taskId]` - Mark task as completed
• `!task delete [taskId]` - Delete a task

**Other:**
• `!help` - Show this message

**Examples:**
```
!task create Fix login Fix username validation issue
!task list
!task done 550e8400-e29b-41d4-a716-446655440000
```
";
    }
}

/* 
 * ═══════════════════════════════════════════════════════════════
 * INTEGRATION STEPS
 * ═══════════════════════════════════════════════════════════════
 * 
 * 1. Add ITaskService to BotService constructor:
 * 
 *    public BotService(
 *        ILogger<BotService> logger,
 *        IConfiguration configuration,
 *        ITaskService taskService)  // ← NEW
 *    {
 *        _logger = logger;
 *        _configuration = configuration;
 *        _taskService = taskService;  // ← NEW
 *    }
 * 
 * 2. Inject BotCommandHandlerExample in BotService:
 * 
 *    private BotCommandHandlerExample? _commandHandler;
 * 
 * 3. Initialize in SubscribeToEvents():
 * 
 *    private void SubscribeToEvents()
 *    {
 *        if (_client == null) return;
 * 
 *        _commandHandler = new BotCommandHandlerExample(
 *            _logger,
 *            _taskService,
 *            _configuration);
 * 
 *        _client.ChannelMessage += OnChannelMessage;
 *        _client.Ready += OnReady;
 *    }
 * 
 * 4. Update OnChannelMessage() to call handler:
 * 
 *    private void OnChannelMessage(ChannelMessage message)
 *    {
 *        try
 *        {
 *            // ... existing logging code ...
 * 
 *            // Handle commands
 *            if (_commandHandler != null && _client != null)
 *            {
 *                #pragma warning disable CS4014
 *                _commandHandler.ProcessMessageAsync(message, _client);
 *                #pragma warning restore CS4014
 *            }
 *        }
 *        catch (Exception ex)
 *        {
 *            _logger.LogError(ex, "Error processing channel message");
 *        }
 *    }
 * 
 * 5. Register ITaskService in Program.cs DI:
 * 
 *    services.AddScoped<ITaskService, TaskService>();
 *    services.AddScoped<ITaskRepository, TaskRepository>();
 * 
 * 6. Update appsettings to include database connection
 * 
 * 7. Run migrations and start bot:
 * 
 *    dotnet ef database update
 *    dotnet run
 * 
 * ═══════════════════════════════════════════════════════════════
 */
