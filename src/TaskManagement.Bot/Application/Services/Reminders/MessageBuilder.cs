using System.Globalization;
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services.Reminders;

public static class MessageBuilder
{
    private const string UnknownValue = "Unknown";
    private const string NoneValue = "None";
    private const string UnknownUserValue = "Unknown User";

    public static ChannelMessageContent BuildReminderNotification(
        Reminder reminder,
        string? assigneeUsername,
        TimeZoneInfo? timeZone = null)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        timeZone ??= TimeZoneInfo.Utc;

        var task = reminder.Task;
        var rule = reminder.ReminderRule;
        var triggerType = rule?.TriggerType;
        var taskId = task?.Id ?? reminder.TaskId;
        var projectTitle = Normalize(task?.Title, $"Task #{taskId}");
        var fields = new List<object>
        {
            BuildField("👤 Người được giao", Normalize(assigneeUsername, UnknownUserValue), inline: true),
            BuildField("📁 Project", Normalize(task?.Team?.Project?.Name, NoneValue), inline: true),
            BuildField("👥 Team", Normalize(task?.Team?.Name, NoneValue), inline: true),
            BuildField("📌 Tiêu đề", Normalize(task?.Title, UnknownValue), inline: false),
            BuildField("⏰ Deadline", FormatDateTime(task?.DueDate, timeZone), inline: true),
            BuildField("📊 Trạng thái", GetStatusText(task?.Status), inline: true),
            BuildField("⚡ Độ ưu tiên", GetPriorityText(task?.Priority), inline: true),
            BuildField("🔔 Loại reminder", FormatRule(rule), inline: false),
            BuildField("🕒 Thời điểm nhắc", FormatDateTime(reminder.NextTriggerAt ?? reminder.TriggerAt, timeZone), inline: true)
        };

        if (!string.IsNullOrWhiteSpace(task?.Description))
        {
            fields.Add(BuildField("📝 Mô tả", task.Description.Trim(), inline: false));
        }

        var embed = new
        {
            title = $"{GetReminderTitle(triggerType)} - {projectTitle}",
            description = GetReminderDescription(triggerType),
            color = GetReminderColor(triggerType),
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Được tạo vào {FormatCurrentTime(timeZone)}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"{GetReminderLabel(triggerType)} {projectTitle}",
            Embed = new object[] { embed }
        };
    }

    private static object BuildField(string name, string value, bool inline) =>
        new { name, value, inline };

    private static string FormatCurrentTime(TimeZoneInfo timeZone)
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        return $"{localTime:dd-MM-yyyy HH:mm:ss} ({FormatOffset(localTime.Offset)})";
    }

    private static string FormatDateTime(DateTime? value) =>
        value?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? NoneValue;

    private static string FormatDateTime(DateTime? value, TimeZoneInfo timeZone)
    {
        if (!value.HasValue)
        {
            return NoneValue;
        }

        var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(ToUtcDateTime(value.Value), timeZone);
        return localDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static DateTime ToUtcDateTime(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Utc => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absoluteOffset = offset.Duration();
        return $"UTC{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}";
    }

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string FormatRule(ReminderRule? rule)
    {
        if (rule == null)
        {
            return NoneValue;
        }

        var triggerLabel = GetTriggerLabel(rule.TriggerType);
        if (rule.TriggerType == EReminderTriggerType.OnDeadline)
        {
            return triggerLabel;
        }

        var interval = FormatInterval(rule.Value, rule.IntervalUnit);
        return string.IsNullOrWhiteSpace(interval)
            ? triggerLabel
            : $"{triggerLabel} ({interval})";
    }

    private static string FormatInterval(double value, ETimeUnit? unit)
    {
        if (value <= 0 || unit == null)
        {
            return string.Empty;
        }

        var formattedValue = value % 1 == 0
            ? ((int)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

        return $"{formattedValue} {GetTimeUnitLabel(unit.Value)}";
    }

    private static string GetReminderLabel(EReminderTriggerType? triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => "Deadline",
            EReminderTriggerType.BeforeDeadline => "Reminder",
            EReminderTriggerType.AfterDeadline => "Overdue",
            EReminderTriggerType.Repeat => "Repeat Reminder",
            _ => "Reminder"
        };

    private static string GetReminderTitle(EReminderTriggerType? triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => "⏰ TASK ĐẾN DEADLINE",
            EReminderTriggerType.BeforeDeadline => "🔔 NHẮC TASK SẮP ĐẾN HẠN",
            EReminderTriggerType.AfterDeadline => "⚠️ TASK QUÁ HẠN",
            EReminderTriggerType.Repeat => "🔁 NHẮC TASK LẶP LẠI",
            _ => "🔔 REMINDER TASK"
        };

    private static string GetReminderDescription(EReminderTriggerType? triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => "Task đã đến thời điểm deadline.",
            EReminderTriggerType.BeforeDeadline => "Task sắp đến deadline. Vui lòng kiểm tra tiến độ.",
            EReminderTriggerType.AfterDeadline => "Task đã quá deadline. Vui lòng cập nhật trạng thái.",
            EReminderTriggerType.Repeat => "Reminder lặp lại cho task chưa hoàn tất.",
            _ => "Thông tin reminder task."
        };

    private static string GetReminderColor(EReminderTriggerType? triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => "#ED4245",
            EReminderTriggerType.BeforeDeadline => "#FEE75C",
            EReminderTriggerType.AfterDeadline => "#ED4245",
            EReminderTriggerType.Repeat => "#5865F2",
            _ => "#5865F2"
        };

    private static string GetTriggerLabel(EReminderTriggerType triggerType) =>
        triggerType switch
        {
            EReminderTriggerType.OnDeadline => "Đúng deadline",
            EReminderTriggerType.BeforeDeadline => "Trước deadline",
            EReminderTriggerType.AfterDeadline => "Sau deadline",
            EReminderTriggerType.Repeat => "Lặp lại",
            _ => triggerType.ToString()
        };

    private static string GetTimeUnitLabel(ETimeUnit unit) =>
        unit switch
        {
            ETimeUnit.Minutes => "phút",
            ETimeUnit.Hours => "giờ",
            ETimeUnit.Days => "ngày",
            ETimeUnit.Weeks => "tuần",
            _ => unit.ToString()
        };

    private static string GetStatusText(ETaskStatus? status) =>
        status switch
        {
            ETaskStatus.ToDo => "📋 ToDo",
            ETaskStatus.Doing => "🔄 Doing",
            ETaskStatus.Review => "✅ Review",
            ETaskStatus.Late => "⚠️ Late",
            ETaskStatus.Completed => "✔️ Completed",
            ETaskStatus.Cancelled => "❌ Cancelled",
            _ => UnknownValue
        };

    private static string GetPriorityText(EPriorityLevel? priority) =>
        priority switch
        {
            EPriorityLevel.High => "🔴 Cao",
            EPriorityLevel.Medium => "🟡 Trung bình",
            EPriorityLevel.Low => "🟢 Thấp",
            _ => UnknownValue
        };
}
