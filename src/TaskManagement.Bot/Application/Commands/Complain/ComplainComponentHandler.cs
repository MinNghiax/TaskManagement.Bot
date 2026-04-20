using Mezon.Sdk.Domain;
using System.Text.Json;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.Complain;

/// <summary>
/// Handles button clicks and dropdown selections for complaint workflow.
/// </summary>
public class ComplainComponentHandler : IComponentHandler
{
    private readonly IComplainService _complainService;
    private readonly ITaskService _taskService;
    private readonly IMezonUserService _userService;

    public ComplainComponentHandler(
        IComplainService complainService,
        ITaskService taskService,
        IMezonUserService userService)
    {
        _complainService = complainService;
        _taskService = taskService;
        _userService = userService;
    }

    public bool CanHandle(string customId)
    {
        return customId == "complain_submit" ||
               customId == "complain_cancel" ||
               customId == "complain_approve_submit" ||
               customId == "complain_reject_submit" ||
               customId == "approve_cancel" ||
               customId.StartsWith("complain_approve_") ||
               customId.StartsWith("complain_reject_");
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        var customId = context.CustomId;
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        var userName = await _userService.GetDisplayNameAsync(userId, clanId, cancellationToken);

        if (customId == "complain_cancel")
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ Complaint cancelled by {userName}.", mode, isPublic, messageId, null)
                .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
        }

        if (customId == "approve_cancel")
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ Review cancelled by {userName}.", mode, isPublic, messageId, null)
                .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
        }

        if (customId == "complain_submit")
        {
            return await HandleComplainSubmit(context, userName, cancellationToken);
        }

        if (customId == "complain_approve_submit")
        {
            return await HandleApproveSubmitFromForm(context, userName, cancellationToken);
        }

        if (customId == "complain_reject_submit")
        {
            return await HandleRejectSubmitFromForm(context, userName, cancellationToken);
        }

        if (customId.StartsWith("complain_approve_"))
        {
            var complainId = int.Parse(customId.Split('_')[2]);
            return await HandleApproveSubmit(complainId, context, userName, cancellationToken);
        }

        if (customId.StartsWith("complain_reject_"))
        {
            var complainId = int.Parse(customId.Split('_')[2]);
            var extraData = ComponentPayloadHelper.GetExtraData(context.Payload);
            var rejectReason = ParseExtraData(extraData, "reject_reason");
            return await HandleRejectSubmit(complainId, rejectReason, context, userName, cancellationToken);
        }

        return ComponentResponse.FromText(clanId, channelId, "❌ Unknown action.", mode, isPublic, messageId, null);
    }

    private async Task<ComponentResponse> HandleComplainSubmit(ComponentContext context, string userName, CancellationToken ct)
    {
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        var extraData = ComponentPayloadHelper.GetExtraData(context.Payload);

        var taskIdStr = ParseExtraData(extraData, "complain_task_select");
        var complainType = ParseExtraData(extraData, "complain_type_select");
        var reason = ParseExtraData(extraData, "complain_reason");
        var durationStr = ParseExtraData(extraData, "extend_duration");

        // Validate task selection
        if (!int.TryParse(taskIdStr, out var taskId))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please select a task.", mode, isPublic, messageId, null);
        }

        // Validate complaint type
        if (string.IsNullOrEmpty(complainType))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please select a complaint type.", mode, isPublic, messageId, null);
        }

        // Validate reason
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please enter a reason.", mode, isPublic, messageId, null);
        }

        // Get task
        var task = await _taskService.GetByIdAsync(taskId, ct);
        if (task == null)
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Task does not exist.", mode, isPublic, messageId, null);
        }

        // Handle RequestExtend
        if (complainType == "RequestExtend")
        {
            // Validate duration
            if (string.IsNullOrEmpty(durationStr) || !int.TryParse(durationStr, out var hours) || hours <= 0)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, "❌ Invalid duration selected. Please choose a valid extension time.", mode, isPublic, messageId, null);
            }

            // Validate hours range (max 72 hours)
            if (hours > 72)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, "❌ Extension duration cannot exceed 72 hours.", mode, isPublic, messageId, null);
            }

            var newDue = (task.DueDate ?? DateTime.UtcNow).AddHours(hours);

            // Additional validation: new deadline must be after current deadline
            if (task.DueDate.HasValue && newDue <= task.DueDate)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, "❌ New deadline must be after current deadline.", mode, isPublic, messageId, null);
            }

            var dto = new CreateComplainDto
            {
                TaskItemId = taskId,
                UserId = userId,
                Reason = reason,
                Type = EComplainType.RequestExtend,
                NewDueDate = newDue
            };

            var (result, error) = await _complainService.CreateAsync(dto, ct);
            if (error != null)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
            }

            return ComponentResponse
                .FromContent(clanId, channelId, BuildExtendSuccessMessage(result!, task, userName, reason, newDue, hours), mode, isPublic, messageId, null)
                .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
        }
        else // RequestCancel
        {
            // Additional validation for cancelled tasks
            if (task.Status == ETaskStatus.Completed)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, "❌ Cannot cancel a completed task.", mode, isPublic, messageId, null);
            }

            if (task.Status == ETaskStatus.Cancelled)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, "❌ Task is already cancelled.", mode, isPublic, messageId, null);
            }

            var dto = new CreateComplainDto
            {
                TaskItemId = taskId,
                UserId = userId,
                Reason = reason,
                Type = EComplainType.RequestCancel
            };

            var (result, error) = await _complainService.CreateAsync(dto, ct);
            if (error != null)
            {
                return ComponentResponse
                    .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
            }

            return ComponentResponse
                .FromContent(clanId, channelId, BuildCancelSuccessMessage(result!, task, userName, reason), mode, isPublic, messageId, null)
                .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
        }
    }

    private async Task<ComponentResponse> HandleApproveSubmitFromForm(ComponentContext context, string userName, CancellationToken ct)
    {
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        var extraData = ComponentPayloadHelper.GetExtraData(context.Payload);
        var complainIdStr = ParseExtraData(extraData, "approve_complain_select");

        if (!int.TryParse(complainIdStr, out var complainId))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please select a complaint.", mode, isPublic, messageId, null);
        }

        var complain = await _complainService.GetByIdAsync(complainId, ct);
        if (complain == null)
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Complaint not found.", mode, isPublic, messageId, null);
        }

        if (complain.Status != "Pending")
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ This complaint has already been {complain.Status.ToLower()}.", mode, isPublic, messageId, null);
        }

        var task = await _taskService.GetByIdAsync(complain.TaskItemId, ct);
        var complainantName = await _userService.GetDisplayNameAsync(complain.UserId, clanId, ct);

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = true
        };

        var (success, error) = await _complainService.ReviewAsync(dto, ct);
        if (!success)
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
        }

        return ComponentResponse
            .FromContent(clanId, channelId, BuildApproveSuccessMessage(complain, task, complainantName, userName), mode, isPublic, messageId, null)
            .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
    }

    private async Task<ComponentResponse> HandleRejectSubmitFromForm(ComponentContext context, string userName, CancellationToken ct)
    {
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        var extraData = ComponentPayloadHelper.GetExtraData(context.Payload);

        var complainIdStr = ParseExtraData(extraData, "approve_complain_select");
        var rejectReason = ParseExtraData(extraData, "reject_reason");

        if (!int.TryParse(complainIdStr, out var complainId))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please select a complaint.", mode, isPublic, messageId, null);
        }

        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please enter a rejection reason.", mode, isPublic, messageId, null);
        }

        var complain = await _complainService.GetByIdAsync(complainId, ct);
        if (complain == null)
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Complaint not found.", mode, isPublic, messageId, null);
        }

        if (complain.Status != "Pending")
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ This complaint has already been {complain.Status.ToLower()}.", mode, isPublic, messageId, null);
        }

        var task = await _taskService.GetByIdAsync(complain.TaskItemId, ct);
        var complainantName = await _userService.GetDisplayNameAsync(complain.UserId, clanId, ct);

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = false,
            RejectReason = rejectReason
        };

        var (success, error) = await _complainService.ReviewAsync(dto, ct);
        if (!success)
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
        }

        return ComponentResponse
            .FromContent(clanId, channelId, BuildRejectSuccessMessage(complain, task, complainantName, userName, rejectReason), mode, isPublic, messageId, null)
            .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
    }

    private async Task<ComponentResponse> HandleApproveSubmit(int complainId, ComponentContext context, string userName, CancellationToken ct)
    {
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        var complain = await _complainService.GetByIdAsync(complainId, ct);
        if (complain == null)
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Complaint not found.", mode, isPublic, messageId, null);
        }

        var task = await _taskService.GetByIdAsync(complain.TaskItemId, ct);
        var complainantName = await _userService.GetDisplayNameAsync(complain.UserId, clanId, ct);

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = true
        };

        var (success, error) = await _complainService.ReviewAsync(dto, ct);
        if (!success)
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
        }

        return ComponentResponse
            .FromContent(clanId, channelId, BuildApproveSuccessMessage(complain, task, complainantName, userName), mode, isPublic, messageId, null)
            .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
    }

    private async Task<ComponentResponse> HandleRejectSubmit(int complainId, string? rejectReason, ComponentContext context, string userName, CancellationToken ct)
    {
        var clanId = context.ClanId!;
        var channelId = context.ChannelId!;
        var userId = context.CurrentUserId!;
        var messageId = context.MessageId!;
        var mode = context.Mode;
        var isPublic = context.IsPublic;

        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Please enter a rejection reason.", mode, isPublic, messageId, null);
        }

        var complain = await _complainService.GetByIdAsync(complainId, ct);
        if (complain == null)
        {
            return ComponentResponse
                .FromText(clanId, channelId, "❌ Complaint not found.", mode, isPublic, messageId, null);
        }

        var task = await _taskService.GetByIdAsync(complain.TaskItemId, ct);
        var complainantName = await _userService.GetDisplayNameAsync(complain.UserId, clanId, ct);

        var dto = new ApproveComplainDto
        {
            ComplainId = complainId,
            ApprovedBy = userId,
            IsApproved = false,
            RejectReason = rejectReason
        };

        var (success, error) = await _complainService.ReviewAsync(dto, ct);
        if (!success)
        {
            return ComponentResponse
                .FromText(clanId, channelId, $"❌ {error}", mode, isPublic, messageId, null);
        }

        return ComponentResponse
            .FromContent(clanId, channelId, BuildRejectSuccessMessage(complain, task, complainantName, userName, rejectReason), mode, isPublic, messageId, null)
            .DeleteMessage(clanId, channelId, messageId, mode, isPublic, messageId, null);
    }

    private static string? ParseExtraData(string? extraData, string fieldId)
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

    private static string FormatDateWithVietnamTime(DateTime? date)
    {
        if (!date.HasValue) return "No deadline";
        var vietnamTime = date.Value.AddHours(7);
        return vietnamTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string GetStatusText(ETaskStatus? status)
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

    private static ChannelMessageContent BuildExtendSuccessMessage(ComplainDto complain, TaskDto task, string userName, string reason, DateTime newDue, int hours)
    {
        var createdAt = FormatDateWithVietnamTime(complain.CreatedAt);

        var embed = new
        {
            title = "✅ Deadline extension request submitted",
            color = "#00D26A",
            fields = new object[]
            {
            new { name = "📋 Complaint ID", value = $"#{complain.Id}", inline = true },
            new { name = "🏷️ Type", value = "RequestExtend", inline = true },
            new { name = "📌 Task", value = task.Title, inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "⏰ Old deadline", value = FormatDateWithVietnamTime(task.DueDate), inline = true },
            new { name = "⏰ New deadline", value = FormatDateWithVietnamTime(newDue), inline = true },
            new { name = "📈 Added", value = $"+{hours} hours", inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "👤 Submitted by", value = userName, inline = true },
            new { name = "💬 Reason", value = reason, inline = true },
            new { name = "🕐 Submitted at", value = createdAt, inline = true }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private static ChannelMessageContent BuildCancelSuccessMessage(ComplainDto complain, TaskDto task, string userName, string reason)
    {
        var createdAt = FormatDateWithVietnamTime(complain.CreatedAt);

        var embed = new
        {
            title = "✅ Task cancellation request submitted",
            color = "#FF8C00",
            fields = new object[]
            {
            new { name = "📋 Complaint ID", value = $"#{complain.Id}", inline = true },
            new { name = "🏷️ Type", value = "RequestCancel", inline = true },
            new { name = "📌 Task", value = task.Title, inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "📊 Status", value = GetStatusText(task.Status), inline = true },
            new { name = "⏰ Deadline", value = FormatDateWithVietnamTime(task.DueDate), inline = true },
            new { name = "\u200B", value = "\u200B", inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "👤 Submitted by", value = userName, inline = true },
            new { name = "💬 Reason", value = reason, inline = true },
            new { name = "🕐 Submitted at", value = createdAt, inline = true }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private static ChannelMessageContent BuildApproveSuccessMessage(ComplainDto complain, TaskDto? task, string? complainantName, string approverName)
    {
        var isExtend = complain.Type == "RequestExtend";
        var approvedTime = complain.ApprovedAt.HasValue
            ? FormatDateWithVietnamTime(complain.ApprovedAt.Value)
            : FormatDateWithVietnamTime(DateTime.UtcNow);
        var createdAt = FormatDateWithVietnamTime(complain.CreatedAt);

        var fields = new List<object>
    {
        new { name = "📋 Complaint ID", value = $"#{complain.Id}", inline = true },
        new { name = "🏷️ Type", value = isExtend ? "RequestExtend" : "RequestCancel", inline = true },
        new { name = "📌 Task", value = task?.Title ?? "N/A", inline = true },

        new { name = "\u200B", value = "\u200B", inline = false },

        new { name = "🔄 New status", value = GetStatusText(task?.Status), inline = true }
    };

        if (isExtend && complain.NewDueDate.HasValue)
        {
            fields.Add(new { name = "⏰ New deadline", value = FormatDateWithVietnamTime(complain.NewDueDate), inline = true });
            fields.Add(new { name = "\u200B", value = "\u200B", inline = true });
        }
        else
        {
            fields.Add(new { name = "\u200B", value = "\u200B", inline = true });
            fields.Add(new { name = "\u200B", value = "\u200B", inline = true });
        }

        fields.AddRange(new object[]
        {
        new { name = "\u200B", value = "\u200B", inline = false },

        new { name = "👤 Complainant", value = complainantName ?? "Unknown", inline = true },
        new { name = "👤 Approved by", value = approverName, inline = true },
        new { name = "\u200B", value = "\u200B", inline = true },

        new { name = "\u200B", value = "\u200B", inline = false },

        new { name = "🕐 Created at", value = createdAt, inline = true },
        new { name = "🕐 Approved at", value = approvedTime, inline = true },
        new { name = "\u200B", value = "\u200B", inline = true }
        });

        var embed = new
        {
            title = isExtend ? "✅ Deadline extension approved" : "✅ Task cancellation approved",
            color = "#00D26A",
            fields = fields.ToArray()
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }

    private static ChannelMessageContent BuildRejectSuccessMessage(ComplainDto complain, TaskDto? task, string? complainantName, string rejectorName, string rejectReason)
    {
        var isExtend = complain.Type == "RequestExtend";
        var rejectedTime = complain.ApprovedAt.HasValue
            ? FormatDateWithVietnamTime(complain.ApprovedAt.Value)
            : FormatDateWithVietnamTime(DateTime.UtcNow);
        var createdAt = FormatDateWithVietnamTime(complain.CreatedAt);

        var embed = new
        {
            title = "❌ Request rejected",
            color = "#FF3B30",
            fields = new object[]
            {
            new { name = "📋 Complaint ID", value = $"#{complain.Id}", inline = true },
            new { name = "🏷️ Type", value = isExtend ? "RequestExtend" : "RequestCancel", inline = true },
            new { name = "📌 Task", value = task?.Title ?? "N/A", inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "💬 Original reason", value = complain.Reason, inline = false },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "👤 Complainant", value = complainantName ?? "Unknown", inline = true },
            new { name = "👤 Rejected by", value = rejectorName, inline = true },
            new { name = "\u200B", value = "\u200B", inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "🕐 Created at", value = createdAt, inline = true },
            new { name = "🕐 Rejected at", value = rejectedTime, inline = true },
            new { name = "\u200B", value = "\u200B", inline = true },

            new { name = "\u200B", value = "\u200B", inline = false },

            new { name = "💬 Rejection reason", value = rejectReason, inline = false }
            }
        };

        return new ChannelMessageContent { Text = " ", Embed = new object[] { embed } };
    }
}