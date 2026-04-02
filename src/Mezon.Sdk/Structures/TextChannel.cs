namespace Mezon.Sdk.Structures;

using Mezon.Sdk.Constants;
using Mezon.Sdk.Proto;

public class TextChannel
{
    public string Id { get; }
    public string Name { get; }
    public int ChannelType { get; }
    public bool IsPrivate { get; }
    public string CategoryId { get; }
    public string MeetingCode { get; }
    public Clan Clan { get; }

    public TextChannel(ChannelDescription data, Clan clan)
    {
        Id = data.ChannelId.ToString();
        Name = data.ChannelLabel;
        ChannelType = data.Type;
        IsPrivate = data.ChannelPrivate != 0;
        CategoryId = data.CategoryId.ToString();
        MeetingCode = data.MeetingCode;
        Clan = clan;
    }

    public int Mode => ChannelTypeToMode(ChannelType);

    private static int ChannelTypeToMode(int type) => type switch
    {
        (int)Constants.ChannelType.Channel   => (int)ChannelStreamMode.Channel,
        (int)Constants.ChannelType.Group     => (int)ChannelStreamMode.Group,
        (int)Constants.ChannelType.DM        => (int)ChannelStreamMode.Dm,
        (int)Constants.ChannelType.Thread    => (int)ChannelStreamMode.Thread,
        _ => (int)ChannelStreamMode.Channel,
    };

    public Task SendAsync(string contentJson, int code = 0, string? topicId = null, CancellationToken ct = default)
        => Clan.MessageQueue.EnqueueAsync((Func<Task>)(() => Clan.SocketManager.SendChatMessageAsync(
            Clan.Id, Id, Mode, !IsPrivate, contentJson, code: code, topicId: topicId, ct: ct)!));

    public Task SendTextAsync(string text, int code = 0, string? topicId = null, CancellationToken ct = default)
        => SendAsync($"{{\"t\":\"{EscapeJson(text)}\"}}", code, topicId, ct);

    public Task SendEphemeralAsync(string receiverId, string contentJson, CancellationToken ct = default)
        => Clan.MessageQueue.EnqueueAsync((Func<Task>)(() => Clan.SocketManager.SendEphemeralMessageAsync(
            receiverId, Clan.Id, Id, Mode, !IsPrivate, contentJson, ct)));

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
}
