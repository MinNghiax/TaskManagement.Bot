namespace Mezon.Sdk.Structures;

using Mezon.Sdk.Constants;

public class Message
{
    public string Id { get; }
    public string? SenderId { get; }
    public string? Content { get; }
    public string? TopicId { get; }
    public long CreateTimeSeconds { get; }
    public TextChannel Channel { get; }

    public Message(string id, string? senderId, string? content, TextChannel channel,
                   string? topicId = null, long createTimeSeconds = 0)
    {
        Id = id;
        SenderId = senderId;
        Content = content;
        TopicId = topicId;
        CreateTimeSeconds = createTimeSeconds;
        Channel = channel;
    }

    public Task ReplyAsync(string contentJson, int code = 0, string? topicId = null, CancellationToken ct = default)
        => Channel.Clan.MessageQueue.EnqueueAsync((Func<Task>)(() => Channel.Clan.SocketManager.SendChatMessageAsync(
            Channel.Clan.Id, Channel.Id!, Channel.Mode, !Channel.IsPrivate,
            contentJson, code: code, topicId: topicId ?? TopicId, ct: ct)!));

    public Task ReplyTextAsync(string text, int code = 0, string? topicId = null, CancellationToken ct = default)
        => ReplyAsync($"{{\"t\":\"{EscapeJson(text)}\"}}", code, topicId, ct);

    public Task UpdateAsync(string contentJson, CancellationToken ct = default)
        => Channel.Clan.MessageQueue.EnqueueAsync((Func<Task>)(() => Channel.Clan.SocketManager.UpdateChatMessageAsync(
            Channel.Clan.Id, Channel.Id!, Channel.Mode, !Channel.IsPrivate,
            Id, contentJson, ct)));

    public Task DeleteAsync(CancellationToken ct = default)
        => Channel.Clan.MessageQueue.EnqueueAsync((Func<Task>)(() => Channel.Clan.SocketManager.RemoveChatMessageAsync(
            Channel.Clan.Id, Channel.Id!, Channel.Mode, !Channel.IsPrivate,
            Id, TopicId, ct)));

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
}
