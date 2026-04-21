using System.Text.Json;
using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands;

public interface IComponentHandler
{
    bool CanHandle(string customId);

    Task<ComponentResponse> HandleAsync(
        ComponentContext context,
        CancellationToken cancellationToken);
}

public sealed record ComponentContext
{
    public required JsonElement Payload { get; init; }
    public required string CustomId { get; init; }
    public string? ClanId { get; init; }
    public string? ChannelId { get; init; }
    public string? CurrentUserId { get; init; }
    public string? MessageId { get; init; }
    public int Mode { get; init; } = 2;
    public bool IsPublic { get; init; } = true;
}

public sealed class ComponentResponse
{
    public List<ComponentMessage> Messages { get; } = [];
    public List<ComponentDeleteMessage> DeleteMessages { get; } = [];
    public List<ComponentUpdateMessage> UpdateMessages { get; } = [];  // ⭐ NEW

    public static ComponentResponse FromText(string clanId, string channelId, string text, int mode, bool isPublic, string replyToMessageId, ChannelMessage? originalMessage = null)
    {
        var response = new ComponentResponse();
        response.Messages.Add(new ComponentMessage
        {
            ClanId = clanId,
            ChannelId = channelId,
            Text = text,
            Mode = mode,
            IsPublic = isPublic,
            ReplyToMessageId = replyToMessageId,
            OriginalMessage = originalMessage
        });
        return response;
    }

    public static ComponentResponse FromContent(string clanId, string channelId, ChannelMessageContent content, int mode, bool isPublic, string replyToMessageId, ChannelMessage? originalMessage = null)
    {
        var response = new ComponentResponse();
        response.Messages.Add(new ComponentMessage
        {
            ClanId = clanId,
            ChannelId = channelId,
            Content = content,
            Mode = mode,
            IsPublic = isPublic,
            ReplyToMessageId = replyToMessageId,
            OriginalMessage = originalMessage
        });
        return response;
    }

    public ComponentResponse DeleteMessage(string clanId, string channelId, string messageId, int mode, bool isPublic, string replyToMessageId, ChannelMessage? originalMessage = null)
    {
        DeleteMessages.Add(new ComponentDeleteMessage
        {
            ClanId = clanId,
            ChannelId = channelId,
            MessageId = messageId,
            Mode = mode,
            IsPublic = isPublic,
            ReplyToMessageId = replyToMessageId,
            OriginalMessage = originalMessage
        });

        return this;
    }
    
    public ComponentResponse UpdateMessage(
        string clanId, 
        string channelId, 
        string messageId, 
        ChannelMessageContent content, 
        int mode, 
        bool isPublic,
        Action? onSuccess = null)
    {
        UpdateMessages.Add(new ComponentUpdateMessage
        {
            ClanId = clanId,
            ChannelId = channelId,
            MessageId = messageId,
            Content = content,
            Mode = mode,
            IsPublic = isPublic,
            OnSuccess = onSuccess
        });

        return this;
    }
}

public sealed record ComponentUpdateMessage
{
    public required string ClanId { get; init; }
    public required string ChannelId { get; init; }
    public required string MessageId { get; init; }
    public required ChannelMessageContent Content { get; init; }
    public int Mode { get; init; } = 2;
    public bool IsPublic { get; init; } = true;
    public Action? OnSuccess { get; init; }  // ⭐ Callback sau khi update thành công
}

public sealed record ComponentMessage
{
    public required string ClanId { get; init; }
    public required string ChannelId { get; init; }
    public string? Text { get; init; }
    public ChannelMessageContent? Content { get; init; }
    public int Mode { get; init; } = 2;
    public bool IsPublic { get; init; } = true;
    public string? ReplyToMessageId { get; init; }      
    public ChannelMessage? OriginalMessage { get; init; }
}

public sealed record ComponentDeleteMessage
{
    public required string ClanId { get; init; }
    public required string ChannelId { get; init; }
    public required string MessageId { get; init; }
    public int Mode { get; init; } = 2;
    public bool IsPublic { get; init; } = true;
    public string? ReplyToMessageId { get; init; }      
    public ChannelMessage? OriginalMessage { get; init; }
}
