using System.Text.RegularExpressions;
using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public static class TeamFormBuilder
{
    public static ChannelMessageContent BuildTeamForm(string clanId)
    {
        var interactive = new
        {
            title = "📋 Tạo Project và Team",
            description = "Vui lòng điền thông tin bên dưới:",
            color = "#5865F2",
            fields = new object[]
            {
                new
                {
                    name = "📁 Tên Project",
                    value = "",
                    inputs = new
                    {
                        id = "project_name",
                        type = 3,
                        component = new
                        {
                            id = "project_name_input",
                            placeholder = "Nhập tên project (tối đa 50 ký tự)",
                            defaultValue = "",
                            type = "text",
                            textarea = false,
                            maxLength = 50
                        }
                    }
                },
                new
                {
                    name = "👥 Tên Team",
                    value = "",
                    inputs = new
                    {
                        id = "team_name",
                        type = 3,
                        component = new
                        {
                            id = "team_name_input",
                            placeholder = "Nhập tên team (tối đa 20 ký tự)",
                            defaultValue = "",
                            type = "text",
                            textarea = false,
                            maxLength = 20
                        }
                    }
                },
                new
                {
                    name = "👥 Thành viên (3-6 người)",
                    value = "",
                    inputs = new
                    {
                        id = "members",
                        type = 3,
                        component = new
                        {
                            id = "members_input",
                            placeholder = "<@userId> hoặc @username, cách nhau bằng khoảng trắng",
                            defaultValue = "",
                            type = "text",
                            textarea = true
                        }
                    }
                }
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
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
                            id = $"CREATE_TEAM|{clanId}",
                            type = 1,
                            component = new
                            {
                                label = "✅ Tạo Team",
                                style = 3
                            }
                        },
                        new
                        {
                            id = $"ADD_MEMBER|{clanId}",
                            type = 1,
                            component = new
                            {
                                label = "➕ Thêm thành viên",
                                style = 2
                            }
                        },
                        new
                        {
                            id = $"CANCEL_TEAM|{clanId}",
                            type = 1,
                            component = new
                            {
                                label = "❌ Hủy",
                                style = 4
                            }
                        }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildAddMemberForm(string clanId, string requestId, int currentCount)
    {
        var interactive = new
        {
            title = "➕ Thêm thành viên mới",
            description = $"Hiện tại có {currentCount}/6 thành viên. Nhập thông tin thành viên mới:",
            color = "#57F287",
            fields = new object[]
            {
                new
                {
                    name = "Thành viên mới",
                    value = "",
                    inputs = new
                    {
                        id = "new_member",
                        type = 3,
                        component = new
                        {
                            id = "new_member_input",
                            placeholder = "<@userId> hoặc @username",
                            defaultValue = "",
                            type = "text",
                            textarea = false
                        }
                    }
                }
            }
        };

        return new ChannelMessageContent
        {
            Text = "interactive",
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
                            id = $"CONFIRM_ADD_MEMBER|{clanId}|{requestId}",
                            type = 1,
                            component = new
                            {
                                label = "✅ Xác nhận thêm",
                                style = 3
                            }
                        },
                        new
                        {
                            id = $"CANCEL_ADD_MEMBER|{clanId}|{requestId}",
                            type = 1,
                            component = new
                            {
                                label = "❌ Hủy",
                                style = 4
                            }
                        }
                    }
                }
            }
        };
    }

    public static (bool isValid, string message) ValidateForm(
        string projectName,
        string teamName,
        string role,
        string members,
        Func<string, Task<bool>>? checkProjectExists = null,
        Func<string, Task<bool>>? checkTeamExists = null)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return (false, "❌ Tên project không được để trống");
        }

        if (projectName.Length > 50)
        {
            return (false, "❌ Tên project tối đa 50 ký tự");
        }

        if (string.IsNullOrWhiteSpace(teamName))
        {
            return (false, "❌ Tên team không được để trống");
        }

        if (teamName.Length > 20)
        {
            return (false, "❌ Tên team tối đa 20 ký tự");
        }

        if (string.IsNullOrWhiteSpace(members))
        {
            return (false, "❌ Danh sách thành viên không được để trống");
        }

        var memberList = ExtractMemberIds(members);

        if (memberList.Count < 1)
        {
            return (false, $"❌ Team phải có ít nhất 3 thành viên (hiện tại: {memberList.Count})");
        }

        if (memberList.Count > 6)
        {
            return (false, $"❌ Team phải có ít nhất 3 thành viên (hiện tại: {memberList.Count})");
        }

        foreach (var member in memberList)
        {
            if (!IsValidMemberToken(member))
            {
                return (false, $"❌ Thành viên không hợp lệ: {member}");
            }
        }

        return (true, "✅ Hợp lệ");
    }

    public static List<string> ExtractMemberIds(string members)
    {
        if (string.IsNullOrWhiteSpace(members))
            return new List<string>();

        var tokens = members.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var memberIds = new List<string>();

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();

            // Format: <@123456789>
            if (trimmed.StartsWith("<@") && trimmed.EndsWith(">"))
            {
                var id = trimmed.Substring(2, trimmed.Length - 3);
                memberIds.Add(id);
            }
            // Format: @username
            else if (trimmed.StartsWith("@"))
            {
                memberIds.Add(trimmed);
            }
            // Format: 123456789
            else if (Regex.IsMatch(trimmed, @"^\d+$"))
            {
                memberIds.Add(trimmed);
            }
            else
            {
                memberIds.Add(trimmed);
            }
        }

        return memberIds.Distinct().ToList();
    }

    private static bool IsValidMemberToken(string member)
    {
        if (string.IsNullOrWhiteSpace(member))
        {
            return false;
        }

        return Regex.IsMatch(member, @"^@[A-Za-z0-9._-]+$")
            || Regex.IsMatch(member, @"^<@\d+>$")
            || Regex.IsMatch(member, @"^\d+$");
    }

    public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId, string clanId)
    {
        return new ChannelMessageContent
        {
            Text = $"Ban duoc moi tham gia vao team `{teamName}` voi project '{projectName}'",
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"ACCEPT|{requestId}|{userId}|{clanId}",
                            type = 1,
                            component = new { label = "Accept", style = 3 }
                        },
                        new
                        {
                            id = $"REJECT|{requestId}|{userId}|{clanId}",
                            type = 1,
                            component = new { label = "Reject", style = 4 }
                        }
                    }
                }
            }
        };
    }
}
