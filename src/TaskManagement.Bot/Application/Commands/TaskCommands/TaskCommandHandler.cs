using Mezon.Sdk.Domain;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.Sessions;

namespace TaskManagement.Bot.Application.Commands;

public class TaskCommandHandler : ICommandHandler
{
    private readonly SessionService _sessionService;
    private readonly ITeamService _teamService;

    public TaskCommandHandler(SessionService sessionService, ITeamService teamService)
    {
        _sessionService = sessionService;
        _teamService = teamService;
    }

    public bool CanHandle(string command)
    {
        return command.StartsWith("!task", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CommandResponse> HandleAsync(
        ChannelMessage message,
        CancellationToken cancellationToken)
    {
        var content = ParseContent(message.Content?.Text);

        if (string.IsNullOrWhiteSpace(content))
            return new CommandResponse("❌ Empty command");

        if (!long.TryParse(message.SenderId, out var userId))
            return new CommandResponse("❌ Invalid user");

        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return new CommandResponse(GetHelpText());
        }

        var action = parts[1].ToLower();

        return action switch
        {
            "create" or "form" => await HandleTaskCreate(message, userId),
            "list" => await HandleTaskList(message, userId),
            "update" => await HandleTaskUpdate(message, parts, userId),
            "status" => await HandleTaskStatusUpdate(message, parts, userId),
            "delete" => await HandleTaskDelete(message, parts, userId),
            _ => new CommandResponse(GetHelpText())
        };
    }

    // ==================== TẠO TASK ====================
    private async Task<CommandResponse> HandleTaskCreate(ChannelMessage message, long userId)
    {
        var session = _sessionService.Get(userId);
        var userIdStr = userId.ToString();

        if (session == null)
        {
            session = new UserSession
            {
                Step = "",
                TeamMembers = new List<string>()
            };
            _sessionService.Set(userId, session);
        }

        if (session.TeamId == null)
        {
            var teams = await _teamService.GetTeamsByMemberAsync(userIdStr);
            if (teams.Any())
            {
                var team = teams.First();
                session.TeamId = team.Id;
                session.TeamName = team.Name;
                session.TeamMembers = await _teamService.GetMembers(team.Id);
                _sessionService.Set(userId, session);
            }
            else
            {
                return new CommandResponse("❌ Bạn chưa tham gia team nào. Hãy tạo team bằng lệnh `!team init`");
            }
        }

        if (session.TeamMembers == null || session.TeamMembers.Count == 0)
        {
            session.TeamMembers = await _teamService.GetMembers(session.TeamId.Value);
            _sessionService.Set(userId, session);
        }

        var taskForm = TaskFormBuilder.BuildTaskForm(session.TeamMembers);
        return new CommandResponse(taskForm);
    }

    // ==================== DANH SÁCH TASK ====================
    private async Task<CommandResponse> HandleTaskList(ChannelMessage message, long userId)
    {
        var session = _sessionService.Get(userId);

        if (session?.TeamId == null)
        {
            return new CommandResponse("❌ Bạn chưa tham gia team nào!");
        }

        // Tạo nút để hiển thị danh sách (sẽ được xử lý bởi TaskComponentHandler)
        var listForm = new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new object[] { new { title = "📋 Danh sách Task", description = "Nhấn nút bên dưới để xem danh sách task", color = "#5865F2" } },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = "LIST_TASKS",
                            type = 1,
                            component = new { label = "📋 Xem danh sách task", style = 3 }
                        }
                    }
                }
            }
        };

        return new CommandResponse(listForm);
    }

    // ==================== CẬP NHẬT TASK (MENTOR) ====================
    private async Task<CommandResponse> HandleTaskUpdate(ChannelMessage message, string[] parts, long userId)
    {
        if (parts.Length < 3)
        {
            return new CommandResponse("❌ Dùng: !task update <taskId>");
        }

        if (!int.TryParse(parts[2], out var taskId))
        {
            return new CommandResponse("❌ Task ID không hợp lệ");
        }

        var session = _sessionService.Get(userId);
        if (session?.TeamId == null)
        {
            return new CommandResponse("❌ Bạn chưa tham gia team nào!");
        }

        var isPM = await _teamService.IsPM(userId.ToString(), session.TeamId.Value);
        if (!isPM)
        {
            return new CommandResponse("❌ Chỉ Mentor mới có quyền cập nhật task!");
        }

        // Tạo nút cập nhật (sẽ được xử lý bởi TaskComponentHandler)
        var updateForm = new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new object[] { new { title = $"✏️ Cập nhật Task #{taskId}", description = "Nhấn nút bên dưới để cập nhật task", color = "#5865F2" } },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"UPDATE_TASK|{taskId}",
                            type = 1,
                            component = new { label = "✏️ Cập nhật task", style = 3 }
                        }
                    }
                }
            }
        };

        return new CommandResponse(updateForm);
    }

    // ==================== CẬP NHẬT TRẠNG THÁI (MEMBER) ====================
    private async Task<CommandResponse> HandleTaskStatusUpdate(ChannelMessage message, string[] parts, long userId)
    {
        if (parts.Length < 3)
        {
            return new CommandResponse("❌ Dùng: !task status <taskId>");
        }

        if (!int.TryParse(parts[2], out var taskId))
        {
            return new CommandResponse("❌ Task ID không hợp lệ");
        }

        // Tạo nút cập nhật trạng thái (sẽ được xử lý bởi TaskComponentHandler)
        var statusForm = new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new object[] { new { title = $"📊 Cập nhật trạng thái Task #{taskId}", description = "Nhấn nút bên dưới để cập nhật trạng thái", color = "#FEE75C" } },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"UPDATE_TASK_STATUS|{taskId}",
                            type = 1,
                            component = new { label = "📊 Cập nhật trạng thái", style = 3 }
                        }
                    }
                }
            }
        };

        return new CommandResponse(statusForm);
    }

    // ==================== XÓA TASK (MENTOR) ====================
    private async Task<CommandResponse> HandleTaskDelete(ChannelMessage message, string[] parts, long userId)
    {
        if (parts.Length < 3)
        {
            return new CommandResponse("❌ Dùng: !task delete <taskId>");
        }

        if (!int.TryParse(parts[2], out var taskId))
        {
            return new CommandResponse("❌ Task ID không hợp lệ");
        }

        var session = _sessionService.Get(userId);
        if (session?.TeamId == null)
        {
            return new CommandResponse("❌ Bạn chưa tham gia team nào!");
        }

        var isPM = await _teamService.IsPM(userId.ToString(), session.TeamId.Value);
        if (!isPM)
        {
            return new CommandResponse("❌ Chỉ Mentor mới có quyền xóa task!");
        }

        // Tạo nút xóa (sẽ được xử lý bởi TaskComponentHandler)
        var deleteForm = new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new object[] { new { title = $"🗑️ Xóa Task #{taskId}", description = "Nhấn nút bên dưới để xóa task", color = "#ED4245" } },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"DELETE_TASK|{taskId}",
                            type = 1,
                            component = new { label = "🗑️ Xóa task", style = 4 }
                        }
                    }
                }
            }
        };

        return new CommandResponse(deleteForm);
    }

    private string GetHelpText()
    {
        return @"
╔══════════════════════════════════════════════════════════════╗
║                    📝 QUẢN LÝ TASK                          ║
╚══════════════════════════════════════════════════════════════╝

📌 **Các lệnh có sẵn:**

1. **Tạo task mới**
   `!task create` hoặc `!task form`

2. **Xem danh sách task**
   `!task list`

3. **Cập nhật task (Chỉ Mentor)**
   `!task update <taskId>`

4. **Cập nhật trạng thái (Member)**
   `!task status <taskId>`

5. **Xóa task (Chỉ Mentor)**
   `!task delete <taskId>`

📌 **Ví dụ:**
   `!task create`
   `!task list`
   `!task update 123`
   `!task status 456`
   `!task delete 789`
";
    }

    // ================= PARSE =================
    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (raw.StartsWith("{"))
        {
            try
            {
                var json = JsonDocument.Parse(raw);
                return json.RootElement.GetProperty("t").GetString();
            }
            catch { }
        }

        return raw;
    }
}
