// TaskManagement.Bot.Application.Services.BotService.cs
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Commands.Complain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string clanId, string channelId, string text, int mode, bool isPublic, CancellationToken ct = default);
}

public class BotService : IBotService
{
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<ICommandHandler> _handlers;
    private readonly ComplainCommandHandler _complainHandler;
    private readonly IComplainService _complainService;
    private readonly ITaskService _taskService;

    private MezonClient? _client;
    private string? _botUserId;
    private readonly Dictionary<string, string> _channelToClan = new();

    // Timezone cho Việt Nam (UTC+7)
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            // Thử với Windows format trước
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Fallback cho Linux/Mac
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback cuối cùng: UTC+7
                return TimeZoneInfo.CreateCustomTimeZone("Vietnam Standard Time", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
            }
        }
    }

    public BotService(
        ILogger<BotService> logger,
        IConfiguration configuration,
        IEnumerable<ICommandHandler> handlers,
        ComplainCommandHandler complainHandler,
        IComplainService complainService,
        ITaskService taskService)
    {
        _logger = logger;
        _configuration = configuration;
        _handlers = handlers;
        _complainHandler = complainHandler;
        _complainService = complainService;
        _taskService = taskService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var options = new MezonClientOptions
        {
            BotId = _configuration["Mezon:BotId"] ?? throw new Exception("Missing BotId"),
            Token = _configuration["Mezon:Token"] ?? throw new Exception("Missing Token"),
            Host = _configuration["Mezon:Host"] ?? "gw.mezon.ai",
            Port = _configuration["Mezon:Port"] ?? "443",
            UseSSL = bool.Parse(_configuration["Mezon:UseSsl"] ?? "true"),
            TimeoutMs = int.Parse(_configuration["Mezon:TimeoutMs"] ?? "10000")
        };
        _client = new MezonClient(options);

        _client.On("channel_message", OnChannelMessage);
        _client.On("message_button_clicked", OnButtonClicked);
        _client.On("dropdown_selected", OnDropdownSelected);

        var session = await _client.LoginAsync(cancellationToken);
        _botUserId = session.UserId;
        _logger.LogInformation("Bot started. BotUserId: {BotUserId}", _botUserId);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null) await _client.LogoutAsync(cancellationToken);
    }

    public async Task SendMessageAsync(string clanId, string channelId, string text, int mode, bool isPublic, CancellationToken ct = default)
    {
        if (_client == null) return;
        await _client.SendMessageAsync(clanId, channelId, mode, isPublic,
            new ChannelMessageContent { Text = text }, cancellationToken: ct);
    }

    // ================= HELPER: Get user info =================

    private async Task<string> GetUserDisplayNameAsync(string clanId, string userId)
    {
        if (_client == null) return userId;

        try
        {
            var clan = _client.Clans.Get(clanId);
            if (clan != null)
            {
                var user = clan.Users.Get(userId);
                if (user != null)
                {
                    return !string.IsNullOrEmpty(user.DisplayName)
                        ? user.DisplayName
                        : (user.Username ?? userId);
                }
            }
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user display name for {UserId}", userId);
            return userId;
        }
    }

    // ================= HELPER: Reply message =================

    private ApiMessageRef[]? BuildReplyReferences(ChannelMessage originalMessage)
    {
        if (originalMessage == null) return null;

        return new[]
        {
            new ApiMessageRef
            {
                MessageId = originalMessage.Id,
                MessageRefId = originalMessage.Id,
                MessageSenderId = originalMessage.SenderId ?? "",
                MessageSenderUsername = originalMessage.Username ?? "",
                MessageSenderDisplayName = originalMessage.DisplayName ?? "",
                MessageSenderClanNick = originalMessage.ClanNick ?? "",
                MesagesSenderAvatar = originalMessage.ClanAvatar ?? "",
                Content = originalMessage.Content?.Text ?? "",
                HasAttachment = originalMessage.Attachments?.Any() ?? false,
                RefType = 0
            }
        };
    }

    private async Task ReplyTextAsync(string clanId, string channelId, string text, ChannelMessage originalMessage, int mode = 2, bool isPublic = true)
    {
        if (_client == null) return;

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: isPublic,
            content: new ChannelMessageContent { Text = text },
            references: BuildReplyReferences(originalMessage)
        );
    }

    private async Task ReplyFormAsync(string clanId, string channelId, ChannelMessageContent content, ChannelMessage originalMessage, int mode = 2, bool isPublic = true)
    {
        if (_client == null) return;

        await _client.SendMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: mode,
            isPublic: isPublic,
            content: content,
            references: BuildReplyReferences(originalMessage)
        );
    }

    // ================= HELPER: Format date with Vietnam timezone =================

    private string FormatDateWithVietnamTime(DateTime? date)
    {
        if (!date.HasValue) return "No deadline";

        try
        {
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date.Value, VietnamTimeZone);
            return vietnamTime.ToString("dd/MM/yyyy HH:mm");
        }
        catch
        {
            // Fallback: format UTC time if timezone conversion fails
            return date.Value.ToString("dd/MM/yyyy HH:mm") + " (UTC)";
        }
    }

    // ================= EVENT HANDLERS =================

    private async void OnChannelMessage(object? sender, MezonEventArgs e)
    {
        try
        {
            if (_client == null || e.Data is not ChannelMessage msg) return;
            if (msg.SenderId == _botUserId) return;

            var raw = msg.Content?.Text;
            var content = ParseContent(raw);
            if (string.IsNullOrWhiteSpace(content)) return;

            _channelToClan[msg.ChannelId!] = msg.ClanId!;

            var senderName = await GetUserDisplayNameAsync(msg.ClanId!, msg.SenderId!);
            _logger.LogInformation("Message from {SenderName}: {Content}", senderName, content);

            if (content.Trim().Equals("!complain", StringComparison.OrdinalIgnoreCase))
            {
                var form = await _complainHandler.GetFormAsync(msg.SenderId!, CancellationToken.None);
                if (form == null)
                    await ReplyTextAsync(msg.ClanId!, msg.ChannelId!, "You have no tasks to complain about.", msg, msg.Mode ?? 2, msg.IsPublic ?? true);
                else
                    await ReplyFormAsync(msg.ClanId!, msg.ChannelId!, form, msg, msg.Mode ?? 2, msg.IsPublic ?? true);
                return;
            }

            if (content.Trim().Equals("!approve", StringComparison.OrdinalIgnoreCase))
            {
                var form = await GetApproveFormAsync(msg.ClanId!, msg.SenderId!);
                if (form == null)
                    await ReplyTextAsync(msg.ClanId!, msg.ChannelId!, "No pending complaints to review.", msg, msg.Mode ?? 2, msg.IsPublic ?? true);
                else
                    await ReplyFormAsync(msg.ClanId!, msg.ChannelId!, form, msg, msg.Mode ?? 2, msg.IsPublic ?? true);
                return;
            }

            foreach (var handler in _handlers)
            {
                if (!handler.CanHandle(content)) continue;
                var response = await handler.HandleAsync(msg, CancellationToken.None);
                if (!string.IsNullOrEmpty(response))
                    await ReplyTextAsync(msg.ClanId!, msg.ChannelId!, response, msg, msg.Mode ?? 2, msg.IsPublic ?? true);
                break;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "OnChannelMessage error"); }
    }

    private async Task<ChannelMessageContent?> GetApproveFormAsync(string clanId, string pmUserId)
    {
        var pendingComplains = await _complainService.GetPendingByPMAsync(pmUserId);
        if (!pendingComplains.Any()) return null;

        var options = new List<object>();
        foreach (var c in pendingComplains)
        {
            var complainantName = await GetUserDisplayNameAsync(clanId, c.UserId);
            options.Add(new
            {
                label = $"#{c.Id} - {c.TaskTitle} [{c.Type}] - From: {complainantName}",
                value = c.Id.ToString()
            });
        }

        return BuildApproveForm(options.ToArray());
    }

    private ChannelMessageContent BuildApproveForm(object[] complainOptions)
    {
        var fields = new List<object>
        {
            new
            {
                name = "📋 Select the complaint to review.",
                value = string.Empty,
                inputs = new
                {
                    id = "approve_complain_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Choose a complaint...",
                        options = complainOptions
                    }
                }
            },
            new
            {
                name = "📝 Reason for refusal (fill in only if refusing)",
                value = string.Empty,
                inputs = new
                {
                    id = "reject_reason",
                    type = 3,
                    component = new
                    {
                        id = "reject_reason_component",
                        placeholder = "Enter the reason for refusal...",
                        defaultValue = "",
                        type = "text",
                        textarea = true
                    }
                }
            }
        };

        var embed = new
        {
            title = "🔍 Review complaints",
            description = "Select the complaint and enter the reason for rejection (if any).",
            color = "#5865F2",
            fields = fields.ToArray()
        };

        return new ChannelMessageContent
        {
            Text = "Please select a complaint to review.:",
            Embed = new object[] { embed },
            Components = new object[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new { type = 1, id = "approve_submit", component = new { label = "✅ Approve", style = 3 } },
                        new { type = 1, id = "reject_submit", component = new { label = "❌ Reject", style = 4 } },
                        new { type = 1, id = "approve_cancel", component = new { label = "Cancel", style = 2 } }
                    }
                }
            }
        };
    }

    private async void OnButtonClicked(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not Mezon.Sdk.Proto.Envelope env || env.MessageButtonClicked == null)
                return;

            var btn = env.MessageButtonClicked;
            var buttonId = btn.ButtonId ?? "";
            var channelId = btn.ChannelId.ToString();
            var userId = btn.UserId.ToString();
            var extraData = btn.ExtraData ?? "";
            var messageId = btn.MessageId.ToString();

            if (!_channelToClan.TryGetValue(channelId, out var clanId))
                return;

            var userName = await GetUserDisplayNameAsync(clanId, userId);
            _logger.LogInformation("Button {ButtonId} clicked by {UserName}", buttonId, userName);

            if (buttonId == "complain_cancel" || buttonId == "approve_cancel")
            {
                await UpdateMessageAsync(clanId, channelId, messageId, $"❌ {userName} canceled.");
                return;
            }

            if (buttonId == "complain_submit")
            {
                await HandleComplainSubmit(clanId, channelId, messageId, userId, userName, extraData);
                return;
            }

            if (buttonId == "approve_submit")
            {
                await HandleApproveSubmit(clanId, channelId, messageId, userId, userName, extraData);
                return;
            }

            if (buttonId == "reject_submit")
            {
                await HandleRejectSubmit(clanId, channelId, messageId, userId, userName, extraData);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnButtonClicked error");
        }
    }

    private async Task HandleComplainSubmit(string clanId, string channelId, string messageId, string userId, string userName, string extraData)
    {
        var taskIdStr = ParseExtraData(extraData, "complain_task_select");
        var complainType = ParseExtraData(extraData, "complain_type_select");
        var reason = ParseExtraData(extraData, "complain_reason");
        var durationStr = ParseExtraData(extraData, "extend_duration");

        if (!int.TryParse(taskIdStr, out var taskId))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please select a task.");
            return;
        }

        if (string.IsNullOrEmpty(complainType))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please select a complaint type.");
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please enter a reason.");
            return;
        }

        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null)
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Task does not exist.");
            return;
        }

        if (complainType == "RequestExtend")
        {
            if (!int.TryParse(durationStr, out var hours) || hours <= 0)
            {
                await UpdateMessageAsync(clanId, channelId, messageId, "❌ Invalid duration.");
                return;
            }

            var newDue = (task.DueDate ?? DateTime.UtcNow).AddHours(hours);
            var dto = new CreateComplainDto
            {
                TaskItemId = taskId,
                UserId = userId,
                Reason = reason,
                Type = EComplainType.RequestExtend,
                NewDueDate = newDue
            };

            var (result, err) = await _complainService.CreateAsync(dto);
            if (err != null)
                await UpdateMessageAsync(clanId, channelId, messageId, $"❌ {err}");
            else
                await UpdateRichMessageAsync(clanId, channelId, messageId, BuildExtendComplainSuccessMessage(result!.Id, task, userName, reason, newDue, hours));
        }
        else
        {
            var dto = new CreateComplainDto
            {
                TaskItemId = taskId,
                UserId = userId,
                Reason = reason,
                Type = EComplainType.RequestCancel
            };

            var (result, err) = await _complainService.CreateAsync(dto);
            if (err != null)
                await UpdateMessageAsync(clanId, channelId, messageId, $"❌ {err}");
            else
                await UpdateRichMessageAsync(clanId, channelId, messageId, BuildCancelComplainSuccessMessage(result!.Id, task, userName, reason));
        }
    }

    private async Task HandleApproveSubmit(string clanId, string channelId, string messageId, string userId, string userName, string extraData)
    {
        var complainIdStr = ParseExtraData(extraData, "approve_complain_select");

        if (!int.TryParse(complainIdStr, out var complainId))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please select a complaint.");
            return;
        }

        var complain = await _complainService.GetByIdAsync(complainId);
        var task = complain != null ? await _taskService.GetByIdAsync(complain.TaskItemId) : null;
        var complainantName = complain != null ? await GetUserDisplayNameAsync(clanId, complain.UserId) : complain?.UserId;

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = true
        };

        var (success, error) = await _complainService.ReviewAsync(dto);
        if (!success)
            await UpdateMessageAsync(clanId, channelId, messageId, $"❌ {error}");
        else
            await UpdateRichMessageAsync(clanId, channelId, messageId, BuildApproveSuccessMessage(complain!, task, complainantName, userName));
    }

    private async Task HandleRejectSubmit(string clanId, string channelId, string messageId, string userId, string userName, string extraData)
    {
        var complainIdStr = ParseExtraData(extraData, "approve_complain_select");
        var rejectReason = ParseExtraData(extraData, "reject_reason");

        if (!int.TryParse(complainIdStr, out var complainId))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please select a complaint.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            await UpdateMessageAsync(clanId, channelId, messageId, "❌ Please enter a rejection reason.");
            return;
        }

        var complain = await _complainService.GetByIdAsync(complainId);
        var task = complain != null ? await _taskService.GetByIdAsync(complain.TaskItemId) : null;
        var complainantName = complain != null ? await GetUserDisplayNameAsync(clanId, complain.UserId) : complain?.UserId;

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = false,
            RejectReason = rejectReason
        };

        var (success, error) = await _complainService.ReviewAsync(dto);
        if (!success)
            await UpdateMessageAsync(clanId, channelId, messageId, $"❌ {error}");
        else
            await UpdateRichMessageAsync(clanId, channelId, messageId, BuildRejectSuccessMessage(complain!, task, complainantName, userName, rejectReason));
    }

    private async void OnDropdownSelected(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not Mezon.Sdk.Proto.Envelope env || env.DropdownBoxSelected == null)
                return;

            var dropdown = env.DropdownBoxSelected;
            var channelId = dropdown.ChannelId.ToString();
            var userId = dropdown.UserId.ToString();
            var selectboxId = dropdown.SelectboxId;
            var selectedValues = dropdown.Values.ToList();

            if (!_channelToClan.TryGetValue(channelId, out var clanId))
                return;

            var userName = await GetUserDisplayNameAsync(clanId, userId);
            var selectedText = string.Join(", ", selectedValues);

            _logger.LogInformation("Dropdown {SelectboxId} selected by {UserName}: {SelectedValues}",
                selectboxId, userName, selectedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnDropdownSelected error");
        }
    }

    private static string? ParseExtraData(string extraData, string fieldId)
    {
        if (string.IsNullOrEmpty(extraData)) return null;
        try
        {
            var json = JsonDocument.Parse(extraData);
            if (json.RootElement.TryGetProperty(fieldId, out var val))
                return val.GetString();
            if (json.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty(fieldId, out val))
                return val.GetString();
            return null;
        }
        catch { return null; }
    }

    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.StartsWith("{"))
        {
            try { return JsonDocument.Parse(raw).RootElement.GetProperty("t").GetString(); }
            catch { }
        }
        return raw;
    }

    private async Task UpdateMessageAsync(string clanId, string channelId, string messageId, string text)
    {
        if (_client == null) return;

        await _client.UpdateMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: 2,
            isPublic: true,
            messageId: messageId,
            content: new ChannelMessageContent
            {
                Text = text,
                Embed = Array.Empty<object>(),
                Components = Array.Empty<object>()
            }
        );
    }

    private async Task UpdateRichMessageAsync(string clanId, string channelId, string messageId, ChannelMessageContent content)
    {
        if (_client == null) return;

        await _client.UpdateMessageAsync(
            clanId: clanId,
            channelId: channelId,
            mode: 2,
            isPublic: true,
            messageId: messageId,
            content: content
        );
    }

    private string GetStatusText(ETaskStatus? status)
    {
        return status switch
        {
            ETaskStatus.ToDo => "📋 To Do",
            ETaskStatus.Doing => "🔄 Doing",
            ETaskStatus.Review => "👀 Review",
            ETaskStatus.Late => "⚠️ Late",
            ETaskStatus.Completed => "✅ Completed",
            ETaskStatus.Cancelled => "❌ Cancelled",
            _ => "Unknown"
        };
    }

    // ================= BUILD MESSAGES =================

    private ChannelMessageContent BuildExtendComplainSuccessMessage(int complainId, TaskDto task, string userName, string reason, DateTime newDue, int hours)
    {
        var embed = new
        {
            title = "✅ Deadline extension request submitted",
            color = "#00D26A",
            fields = new object[]
            {
                new { name = "📋 Complain ID", value = $"#{complainId}", inline = true },
                new { name = "📌 Type", value = "RequestExtend", inline = true },
                new { name = "📌 Task", value = $"{task.Title}", inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "⏰ Old deadline", value = FormatDateWithVietnamTime(task.DueDate), inline = true },
                new { name = "⏰ New deadline", value = FormatDateWithVietnamTime(newDue), inline = true },
                new { name = "📈 Added", value = $"+{hours} hours", inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "👤 Submitted by", value = userName, inline = true },
                new { name = "📝 Reason", value = reason, inline = true }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private ChannelMessageContent BuildCancelComplainSuccessMessage(int complainId, TaskDto task, string userName, string reason)
    {
        var embed = new
        {
            title = "✅ Task cancellation request submitted",
            color = "#FF8C00",
            fields = new object[]
            {
                new { name = "📋 Complain ID", value = $"#{complainId}", inline = true },
                new { name = "📌 Type", value = "RequestCancel", inline = true },
                new { name = "📌 Task", value = $"{task.Title}", inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "📊 Status", value = GetStatusText(task.Status), inline = true },
                new { name = "⏰ Deadline", value = FormatDateWithVietnamTime(task.DueDate), inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "👤 Submitted by", value = userName, inline = true },
                new { name = "📝 Reason", value = reason, inline = true }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private ChannelMessageContent BuildApproveSuccessMessage(ComplainDto complain, TaskDto? task, string complainantName, string approverName)
    {
        var isExtend = complain.Type == "RequestExtend";
        var fields = new List<object>
        {
            new { name = "📋 Complain ID", value = $"#{complain.Id}", inline = true },
            new { name = "📌 Type", value = isExtend ? "RequestExtend" : "RequestCancel", inline = true },
            new { name = "📌 Task", value = $"{task?.Title}", inline = true },
            new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
            new { name = "🔄 New status", value = GetStatusText(task?.Status), inline = true }
        };

        if (isExtend && complain.NewDueDate.HasValue)
        {
            fields.Add(new { name = "⏰ New deadline", value = FormatDateWithVietnamTime(complain.NewDueDate), inline = true });
        }

        fields.AddRange(new object[]
        {
            new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
            new { name = "👤 Complainant", value = complainantName, inline = true },
            new { name = "👤 Approved by", value = approverName, inline = true }
        });

        var embed = new
        {
            title = isExtend ? "✅ Deadline extension approved" : "✅ Task cancellation approved",
            color = "#00D26A",
            fields = fields.ToArray()
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private ChannelMessageContent BuildRejectSuccessMessage(ComplainDto complain, TaskDto? task, string complainantName, string rejectorName, string rejectReason)
    {
        var isExtend = complain.Type == "RequestExtend";
        var embed = new
        {
            title = "❌ Request rejected",
            color = "#FF3B30",
            fields = new object[]
            {
                new { name = "📋 Complain ID", value = $"#{complain.Id}", inline = true },
                new { name = "📌 Type", value = isExtend ? "RequestExtend" : "RequestCancel", inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "📌 Task", value = $"{task?.Title}", inline = true },
                new { name = "📝 Original reason", value = complain.Reason, inline = true },
                new { name = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", value = "\u200B", inline = false },
                new { name = "👤 Complainant", value = complainantName, inline = true },
                new { name = "👤 Rejected by", value = rejectorName, inline = true },
                new { name = "📝 Rejection reason", value = rejectReason, inline = true }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }
}