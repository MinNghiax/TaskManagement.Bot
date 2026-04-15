using System.Text.RegularExpressions;
using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public static class TeamFormBuilder
{
    public static ChannelMessageContent BuildTeamForm(string clanId)
    {
        var interactive = new
        {
            title = "Create Project Team",
            description = "Nhap thong tin Project va Team. Uu tien dung mention <@userId> hoac user id cho Members.",
            color = "#5865F2",
            fields = new object[]
            {
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
                            placeholder = "Nhap ten project",
                            type = "text"
                        }
                    }
                },
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
                            placeholder = "Nhap ten team",
                            type = "text"
                        }
                    }
                },
                new
                {
                    name = "Members (1-6 nguoi)",
                    value = "",
                    inputs = new
                    {
                        id = "members",
                        type = 3,
                        component = new
                        {
                            placeholder = "<@123456789> 123456789 @username",
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
                    type = 1,
                    components = new object[]
                    {
                        new
                        {
                            id = $"CREATE_TEAM|{clanId}",
                            type = 1,
                            component = new
                            {
                                label = "Tao Team",
                                style = 3
                            }
                        },
                        new
                        {
                            id = $"CANCEL_TEAM|{clanId}",
                            type = 1,
                            component = new
                            {
                                label = "Huy",
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
        string members)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return (false, "Project name khong duoc de trong");
        }

        if (projectName.Length > 50)
        {
            return (false, "Project name toi da 50 ky tu");
        }

        if (string.IsNullOrWhiteSpace(teamName))
        {
            return (false, "Team name khong duoc de trong");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            role = "PM";
        }

        if (string.IsNullOrWhiteSpace(members))
        {
            return (false, "Members khong duoc de trong");
        }

        var memberList = members
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (memberList.Count < 1)
        {
            return (false, "Team phai co it nhat 1 thanh vien");
        }

        if (memberList.Count > 6)
        {
            return (false, "Team toi da 6 thanh vien");
        }

        foreach (var member in memberList)
        {
            if (!IsValidMemberToken(member))
            {
                return (false, $"Member khong hop le: {member}");
            }
        }

        return (true, "Valid");
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

    public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId)
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
