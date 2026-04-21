using Mezon.Sdk.Builders;
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

    public static ChannelMessageContent BuildReportFilterForm(
        PMProjectListDto projectList,
        string clanId,
        int? selectedProjectId = null,
        List<TeamSummaryDto>? teams = null)
    {
        if (projectList.Projects.Count == 0)
        {
            return new ChannelMessageContent
            {
                Text = "❌ Bạn chưa tạo project nào. Sử dụng lệnh `!create team` để tạo project và team."
            };
        }

        var fields = new List<object>();

        // 1. PROJECT SELECT - Always show
        var projectOptions = projectList.Projects.Select(p => new
        {
            label = $"{p.ProjectName} ({p.TeamCount} teams, {p.TotalTasks} tasks)",
            value = p.ProjectId.ToString()
        }).ToArray();

        if (selectedProjectId.HasValue)
        {
            var selectedProject = projectList.Projects.FirstOrDefault(p => p.ProjectId == selectedProjectId.Value);
            if (selectedProject != null)
            {
                fields.Add(new
                {
                    name = "✅ Project đã chọn",
                    value = $"**{selectedProject.ProjectName}**\n_{selectedProject.TeamCount} teams, {selectedProject.TotalTasks} tasks_",
                    inline = false
                });
            }
        }
        else
        {
            fields.Add(new
            {
                name = "📁 Chọn Project",
                value = string.Empty,
                inputs = new
                {
                    id = "report_project_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Chọn Project...",
                        options = projectOptions
                    }
                }
            });
        }

        if (teams != null && teams.Count > 0)
        {
            var teamOptions = teams.Select(t => new
            {
                label = $"{t.TeamName} ({t.MemberCount} members, {t.TotalTasks} tasks)",
                value = t.TeamId.ToString()
            }).ToArray();

            fields.Add(new
            {
                name = "👥 Chọn Team",
                value = string.Empty,
                inputs = new
                {
                    id = "report_team_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Chọn Team...",
                        options = teamOptions
                    }
                }
            });
        }
        else if (selectedProjectId.HasValue)
        {
            fields.Add(new
            {
                name = "👥 Chọn Team",
                value = "_Project này chưa có team nào_",
                inline = false
            });
        }
        else
        {
            fields.Add(new
            {
                name = "👥 Chọn Team",
                value = "_Vui lòng chọn Project trước_",
                inline = false
            });
        }

        var description = !selectedProjectId.HasValue
            ? "**Bước 1:** Chọn Project để load danh sách Team"
            : teams != null && teams.Count > 0
                ? "**Bước 2:** Chọn Team và nhấn 'Xem báo cáo'"
                : "⚠️ Project này chưa có team nào";

        var components = new[]
        {
            new
            {
                id = $"REPORT_VIEW|{clanId}",
                type = 1,
                component = new { label = "📊 Xem báo cáo", style = 3 }
            },
            new
            {
                id = $"REPORT_CANCEL|{clanId}",
                type = 1,
                component = new { label = "❌ Hủy", style = 4 }
            }
        };

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📊 BÁO CÁO TEAM",
                    description = description,
                    color = "#5865F2",
                    fields = fields.ToArray(),
                    footer = new
                    {
                        text = "Chọn Project → Team → Xem báo cáo"
                    }
                }
            },
            Components = new[] { new { components = components } }
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
