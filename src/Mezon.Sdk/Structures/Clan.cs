namespace Mezon.Sdk.Structures;

using Mezon.Sdk.Managers;
using Mezon.Sdk.Proto;
using Mezon.Sdk.Utils;

public class Clan
{
    public string Id { get; }
    public string Name { get; }
    public string WelcomeChannelId { get; }

    internal readonly SocketManager SocketManager;
    internal readonly MessageQueue MessageQueue;
    internal string SessionToken;

    public Clan(ClanDesc data, SocketManager socket, MessageQueue queue, string token)
    {
        Id = data.ClanId.ToString();
        Name = data.ClanName;
        WelcomeChannelId = data.WelcomeChannelId.ToString();
        SocketManager = socket;
        MessageQueue = queue;
        SessionToken = token;
    }
}
