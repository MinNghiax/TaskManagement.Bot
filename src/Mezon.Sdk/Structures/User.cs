namespace Mezon.Sdk.Structures;

using Mezon.Sdk.Constants;
using Mezon.Sdk.Managers;
using Mezon.Sdk.Utils;

public class User
{
    public string Id { get; }
    public string Username { get; set; }
    public string ClanNick { get; set; }
    public string ClanAvatar { get; set; }
    public string DisplayName { get; set; }
    public string Avatar { get; set; }
    public string DmChannelId { get; set; }

    private readonly SocketManager _socket;
    private readonly MessageQueue _queue;
    private readonly ChannelManager _channels;
    private readonly Func<string> _tokenGetter;

    public User(string id, SocketManager socket, MessageQueue queue, ChannelManager channels,
                Func<string> tokenGetter,
                string username = "", string clanNick = "", string clanAvatar = "",
                string displayName = "", string avatar = "", string dmChannelId = "")
    {
        Id = id;
        Username = username;
        ClanNick = clanNick;
        ClanAvatar = clanAvatar;
        DisplayName = displayName;
        Avatar = avatar;
        DmChannelId = dmChannelId;
        _socket = socket;
        _queue = queue;
        _channels = channels;
        _tokenGetter = tokenGetter;
    }

    public async Task SendDmAsync(string contentJson, int code = 0, CancellationToken ct = default)
    {
        await _queue.EnqueueAsync<object?>(async () =>
        {
            if (string.IsNullOrEmpty(DmChannelId))
            {
                var ch = await _channels.CreateDmChannelAsync(_tokenGetter(), Id, ct);
                DmChannelId = ch?.ChannelId.ToString() ?? "";
            }
            if (string.IsNullOrEmpty(DmChannelId))
                throw new InvalidOperationException($"Cannot get DM channel for user {Id}");
            await _socket.SendChatMessageAsync("0", DmChannelId, (int)ChannelStreamMode.Dm, false,
                contentJson, code: code, ct: ct);
            return null;
        });
    }

    public Task SendDmTextAsync(string text, int code = 0, CancellationToken ct = default)
        => SendDmAsync($"{{\"t\":\"{EscapeJson(text)}\"}}", code, ct);

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
}
