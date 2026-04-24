using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public static class TeamFormBuilder
{
    public static ChannelMessageContent BuildTeamForm(int memberCount = 2, string? originalMessageId = null, string? senderId = null)
    {
        var fields = new List<object>
        {
            new
            {
                name = "📁 Tên Project",
                value = string.Empty,
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
                value = string.Empty,
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
            }
        };

        // Add member fields
        for (var i = 1; i <= memberCount; i++)
        {
            fields.Add(new
            {
                name = $"👤 Thành viên {i}",
                value = string.Empty,
                inputs = new
                {
                    id = $"member_{i}",
                    type = 3,
                    component = new
                    {
                        id = $"member_{i}_input",
                        placeholder = $"@username hoặc userId",
                        defaultValue = "",
                        type = "text",
                        textarea = false
                    }
                }
            });
        }

        var components = new List<object>();

        if (memberCount < 6)
        {
            var addMemberId = string.IsNullOrWhiteSpace(originalMessageId) 
                ? $"ADD_MEMBER|{memberCount}" 
                : $"ADD_MEMBER|{memberCount}|{originalMessageId}|{senderId}";
            
            components.Add(new
            {
                id = addMemberId,
                type = 1,
                component = new { label = "➕ Thêm thành viên", style = 2 }
            });
        }

        var createTeamId = string.IsNullOrWhiteSpace(originalMessageId) 
            ? "CREATE_TEAM" 
            : $"CREATE_TEAM|{originalMessageId}|{senderId}";
        
        var cancelTeamId = string.IsNullOrWhiteSpace(originalMessageId) 
            ? "CANCEL_TEAM" 
            : $"CANCEL_TEAM|{originalMessageId}|{senderId}";

        components.Add(new
        {
            id = createTeamId,
            type = 1,
            component = new { label = "✅ Tạo Team", style = 3 }
        });

        components.Add(new
        {
            id = cancelTeamId,
            type = 1,
            component = new { label = "❌ Hủy", style = 4 }
        });

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "📋 Tạo Project và Team",
                    description = "Vui lòng điền thông tin bên dưới:",
                    color = "#5865F2",
                    fields = fields.ToArray(),
                    footer = new { text = "Team cần tối thiểu 1 thành viên, tối đa 6 thành viên" }
                }
            },
            Components = new[] { new { components = components.ToArray() } }
        };
    }


    public static ChannelMessageContent BuildTeamFormWithError(string error, int memberCount = 3)
    {
        var form = BuildTeamForm(memberCount);

        return new ChannelMessageContent
        {
            Embed = new[]
            {
            new
            {
                title = "📋 Tạo Project và Team",
                description = $"❌ {error}\n\nVui lòng điền lại thông tin:",
                color = "#ED4245",
                fields = ((dynamic)form.Embed![0]).fields,
                footer = new { text = "Team cần tối thiểu 1 thành viên, tối đa 6 thành viên" }
            }
        },
            Components = form.Components
        };
    }

    public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId, string? originalMessageId = null, string? originalSenderId = null)
    {
        var acceptId = string.IsNullOrWhiteSpace(originalMessageId)
            ? $"ACCEPT|{requestId}|{userId}"
            : $"ACCEPT|{requestId}|{userId}|{originalMessageId}|{originalSenderId}";
        
        var rejectId = string.IsNullOrWhiteSpace(originalMessageId)
            ? $"REJECT|{requestId}|{userId}"
            : $"REJECT|{requestId}|{userId}|{originalMessageId}|{originalSenderId}";

        return new ChannelMessageContent
        {
            Embed = new[]
            {
                new
                {
                    title = "✨ Lời mời tham gia Team",
                    description = $"Bạn được mời tham gia team **{teamName}** của project **{projectName}**",
                    color = "#5865F2",
                    footer = new { text = "Vui lòng xác nhận trong vòng 30 phút" }
                }
            },
            Components = new[]
            {
                new
                {
                    components = new object[]
                    {
                        new
                        {
                            id = acceptId,
                            type = 1,
                            component = new { label = "✅ Chấp nhận", style = 3 }
                        },
                        new
                        {
                            id = rejectId,
                            type = 1,
                            component = new { label = "❌ Từ chối", style = 4 }
                        }
                    }
                }
            }
        };
    }

    public static (bool IsValid, string Message, List<string> Members) ValidateForm(
        string projectName,
        string teamName,
        Dictionary<string, string> formValues)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return (false, "❌ Tên project không được để trống", new List<string>());

        if (projectName.Length > 50)
            return (false, "❌ Tên project tối đa 50 ký tự", new List<string>());

        if (string.IsNullOrWhiteSpace(teamName))
            return (false, "❌ Tên team không được để trống", new List<string>());

        if (teamName.Length > 20)
            return (false, "❌ Tên team tối đa 20 ký tự", new List<string>());

        var members = new List<string>();
        for (var i = 1; i <= 6; i++)
        {
            if (formValues.TryGetValue($"member_{i}", out var value) && !string.IsNullOrWhiteSpace(value))
            {
                members.Add(value.Trim());
            }
        }

        if (members.Count < 2)
            return (false, $"❌ Team phải có ít nhất 2 thành viên (hiện tại: {members.Count})", new List<string>());

        if (members.Count > 6)
            return (false, $"❌ Team tối đa 6 thành viên (hiện tại: {members.Count})", new List<string>());

        return (true, "✅ Hợp lệ", members);
    }
}
