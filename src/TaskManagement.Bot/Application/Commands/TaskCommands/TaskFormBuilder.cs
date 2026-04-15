using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Commands.TeamCommands;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public static class TaskFormBuilder
{
    [Obsolete("Use TeamFormBuilder for team-related interactive forms.")]
    public static ChannelMessageContent BuildTeamForm(string clanId) => TeamFormBuilder.BuildTeamForm(clanId);

    [Obsolete("Use TeamFormBuilder for team-related interactive forms.")]
    public static (bool isValid, string message) ValidateForm(
        string projectName,
        string teamName,
        string role,
        string members) => TeamFormBuilder.ValidateForm(projectName, teamName, role, members);

    [Obsolete("Use TeamFormBuilder for team-related interactive forms.")]
    public static ChannelMessageContent BuildConfirmForm(string requestId, string teamName, string projectName, string userId) =>
        TeamFormBuilder.BuildConfirmForm(requestId, teamName, projectName, userId);
}
