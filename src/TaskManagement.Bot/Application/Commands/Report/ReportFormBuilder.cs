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

    private static string BuildProjectSummary(ProjectStatisticsDto project)
    {
        return $"{project.ProjectName}: +{project.CreatedTasks}, closed {project.CompletedTasks}, todo {project.PendingTasks}, doing {project.DoingTasks}, review {project.ReviewTasks}, late {project.LateTasks}, overdue {project.OverdueTasks}";
    }

    private static string BuildProjectCoverageText(StatisticsReportDto report)
    {
        if (report.Projects.Count == 0)
        {
            return "No project data";
        }

        return string.Join(
            "\n",
            report.Projects
                .Take(3)
                .Select(BuildProjectSummary));
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
            description = $"Activity and current task snapshot for {periodDesc}",
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

    // ===== NEW COMPREHENSIVE REPORT FORMATTERS =====

    public static ChannelMessageContent BuildComprehensiveTaskReport(ComprehensiveTaskReportDto task)
    {
        var fields = new List<object>
        {
            new { name = $"{task.StatusIcon} Status", value = $"{task.Status} ({task.HealthStatus})", inline = true },
            new { name = $"{task.PriorityIcon} Priority", value = task.Priority.ToString(), inline = true },
            new { name = $"{task.TimeStatusIcon} Health", value = task.HealthStatus, inline = true },

            new { name = "📌 Title", value = task.Title, inline = false },
        };

        if (!string.IsNullOrEmpty(task.Description))
        {
            var desc = task.Description.Length > 200 ? task.Description.Substring(0, 200) + "..." : task.Description;
            fields.Add(new { name = "📝 Description", value = desc, inline = false });
        }

        fields.AddRange(new List<object>
        {
            new { name = "👤 Assignee", value = task.AssignedTo, inline = true },
            new { name = "👨‍💼 Created By", value = task.CreatedBy, inline = true },
        });

        if (!string.IsNullOrEmpty(task.TeamName))
            fields.Add(new { name = "👥 Team", value = task.TeamName, inline = true });
        if (!string.IsNullOrEmpty(task.ProjectName))
            fields.Add(new { name = "🎯 Project", value = task.ProjectName, inline = true });

        // Timeline info
        var dueDateStr = task.DueDate?.ToString("dd-MM-yyyy") ?? "No deadline";
        var createdStr = task.CreatedAt.ToString("dd-MM-yyyy HH:mm");
        fields.Add(new { name = "📅 Due Date", value = dueDateStr, inline = true });
        fields.Add(new { name = "⏱️ Created", value = createdStr, inline = true });

        // Time metrics
        if (task.DaysOverdue > 0)
            fields.Add(new { name = "🔴 Overdue", value = $"{task.DaysOverdue} days", inline = true });
        else if (task.DaysUntilDue != int.MaxValue && task.DaysUntilDue > 0)
            fields.Add(new { name = "🟡 Days Until Due", value = $"{task.DaysUntilDue} days ({(int)((double)task.DaysUntilDue / Math.Max(task.TotalDaysAllocated, 1) * 100)}%)", inline = true });

        // Progress
        if (task.TotalDaysAllocated > 0)
            fields.Add(new { name = "📈 Progress", value = $"{task.ProgressPercentage:F1}% ({task.ProgressPercentage:F0}% of timeline)", inline = false });

        // Associations
        var clans = string.Join(", ", task.ClanIds.Take(3));
        var channels = string.Join(", ", task.ChannelIds.Take(3));
        if (!string.IsNullOrEmpty(clans))
            fields.Add(new { name = "🏘️ Clans", value = clans, inline = true });
        if (!string.IsNullOrEmpty(channels))
            fields.Add(new { name = "📢 Channels", value = channels, inline = true });

        // Reminders & Complaints
        if (task.TotalReminders > 0)
        {
            var nextReminder = task.NextReminderAt?.ToString("HH:mm") ?? "N/A";
            fields.Add(new { name = "🔔 Reminders", value = $"{task.TotalReminders} total | {task.PendingReminders} pending | Next: {nextReminder}", inline = false });
        }

        if (task.TotalComplaints > 0)
        {
            var complaintSummary = $"Total: {task.TotalComplaints} | Pending: {task.PendingComplaints} | Approved: {task.ApprovedComplaints} | Rejected: {task.RejectedComplaints}";
            fields.Add(new { name = "⚠️ Complaints", value = complaintSummary, inline = false });
        }

        var color = task.HealthStatus switch
        {
            "Done" => "#00cc00",
            "Overdue" => "#ff0000",
            "At Risk" => "#ffaa00",
            "On Track" => "#0099ff",
            _ => "#808080"
        };

        var interactive = new
        {
            title = $"{task.StatusIcon} Task #{task.Id}: {task.Title}",
            description = $"{task.TimeStatusIcon} {task.HealthStatus}",
            color = color,
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Generated {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = "📊 Chi tiết công việc chi tiết",
            Embed = [interactive]
        };
    }

    public static ChannelMessageContent BuildEnhancedPersonalTaskReport(EnhancedPersonalTaskReportDto report, string username)
    {
        var healthStatus = report.HealthScore >= 80 ? "🟢 Excellent"
            : report.HealthScore >= 60 ? "🟡 Good"
            : report.HealthScore >= 40 ? "🟠 Fair"
            : "🔴 Needs Attention";

        var fields = new List<object>
        {
            new { name = "👤 User", value = username, inline = true },
            new { name = "📊 Health Score", value = $"{report.HealthScore:F0}/100 {healthStatus}", inline = true },

            new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
            new { name = "✅ Completed", value = report.CompletedCount.ToString(), inline = true },
            new { name = "🚧 Doing", value = report.DoingCount.ToString(), inline = true },
            new { name = "📋 Review", value = report.ReviewCount.ToString(), inline = true },

            new { name = "⏳ Todo", value = report.ToDoCount.ToString(), inline = true },
            new { name = "⏸️ Paused", value = report.PausedCount.ToString(), inline = true },
            new { name = "⚠️ Late", value = report.LateCount.ToString(), inline = true },

            new { name = "📈 Completion Rate", value = $"{report.CompletionRate:F1}%", inline = true },
            new { name = "🔴 Overdue Tasks", value = report.OverdueTasksCount.ToString(), inline = true },
            new { name = "🟡 At-Risk Tasks", value = report.AtRiskTasksCount.ToString(), inline = true },

            new { name = "📅 Total Overdue Days", value = report.TotalOverdueDays.ToString(), inline = true },
            new { name = "👥 Teams", value = report.TeamCount.ToString(), inline = true },
            new { name = "🔔 Pending Reminders", value = report.TotalPendingReminders.ToString(), inline = true },
        };

        if (report.OverdueTasks.Any())
        {
            var overdueStr = string.Join(", ", report.OverdueTasks.Take(3).Select(t => $"{t.Title} ({t.DaysOverdue}d)"));
            fields.Add(new { name = "🔴 Overdue", value = overdueStr, inline = false });
        }

        if (report.AtRiskTasks.Any())
        {
            var atRiskStr = string.Join(", ", report.AtRiskTasks.Take(3).Select(t => $"{t.Title} ({t.DaysUntilDue}d)"));
            fields.Add(new { name = "🟡 At Risk", value = atRiskStr, inline = false });
        }

        var interactive = new
        {
            title = $"📊 PERSONAL REPORT - {username}",
            description = $"Health: {healthStatus}",
            color = report.HealthScore >= 80 ? "#00cc00" : report.HealthScore >= 60 ? "#ffaa00" : "#ff0000",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Generated {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"📊 Báo cáo chi tiết cho {username}",
            Embed = [interactive]
        };
    }

    public static ChannelMessageContent BuildTeamHealthReport(TeamHealthReportDto report)
    {
        var healthColor = report.TeamHealthStatus switch
        {
            "Healthy" => "#00cc00",
            "At Risk" => "#ffaa00",
            "Critical" => "#ff0000",
            _ => "#808080"
        };

        var healthEmoji = report.TeamHealthStatus switch
        {
            "Healthy" => "🟢",
            "At Risk" => "🟡",
            "Critical" => "🔴",
            _ => "❓"
        };

        var fields = new List<object>
        {
            new { name = $"{healthEmoji} Team Health", value = report.TeamHealthStatus, inline = true },
            new { name = "👥 Total Members", value = report.TotalMembers.ToString(), inline = true },
            new { name = "🔧 Active Members", value = report.ActiveMembers.ToString(), inline = true },

            new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
            new { name = "✅ Completed", value = report.CompletedTasks.ToString(), inline = true },
            new { name = "📈 Completion Rate", value = $"{report.TeamCompletionRate:F1}%", inline = true },

            new { name = "🔴 Overdue", value = $"{report.OverdueTasksCount} ({report.OverduePercentage:F1}%)", inline = true },
            new { name = "🟡 At Risk", value = report.AtRiskTasksCount.ToString(), inline = true },

            new { name = "🔥 Critical Priority", value = report.CriticalPriorityTasks.ToString(), inline = true },
            new { name = "🔴 High Priority", value = report.HighPriorityTasks.ToString(), inline = true },
            new { name = "🟡 Medium Priority", value = report.MediumPriorityTasks.ToString(), inline = true },
            new { name = "🟢 Low Priority", value = report.LowPriorityTasks.ToString(), inline = true },
        };

        if (report.MemberBreakdowns.Any())
        {
            var topMembers = report.MemberBreakdowns.OrderByDescending(m => m.CompletionRate).Take(3);
            var memberStats = string.Join(" | ", topMembers.Select(m => $"{m.MemberId}: {m.CompletionRate:F0}%"));
            fields.Add(new { name = "👥 Top Performers", value = memberStats, inline = false });
        }

        var interactive = new
        {
            title = $"👥 TEAM HEALTH - {report.TeamName}",
            description = $"{healthEmoji} {report.TeamHealthStatus}",
            color = healthColor,
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Project: {report.ProjectName ?? "N/A"} | {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = $"👥 Báo cáo sức khỏe team {report.TeamName}",
            Embed = [interactive]
        };
    }

    public static ChannelMessageContent BuildTaskAnalyticsReport(TaskAnalyticsReportDto analytics)
    {
        var fields = new List<object>
        {
            new { name = "📊 Period", value = analytics.ReportPeriod?.ToString() ?? "Custom", inline = true },
            new { name = "📅 Range", value = $"{analytics.PeriodStartDate:dd-MM} to {analytics.PeriodEndDate:dd-MM}", inline = true },

            new { name = "📌 Tasks Created", value = analytics.TasksCreated.ToString(), inline = true },
            new { name = "✅ Tasks Completed", value = analytics.TasksCompleted.ToString(), inline = true },
            new { name = "📈 Delivery Rate", value = $"{analytics.DeliveryRate:F1}%", inline = true },
            new { name = "🚀 Velocity", value = $"{analytics.CompletionVelocity:F2}/day", inline = true },

            new { name = "🔥 Critical Created", value = analytics.CriticalTasksCompleted.ToString(), inline = true },
            new { name = "🔥 Critical Completed", value = analytics.CriticalTasksPending.ToString(), inline = true },
            new { name = "🔥 Critical Rate", value = $"{analytics.CriticalCompletionRate:F1}%", inline = true },

            new { name = "⚠️ Total Complaints", value = analytics.TotalComplaints.ToString(), inline = true },
            new { name = "📅 Extensions", value = analytics.TimeExtensionRequests.ToString(), inline = true },
            new { name = "❌ Cancellations", value = analytics.CancellationRequests.ToString(), inline = true },

            new { name = "📊 Approval Rate", value = $"{analytics.ApprovalRate:F1}%", inline = false },
        };

        var interactive = new
        {
            title = "📊 TASK ANALYTICS REPORT",
            description = $"Performance metrics for {analytics.ReportPeriod}",
            color = "#0099ff",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Generated {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Text = "📊 Báo cáo phân tích năng suất",
            Embed = [interactive]
        };
    }
}
