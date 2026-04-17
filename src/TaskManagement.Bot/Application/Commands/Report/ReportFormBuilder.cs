using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.Report;

public static class ReportFormBuilder
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

    public static ChannelMessageContent BuildUserPersonalReportForm(UserPersonalReportDto report)
    {
        var fields = new List<object>
        {
            new { name = "👤 User", value = report.Username, inline = true },
            new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
            new { name = "✅ Completed", value = report.CompletedTasks.ToString(), inline = true },
            new { name = "📈 Completion Rate", value = $"{report.CompletionRate:F1}%", inline = true },
        };

        foreach (var project in report.Projects)
        {
            fields.Add(new
            {
                name = $"🎯 Project: {project.ProjectName}",
                value = "━━━━━━━━━━━━━━━━━━━━",
                inline = false
            });

            foreach (var team in project.Teams)
            {
                var taskList = team.Tasks.Count == 0
                    ? "  _Không có task_"
                    : string.Join("\n", team.Tasks.Select(t =>
                        $"  • **{t.Title}** - {GetStatusEmoji(t.Status)} {t.Status} | {GetPriorityEmoji(t.Priority)} {t.Priority} | 📅 {t.DueDate:dd/MM/yyyy}"));

                var teamSummary = $"**👥 Team: {team.TeamName}**\n" +
                                  $"📊 {team.TotalTasks} tasks | ✅ {team.CompletedTasks} completed | 📈 {team.CompletionRate:F1}%\n\n" +
                                  $"{taskList}";

                fields.Add(new
                {
                    name = $"",
                    value = teamSummary,
                    inline = false
                });
            }
        }

        if (report.TotalTasks == 0)
        {
            fields.Add(new
            {
                name = "ℹ️ Thông báo",
                value = "Bạn chưa có task nào được giao.",
                inline = false
            });
        }

        var interactive = new
        {
            title = $"📊 BÁO CÁO CÁ NHÂN - {report.Username}",
            description = "Danh sách task của bạn theo Project và Team",
            color = "#0099ff",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Được tạo vào {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[] { interactive }
        };
    }

    public static ChannelMessageContent BuildPMProjectSelectionForm(PMProjectListDto report, string clanId)
    {
        if (report.Projects.Count == 0)
        {
            return new ChannelMessageContent
            {
                Text = "❌ Bạn chưa tạo project nào. Sử dụng lệnh `!create team` để tạo project và team."
            };
        }

        var projectOptions = report.Projects.Select(p => new
        {
            label = $"{p.ProjectName} ({p.TeamCount} teams, {p.TotalTasks} tasks)",
            value = p.ProjectId.ToString()
        }).ToArray();

        var fields = new List<object>
        {
            new
            {
                name = "📁 Danh sách Projects",
                value = string.Join("\n", report.Projects.Select(p =>
                    $"• **{p.ProjectName}** - {p.TeamCount} teams, {p.TotalTasks} tasks")),
                inline = false
            }
        };

        var interactive = new
        {
            title = "📊 BÁO CÁO TEAM - Chọn Project",
            description = "Vui lòng chọn Project để xem báo cáo:",
            color = "#5865F2",
            fields = fields.ToArray(),
            footer = new
            {
                text = "Chọn project từ dropdown bên dưới và nhấn 'Tiếp tục'"
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = "selected_project",
                            type = 2,
                            component = new
                            {
                                placeholder = "Chọn project...",
                                options = projectOptions
                            }
                        }
                    }
                },
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"REPORT_SELECT_PROJECT|{clanId}",
                            type = 1,
                            component = new { label = "➡️ Tiếp tục", style = 3 }
                        },
                        new
                        {
                            id = $"REPORT_CANCEL|{clanId}",
                            type = 1,
                            component = new { label = "❌ Hủy", style = 4 }
                        }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildTeamSelectionForm(int projectId, string projectName, List<TeamSummaryDto> teams, string clanId)
    {
        if (teams.Count == 0)
        {
            return new ChannelMessageContent
            {
                Text = $"❌ Project **{projectName}** chưa có team nào."
            };
        }

        var teamOptions = teams.Select(t => new
        {
            label = $"{t.TeamName} ({t.MemberCount} members, {t.TotalTasks} tasks)",
            value = t.TeamId.ToString()
        }).ToArray();

        var fields = new List<object>
        {
            new
            {
                name = "👥 Danh sách Teams",
                value = string.Join("\n", teams.Select(t =>
                    $"• **{t.TeamName}** - {t.MemberCount} members, {t.TotalTasks} tasks")),
                inline = false
            }
        };

        var interactive = new
        {
            title = $"📊 BÁO CÁO TEAM - Chọn Team",
            description = $"Project: **{projectName}**",
            color = "#5865F2",
            fields = fields.ToArray(),
            footer = new
            {
                text = "Chọn team từ dropdown bên dưới và nhấn 'Xem báo cáo'"
            }
        };

        return new ChannelMessageContent
        {
            Text = "📊 Chọn Team để xem báo cáo",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = "selected_team",
                            type = 2,
                            component = new
                            {
                                placeholder = "Chọn team...",
                                options = teamOptions
                            }
                        }
                    }
                },
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"REPORT_SELECT_TEAM|{clanId}|{projectId}",
                            type = 1,
                            component = new { label = "➡️ Xem báo cáo", style = 3 }
                        },
                        new
                        {
                            id = $"REPORT_BACK_PROJECT|{clanId}",
                            type = 1,
                            component = new { label = "⬅️ Quay lại", style = 2 }
                        },
                        new
                        {
                            id = $"REPORT_CANCEL|{clanId}",
                            type = 1,
                            component = new { label = "❌ Hủy", style = 4 }
                        }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildTeamDetailReportForm(TeamDetailReportDto report)
    {
        var fields = new List<object>
        {
            new { name = "🎯 Project", value = report.ProjectName, inline = true },
            new { name = "👥 Team", value = report.TeamName, inline = true },
            new { name = "👨‍💼 Members", value = report.Members.Count.ToString(), inline = true },
        };

        fields.Add(new
        {
            name = "━━━━━━━━━━━━━━━━━━━━",
            value = "**CHI TIẾT THEO MEMBER**",
            inline = false
        });

        foreach (var member in report.Members)
        {
            var taskList = member.Tasks.Count == 0
                ? "  _Không có task_"
                : string.Join("\n", member.Tasks.Select(t =>
                    $"  • **{t.Title}** - {GetStatusEmoji(t.Status)} {t.Status} | {GetPriorityEmoji(t.Priority)} {t.Priority} | 📅 {t.DueDate:dd/MM/yyyy}"));

            var memberSummary = $"**👤 {member.Username}**\n" +
                                $"📊 {member.TotalTasks} tasks | ✅ {member.CompletedTasks} completed | 📈 {member.CompletionRate:F1}%\n\n" +
                                $"{taskList}";

            fields.Add(new
            {
                name = "",
                value = memberSummary,
                inline = false
            });
        }

        if (report.Members.Count == 0 || report.Members.All(m => m.TotalTasks == 0))
        {
            fields.Add(new
            {
                name = "ℹ️ Thông báo",
                value = "Team chưa có task nào.",
                inline = false
            });
        }

        var interactive = new
        {
            title = $"📊 BÁO CÁO TEAM - {report.TeamName}",
            description = $"Project: {report.ProjectName}",
            color = "#00cc99",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Được tạo vào {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[] { interactive }
        };
    }

    public static ChannelMessageContent BuildTimeBasedReportForm(TimeBasedReportDto report)
    {
        var timeRangeText = report.TimeRange switch
        {
            Application.Services.TimeRangeFilter.Today => "HÔM NAY",
            Application.Services.TimeRangeFilter.Week => "TUẦN NÀY",
            Application.Services.TimeRangeFilter.Month => "THÁNG NÀY",
            _ => "CUSTOM"
        };

        var fields = new List<object>
        {
            new { name = "📅 Khoảng thời gian", value = $"{report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}", inline = false },
            new { name = "👥 Số members", value = report.Members.Count.ToString(), inline = true },
            new { name = "📌 Tổng tasks", value = report.Members.Sum(m => m.TotalTasks).ToString(), inline = true },
        };

        fields.Add(new
        {
            name = "━━━━━━━━━━━━━━━━━━━━",
            value = "**CHI TIẾT THEO MEMBER**",
            inline = false
        });

        foreach (var member in report.Members)
        {
            var taskList = member.Tasks.Count == 0
                ? "  _Không có task_"
                : string.Join("\n", member.Tasks.Take(5).Select(t =>
                    $"  • **{t.Title}** - {GetStatusEmoji(t.Status)} {t.Status} | 📅 {t.DueDate:dd/MM/yyyy}"));

            if (member.Tasks.Count > 5)
            {
                taskList += $"\n  _... và {member.Tasks.Count - 5} tasks khác_";
            }

            var memberSummary = $"**👤 {member.Username}** ({member.TotalTasks} tasks)\n{taskList}";

            fields.Add(new
            {
                name = "",
                value = memberSummary,
                inline = false
            });
        }

        if (report.Members.Count == 0 || report.Members.All(m => m.TotalTasks == 0))
        {
            fields.Add(new
            {
                name = "ℹ️ Thông báo",
                value = $"Không có task nào trong khoảng thời gian {timeRangeText.ToLower()}.",
                inline = false
            });
        }

        var interactive = new
        {
            title = $"📊 BÁO CÁO {timeRangeText}",
            description = $"Tasks có deadline từ {report.StartDate:dd/MM/yyyy} đến {report.EndDate:dd/MM/yyyy}",
            color = "#9b59b6",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Được tạo vào {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[] { interactive }
        };
    }

    public static ChannelMessageContent BuildUserReportByPMForm(UserReportByPMDto report)
    {
        var fields = new List<object>
        {
            new { name = "👤 User", value = report.Username, inline = true },
            new { name = "📌 Total Tasks", value = report.TotalTasks.ToString(), inline = true },
            new { name = "✅ Completed", value = report.CompletedTasks.ToString(), inline = true },
            new { name = "📈 Completion Rate", value = $"{report.CompletionRate:F1}%", inline = true },
        };

        if (report.StatusBreakdown.Any())
        {
            var statusText = string.Join(" | ", report.StatusBreakdown.Select(kvp =>
                $"{GetStatusEmoji(kvp.Key)} {kvp.Key}: {kvp.Value}"));

            fields.Add(new
            {
                name = "📊 Phân bố trạng thái",
                value = statusText,
                inline = false
            });
        }

        if (report.Tasks.Any())
        {
            var taskList = string.Join("\n", report.Tasks.Select(t =>
                $"• **{t.Title}** - {GetStatusEmoji(t.Status)} {t.Status} | {GetPriorityEmoji(t.Priority)} {t.Priority} | 📅 {t.DueDate:dd/MM/yyyy}"));

            fields.Add(new
            {
                name = "📝 Danh sách Tasks",
                value = taskList,
                inline = false
            });
        }
        else
        {
            fields.Add(new
            {
                name = "ℹ️ Thông báo",
                value = "User chưa có task nào.",
                inline = false
            });
        }

        var interactive = new
        {
            title = $"📊 BÁO CÁO USER - {report.Username}",
            description = "Báo cáo chi tiết tasks của user trong các Project bạn quản lý",
            color = "#ff6600",
            fields = fields.ToArray(),
            footer = new
            {
                text = $"Được tạo vào {FormatVNTime()}"
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[] { interactive }
        };
    }

    private static string GetStatusEmoji(ETaskStatus status)
    {
        return status switch
        {
            ETaskStatus.ToDo => "⏳",
            ETaskStatus.Doing => "🚧",
            ETaskStatus.Review => "👀",
            ETaskStatus.Late => "⚠️",
            ETaskStatus.Completed => "✅",
            ETaskStatus.Cancelled => "❌",
            _ => "❓"
        };
    }

    private static string GetPriorityEmoji(EPriorityLevel priority)
    {
        return priority switch
        {
            EPriorityLevel.Low => "🟢",
            EPriorityLevel.Medium => "🟡",
            EPriorityLevel.High => "🔴",
            _ => "⚪"
        };
    }
}
