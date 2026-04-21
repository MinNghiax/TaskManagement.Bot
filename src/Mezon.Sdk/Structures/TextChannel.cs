using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;

namespace Mezon.Sdk.Structures;

public sealed class TextChannel
{
    public string Id { get; }
    public string ClanId { get; }
    public string? Name { get; set; }
    public bool IsPrivate { get; set; }
    public int? ChannelType { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    private readonly MezonClient _client;

    public TextChannel(string clanId, string channelId, MezonClient client)
    {
        ClanId = clanId;
        Id = channelId;
        _client = client;
    }

    public Task<ChannelMessageAck> SendAsync(
        ChannelMessageContent content,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        bool mentionEveryone = false,
        bool anonymousMessage = false,
        string? topicId = null,
        int? code = null,
        CancellationToken cancellationToken = default)
    {
        return _client.SendMessageAsync(
            ClanId, Id,
            ChannelStreamMode.Channel,
            isPublic: !IsPrivate,
            content, mentions, attachments,
            anonymousMessage: anonymousMessage,
            mentionEveryone: mentionEveryone,
            topicId: topicId,
            code: code,
            cancellationToken: cancellationToken);
    }

    public Task<ChannelMessageAck> SendTextAsync(
        string text,
        bool mentionEveryone = false,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            new ChannelMessageContent { Text = text },
            mentionEveryone: mentionEveryone,
            cancellationToken: cancellationToken);
    }

    public async Task<ChannelMessageAck> SendEphemeralAsync(
        string receiverId,
        object content,
        string? referenceMessageId = null,
        ApiMessageMention[]? mentions = null,
        ApiMessageAttachment[]? attachments = null,
        bool mentionEveryone = false,
        bool anonymousMessage = false,
        string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.Socket.WriteEphemeralMessageAsync(
            receiverId, ClanId, Id,
            ChannelStreamMode.Channel,
            isPublic: !IsPrivate,
            content, mentions, attachments,
            mentionEveryone: mentionEveryone,
            anonymousMessage: anonymousMessage,
            topicId: topicId,
            messageId: referenceMessageId,
            cancellationToken: cancellationToken);
    }

    public Task<ChannelMessageAck> DeleteEphemeralAsync(
        string receiverId,
        string messageId,
        string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        return _client.Socket.WriteChatMessageAsync(
            ClanId, Id,
            TypeMessage.DeleteEphemeralMsg,
            isPublic: !IsPrivate,
            content: new { message_id = messageId, receiver_id = receiverId },
            cancellationToken: cancellationToken);
    }

}
