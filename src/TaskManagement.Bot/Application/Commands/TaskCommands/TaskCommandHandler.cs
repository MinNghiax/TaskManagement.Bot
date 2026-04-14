using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Structures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.Sessions;
using TaskManagement.Bot.Infrastructure.Enums;
using TaskManagement.Bot.Application.Commands.TaskCommands;

namespace TaskManagement.Bot.Application.Commands
{
    public class TaskCommandHandler
    {
        private readonly ILogger<TaskCommandHandler> _logger;
        private readonly ITaskService _taskService;
        private readonly ITeamService _teamService;
        private readonly IProjectService _projectService;
        private readonly SessionService _sessionService;

        public TaskCommandHandler(
            ILogger<TaskCommandHandler> logger,
            ITaskService taskService,
            IConfiguration configuration,
            ITeamService teamService,
            IProjectService projectService,
            SessionService sessionService)
        {
            _logger = logger;
            _taskService = taskService;
            _teamService = teamService;
            _projectService = projectService;
            _sessionService = sessionService;
        }

        public async Task<string?> ProcessMessageAsync(ChannelMessage message, MezonClient client, CancellationToken ct = default)
        {
            var content = message.Content?.Text?.Trim();

            //  HANDLE FORM SUBMIT 
            if (!string.IsNullOrEmpty(content) && content.StartsWith("{"))
            {
                try
                {
                    var json = JsonDocument.Parse(content);
                    var root = json.RootElement;

                    //  lấy action từ button
                    if (root.TryGetProperty("id", out var actionProp))
                    {
                        var rawAction = actionProp.GetString();

                        var partsAction = rawAction.Split('|');
                        var action = partsAction[0];
                        var clanId = partsAction.Length > 1 ? partsAction[1] : "";

                        if (action == "CREATE_TEAM")
                        {
                            var data = root.GetProperty("data");

                            var teamName = data.GetProperty("team_name").GetString();
                            var pm = data.GetProperty("pm").GetString();
                            var membersRaw = data.GetProperty("members").GetString();

                            var members = membersRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Replace("@", ""))
                                .ToList();

                            var pmUser = pm.Replace("@", "");

                            var team = await _teamService.CreateTeamAsync(
                                1,
                                teamName!,
                                pmUser!,
                                members
                            );

                            //await client.SendMessageAsync(
                            //    clanId,
                            //    message.ChannelId.ToString(),
                            //    2,
                            //    true,
                            //    new ChannelMessageContent
                            //    {
                            //        Text = $"✅ Team `{teamName}` created!"
                            //    }
                            //);

                            return $"✅ Team `{teamName}` created!";
                        }

                        if (action == "CANCEL_TEAM")
                        {
                            //await client.SendMessageAsync(
                            //    clanId,
                            //    message.ChannelId.ToString(),
                            //    2,
                            //    true,
                            //    new ChannelMessageContent
                            //    {
                            //        Text = "❌ Đã huỷ tạo team"
                            //    }
                            //);

                            return "❌ Đã huỷ tạo team";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Parse form error");
                }
            }

            // Parse JSON
            if (!string.IsNullOrEmpty(content) && content.StartsWith("{"))
            {
                try
                {
                    var json = JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("t", out var tProp))
                        content = tProp.GetString();
                }
                catch { }
            }

            if (!long.TryParse(message.SenderId, out var userId))
                return null;
            var session = _sessionService.Get(userId);

            if (!content!.StartsWith("!")) return null;

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0];

            string response = cmd switch
            {
                "!project" => await HandleProject(content, message),
                "!team" => await HandleTeam(parts, message, client),
                "!task" => await HandleTask(parts, message),
                _ => "❌ Unknown command"
            };

            return response;
        }

        //  PROJECT 
        private async Task<string> HandleProject(string content, ChannelMessage message)
        {
            if (!content.StartsWith("!project create"))
                return "❌ Usage: !project create Name";

            var name = content.Replace("!project create", "").Trim();
            if (!long.TryParse(message.SenderId, out var userId))
                return "❌ Invalid user";

            await _projectService.CreateProjectAsync(name, "", message.Username);

            return $"✅ Project `{name}` created!\n👉 Gõ `!team init` để tạo team";
        }

        // TEAM 
        private async Task<string> HandleTeam(string[] parts, ChannelMessage message, MezonClient client)
        {
            if (!long.TryParse(message.SenderId, out var userId))
                return "❌ Invalid user";
            var session = _sessionService.Get(userId);

            var hasProject = await _projectService.GetAllProjectsAsync();

            if (!hasProject.Any())
            {
                return "❌ Bạn phải tạo project trước";
            }

            if (parts.Length >= 2 && parts[1] == "init")
            {
                //session.Step = "TEAM_NAME";
                //await client.SendMessageAsync(
                //    message.ClanId.ToString(),
                //    message.ChannelId.ToString(),
                //    2,
                //    true,
                //    TaskFormBuilder.BuildTeamForm(message.ClanId!)
                //);

                return "__TEAM_FORM__";
            }

            return "❌ Usage: !team init";
        }

        //  TASK
        private async Task<string> HandleTask(string[] parts, ChannelMessage message)
        {
            if (!long.TryParse(message.SenderId, out var userId))
                return "❌ Invalid user";
            var session = _sessionService.Get(userId);

            if (session == null)
                return "❌ Bạn chưa có session";

            if (session.TeamId == null)
                return "❌ Bạn phải tạo team trước";

            if (parts.Length >= 2 && parts[1] == "create")
            {
                session.Step = "TITLE";
                return "📝 TẠO TASK\n👉 Nhập tiêu đề:";
            }

            return "❌ Usage: !task create";
        }

        private async Task<string> HandleTaskFlow(UserSession session, ChannelMessage message)
        {
            var input = message.Content?.Text?.Trim();

            switch (session.Step)
            {
                case "TITLE":
                    session.TempTask.Title = input;
                    session.Step = "DESC";
                    return "👉 Nhập description (hoặc skip):";

                case "DESC":
                    if (input != "skip")
                        session.TempTask.Description = input;

                    session.Step = "ASSIGN";
                    return "👉 Assign cho ai (@user):";

                case "ASSIGN":
                    session.TempTask.AssignedTo = input.Replace("@", "");
                    session.Step = "PRIORITY";
                    return "👉 Priority (Low/Medium/High):";

                case "PRIORITY":
                    if (!Enum.TryParse<EPriorityLevel>(input, true, out var p))
                        return "❌ Sai priority";

                    session.TempTask.Priority = p;
                    session.Step = "DONE";
                    return "👉 Deadline (yyyy-MM-dd hoặc skip):";

                case "DONE":
                    if (input != "skip" && DateTime.TryParse(input, out var d))
                        session.TempTask.DueDate = d;

                    session.TempTask.TeamId = session.TeamId;
                    session.TempTask.CreatedBy = message.Username;

                    var task = await _taskService.CreateAsync(session.TempTask);

                    _sessionService.Remove(long.Parse(message.SenderId));

                    return $"🎉 DONE!\nTask: {task?.Title}";
            }

            return "❌ Task flow lỗi";
        }

        // SEND 
        private async Task SendReply(ChannelMessage message, MezonClient client, string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return; // tránh gửi rỗng

            await client.SendMessageAsync(
                clanId: message.ClanId.ToString(),
                channelId: message.ChannelId.ToString(),
                mode: message.Mode ?? 2,
                isPublic: message.IsPublic ?? true,
                content: new ChannelMessageContent
                {
                    Text = text
                },
                references: new[]
                {
                    new ApiMessageRef
                    {
                        MessageId = message.MessageId,
                        MessageRefId = message.MessageId,
                        MessageSenderId = message.SenderId.ToString()
                    }
                }
            );
        }


    }
}