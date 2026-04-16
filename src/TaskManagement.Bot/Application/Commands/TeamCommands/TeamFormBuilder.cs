using System.Text.RegularExpressions;
using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public static class TeamFormBuilder
{
    public static ChannelMessageContent BuildTeamForm(string clanId, List<string>? existingMembers = null, int memberCount = 3)
    {
        if (existingMembers == null)
        {
            existingMembers = new List<string>();
            // Khởi tạo 3 trường member mặc định
            for (int i = 0; i < 3; i++)
            {
                existingMembers.Add("");
            }
        }

        var fields = new List<object>();

        // Field Tên Project
        fields.Add(new
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
        });

        // Field Tên Team
        fields.Add(new
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
        });

        // Các field Thành viên (dynamic)
        for (int i = 0; i < memberCount; i++)
        {
            var memberIndex = i + 1;
            fields.Add(new
            {
                name = $"👤 Thành viên {memberIndex}",
                value = existingMembers.Count > i ? existingMembers[i] : "",
                inputs = new
                {
                    id = $"member_{memberIndex}",
                    type = 3,
                    component = new
                    {
                        id = $"member_{memberIndex}_input",
                        placeholder = $"<@userId> hoặc @username (thành viên {memberIndex})",
                        defaultValue = existingMembers.Count > i ? existingMembers[i] : "",
                        type = "text",
                        textarea = false
                    }
                }
            });
        }

        var interactive = new
        {
            title = "📋 Tạo Project và Team",
            description = "Vui lòng điền thông tin bên dưới:",
            color = "#5865F2",
            fields = fields.ToArray()
        };

        var components = new List<object>();

        // Nút Thêm thành viên (nếu chưa đủ 6)
        if (memberCount < 6)
        {
            components.Add(new
            {
                id = $"ADD_MEMBER_FIELD|{clanId}|{memberCount}",
                type = 1,
                component = new
                {
                    label = "➕ Thêm thành viên",
                    style = 2
                }
            });
        }

        // Nút Tạo Team
        components.Add(new
        {
            id = $"CREATE_TEAM|{clanId}",
            type = 1,
            component = new
            {
                label = "✅ Tạo Team",
                style = 3
            }
        });

        // Nút Hủy
        components.Add(new
        {
            id = $"CANCEL_TEAM|{clanId}",
            type = 1,
            component = new
            {
                label = "❌ Hủy",
                style = 4
            }
        });

        return new ChannelMessageContent
        {
            Text = "interactive",
            Embed = new[] { interactive },
            Components = new[]
            {
                new
                {
                    type = 1,
                    components = components.ToArray()
                }
            }
        };
    }

    public static ChannelMessageContent BuildAddMemberFieldForm(string clanId, int currentCount, string projectName, string teamName, List<string> existingMembers)
    {
        // Tăng số lượng member lên 1
        var newCount = currentCount + 1;

        // Thêm member rỗng mới
        var newMembers = new List<string>(existingMembers);
        while (newMembers.Count < newCount)
        {
            newMembers.Add("");
        }

        return BuildTeamForm(clanId, newMembers, newCount);
    }

    public static (bool isValid, string message, List<string> memberList) ValidateFormWithMembers(
        string projectName,
        string teamName,
        Dictionary<string, string> formValues)
    {
        // Validate Project Name
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return (false, "❌ Tên project không được để trống", null);
        }

        if (projectName.Length > 50)
        {
            return (false, "❌ Tên project tối đa 50 ký tự", null);
        }

        // Validate Team Name
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return (false, "❌ Tên team không được để trống", null);
        }

        if (teamName.Length > 20)
        {
            return (false, "❌ Tên team tối đa 20 ký tự", null);
        }

        // Lấy danh sách members từ form values
        var memberList = new List<string>();
        for (int i = 1; i <= 6; i++)
        {
            var memberValue = formValues.GetValueOrDefault($"member_{i}", "");
            if (!string.IsNullOrWhiteSpace(memberValue))
            {
                var ids = ExtractMemberIds(memberValue);
                foreach (var id in ids)
                {
                    if (!memberList.Contains(id))
                    {
                        memberList.Add(id);
                    }
                }
            }
        }

        // Validate số lượng members
        if (memberList.Count < 3)
        {
            return (false, $"❌ Team phải có ít nhất 3 thành viên (hiện tại: {memberList.Count})", null);
        }

        if (memberList.Count > 6)
        {
            return (false, $"❌ Team tối đa 6 thành viên (hiện tại: {memberList.Count})", null);
        }

        // Validate từng member
        foreach (var member in memberList)
        {
            if (!IsValidMemberToken(member))
            {
                return (false, $"❌ Thành viên không hợp lệ: {member}", null);
            }
        }

        return (true, "✅ Hợp lệ", memberList);
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

            if (trimmed.StartsWith("<@") && trimmed.EndsWith(">"))
            {
                var id = trimmed.Substring(2, trimmed.Length - 3);
                memberIds.Add(id);
            }
            else if (trimmed.StartsWith("@"))
            {
                memberIds.Add(trimmed);
            }
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
            return false;

        return Regex.IsMatch(member, @"^@[A-Za-z0-9._-]+$")
            || Regex.IsMatch(member, @"^<@\d+>$")
            || Regex.IsMatch(member, @"^\d+$");
    }

    public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId, string clanId)
    {
        return new ChannelMessageContent
        {
            Text = $"✨ Bạn được mời tham gia team **{teamName}** của project **{projectName}**\n\nVui lòng xác nhận trong vòng 30 phút:",
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
                            component = new { label = "✅ Chấp nhận", style = 3 }
                        },
                        new
                        {
                            id = $"REJECT|{requestId}|{userId}|{clanId}",
                            type = 1,
                            component = new { label = "❌ Từ chối", style = 4 }
                        }
                    }
                }
            }
        };
    }
}