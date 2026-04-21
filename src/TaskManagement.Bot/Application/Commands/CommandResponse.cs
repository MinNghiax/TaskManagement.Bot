using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands;

public class CommandResponse
{
    public string? Text { get; set; }
    public IInteractiveMessageProps? Embed { get; set; }
    public ChannelMessageContent? Content { get; set; }

    public CommandResponse(string text)
    {
        Text = text;
        Embed = null;
        Content = null;
    }

    public CommandResponse(IInteractiveMessageProps embed)
    {
        Text = null;
        Embed = embed;
        Content = null;
    }

    public CommandResponse(string text, IInteractiveMessageProps embed)
    {
        Text = text;
        Embed = embed;
        Content = null;
    }

    public CommandResponse(ChannelMessageContent content)
    {
        Text = null;
        Embed = null;
        Content = content;
    }

    public CommandResponse()
    {
        Text = null;
        Embed = null;
        Content = null;
    }
}
