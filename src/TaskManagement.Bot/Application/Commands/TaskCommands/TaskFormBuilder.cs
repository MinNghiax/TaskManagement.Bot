using Azure.Core;
using Mezon.Sdk.Domain;
using System.Text.RegularExpressions;

namespace TaskManagement.Bot.Application.Commands.TaskCommands
{
    public static class TaskFormBuilder
    {
        public static ChannelMessageContent BuildTeamForm(string clanId)
        {
            var interactive = new
            {
                title = "Create Project Team",
                description = "Nhập thông tin Project & Team",
                color = "#5865F2",
                fields = new object[]
                {
                    //  Project Name
                    new
                    {
                        name = "Project Name",
                        value = "",
                        inputs = new
                        {
                            id = "project_name",
                            type = 3,
                            component = new
                            {
                                placeholder = "Nhập tên project",
                                type = "text"
                            }
                        }
                    },

                    //  Team Name
                    new
                    {
                        name = "Team Name",
                        value = "",
                        inputs = new
                        {
                            id = "team_name",
                            type = 3,
                            component = new
                            {
                                placeholder = "Nhập tên team",
                                type = "text"
                            }
                        }
                    },

                    //  Members
                    new
                    {
                        name = "Members (3-6 người)",
                        value = "",
                        inputs = new
                        {
                            id = "members",
                            type = 3,
                            component = new
                            {
                                placeholder = "@a @b @c ... (3-6 người)",
                                type = "text"
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
                        components = new object[]
                        {
                            new
                            {
                                id = $"CREATE_TEAM|{clanId}",
                                type = 1,
                                component = new
                                {
                                    label = "Tạo Team",
                                    style = 3
                                }
                            },
                            new
                            {
                                id = $"CANCEL_TEAM|{clanId}",
                                type = 1,
                                component = new
                                {
                                    label = "Huỷ",
                                    style = 4
                                }
                            }
                        }
                    }
                }
            };
        }

        //  VALIDATION METHOD
        public static (bool isValid, string message) ValidateForm(
            string projectName,
            string teamName,
            string role,
            string members)
        {
            // Project name
            if (string.IsNullOrWhiteSpace(projectName))
                return (false, "❌ Project name không được để trống");

            if (projectName.Length > 50)
                return (false, "❌ Project name tối đa 50 ký tự");

            // Team name
            if (string.IsNullOrWhiteSpace(teamName))
                return (false, "❌ Team name không được để trống");

            // Role
            if (string.IsNullOrWhiteSpace(role))
                role = "PM"; // default

            // Members
            if (string.IsNullOrWhiteSpace(members))
                return (false, "❌ Members không được để trống");

            // Tách member theo khoảng trắng
            var memberList = members
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToList();

            //  MIN 3 - MAX 8
            if (memberList.Count < 1)
                return (false, "❌ Team phải có ít nhất 3 thành viên");

            if (memberList.Count > 6)
                return (false, "❌ Team tối đa 6 thành viên");

            // Validate format @username
            var regex = new Regex(@"^@\w+$");
            foreach (var m in memberList)
            {
                if (!regex.IsMatch(m))
                    return (false, $"❌ Member không hợp lệ: {m}");
            }

            return (true, "✅ Valid");

        }

        public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId)
        {
            return new ChannelMessageContent
            {
                Text = $"📩 Bạn được mời tham gia vào team `{teamName}` với project '{projectName}'",
                Components = new[]
                {
                    new
                    {
                        type = 1,
                        components = new object[]
                        {
                            new
                            {
                                id = $"ACCEPT|{requestId}|{userId}",
                                type = 1,
                                component = new { label = "Accept", style = 3 }
                            },
                            new
                            {
                                id = $"REJECT|{requestId}|{userId}",
                                type = 1,
                                component = new { label = "Reject", style = 4 }
                            }
                        }
                    }
                }
            };
        }
    }
}