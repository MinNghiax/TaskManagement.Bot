using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Helpers;

public static class UserHelper
{
    public static async Task<string> ResolveDisplayNameAsync(
        IMezonUserService userService,
        string userId,
        string? clanId = null,
        ChannelMessage? message = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Unknown User";
        }

        if (message != null && userId == message.SenderId)
        {
            return message.DisplayName 
                ?? message.ClanNick 
                ?? message.Username 
                ?? await userService.GetDisplayNameAsync(userId, clanId);
        }

        return await userService.GetDisplayNameAsync(userId, clanId);
    }

    public static string? ExtractUserIdFromMention(string mentionToken, ChannelMessage message)
    {
        var mention = message.Mentions?.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.UserId));
        if (mention != null)
        {
            return mention.UserId;
        }

        if (mentionToken.StartsWith("<@") && mentionToken.EndsWith(">"))
        {
            return mentionToken.Substring(2, mentionToken.Length - 3);
        }

        if (mentionToken.StartsWith("@"))
        {
            return mentionToken.Substring(1);
        }

        return mentionToken;
    }
}
