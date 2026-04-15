using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;

namespace TaskManagement.Bot.Application.Commands.Report;

public class ReportFormBuilder
{
    private static DateTime GetVietnamTime()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    private static string FormatVNTime()
    {
        return $"{GetVietnamTime():dd-MM-yyyy HH:mm:ss} (GMT+7)";
    }

    public static ChannelMessageContent BuildPersonalReportForm(
        PersonalReportDto report,
        string username)
    {
        var interactive = new
        {
            title = $"📊 PERSONAL REPORT - {username}",
            description = $"Task summary for {username}",
            color = "#0099ff",
            fields = new object[]
            {
                new { name = "👤 User", value = username, inline = true },
                new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
                new { name = "✅ Completed", value = report.CompletedTasks.ToString(), inline = true },
                new { name = "🚧 Doing", value = report.DoingTasks.ToString(), inline = true },
                new { name = "⏳ Todo", value = report.ToDoTasks.ToString(), inline = true },
                new { name = "⚠️ Late", value = report.LateTasks.ToString(), inline = true },
                new { name = "📈 Completion Rate", value = $"{report.CompletionRate:F2}%", inline = false }
            },
            footer = new
            {
                text = $"Được tạo vào ngày {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"Báo cáo thông kê cho Sốp iu đây, hẹ hẹ!",
            Embed = [interactive]
        };
    }

    public static ChannelMessageContent BuildTeamReportForm(
        TeamReportDto report,
        string? channelLabel)
    {
        var interactive = new
        {
            title = "👥 TEAM REPORT",
            description = $"Team task summary for {channelLabel ?? "this channel"}",
            color = "#00cc99",
            fields = new object[]
            {
                new { name = "👨 Total Members", value = report.TotalMembers.ToString(), inline = true },
                new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
                new { name = "✅ Completed", value = report.CompletedTasks.ToString(), inline = true },
                new { name = "⚠️ Overdue", value = report.TotalOverdueTasks.ToString(), inline = true },
                new { name = "📈 Team Completion", value = $"{report.TeamCompletionRate:F2}%", inline = false }
            },
            footer = new
            {
                text = $"Được tạo vào ngày {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"Báo cáo thông kê cho Sốp iu đây, hẹ hẹ!",
            Embed = [interactive]
        };
    }

    public static ChannelMessageContent BuildStatisticsReportForm(
        StatisticsReportDto report)
    {
        var timeRangeText = report.TimeRange.ToString().ToUpper();
        var periodDesc = report.TimeRange.ToString().ToLower();

        var interactive = new
        {
            title = $"📊 {timeRangeText} STATISTICS",
            description = $"Task statistics for {periodDesc} period",
            color = "#ff6600",
            fields = new object[]
            {
                new { name = "📌 Created", value = report.TaskCreated.ToString(), inline = true },
                new { name = "✅ Completed", value = report.TaskCompleted.ToString(), inline = true },
                new { name = "🚧 In Progress", value = report.TaskInProgress.ToString(), inline = true },
                new { name = "⏳ Pending", value = report.TaskPending.ToString(), inline = true },
                new { name = "⚠️ Overdue", value = report.OverdueTasks.ToString(), inline = true },
                new { name = "📈 Completion Rate", value = $"{report.CompletionRate:F2}%", inline = true }
            },
            footer = new
            {
                text = $"Period: {report.StartDate:dd-MM-yyyy} to {report.EndDate:dd-MM-yyyy} (VN Time) | Được tạo vào ngày {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"Báo cáo thông kê cho Sốp iu đây, hẹ hẹ!",
            Embed = [interactive]
        };
    }
}