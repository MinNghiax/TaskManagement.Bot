using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.TaskCommands
{
    public static class TaskFormBuilder
    {
        public static ChannelMessageContent BuildTeamForm(string clanId)
        {
            var interactive = new
            {
                title = "Create Team",
                description = "Nhập thông tin team",
                color = "#5865F2",
                fields = new[]
                {
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
                    new
                    {
                        name = "PM",
                        value = "",
                        inputs = new
                        {
                            id = "pm", 
                            type = 3,
                            component = new
                            {
                                placeholder = "@username",
                                type = "text"
                            }
                        }
                    },
                    new
                    {
                        name = "Members",
                        value = "",
                        inputs = new
                        {
                            id = "members", 
                            type = 3,
                            component = new
                            {
                                placeholder = "@a @b @c",
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

        public static ChannelMessageContent BuildConfirmForm(int teamId, string teamName, string userId)
        {
            var interactive = new
            {
                title = "Xác nhận tham gia team",
                description = $"Bạn có muốn tham gia team `{teamName}` không?",
                color = "#00C853"
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
                                id = $"ACCEPT|{teamId}|{userId}",
                                type = 1,
                                component = new
                                {
                                    label = "✅ Đồng ý",
                                    style = 3
                                }
                            },
                            new
                            {
                                id = $"REJECT|{teamId}|{userId}",
                                type = 1,
                                component = new
                                {
                                    label = "❌ Từ chối",
                                    style = 4
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}