using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;

namespace Mezon.Sdk.Structures;

/// <summary>
/// High-level TextChannel structure for sending messages to a channel.
/// </summary>
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

    /// <summary>Send a message to this channel.</summary>
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

    /// <summary>Send a simple text message to this channel.</summary>
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

    /// <summary>Send an ephemeral (auto-dismissing) message to a specific user.</summary>
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

    /// <summary>Delete an ephemeral message.</summary>
    public Task<ChannelMessageAck> DeleteEphemeralAsync(
        string receiverId,
        string messageId,
        string? topicId = null,
        CancellationToken cancellationToken = default)
    {
        // Ephemeral deletion uses the DeleteEphemeralMsg type message
        return _client.Socket.WriteChatMessageAsync(
            ClanId, Id,
            TypeMessage.DeleteEphemeralMsg,
            isPublic: !IsPrivate,
            content: new { message_id = messageId, receiver_id = receiverId },
            cancellationToken: cancellationToken);
    }

}
