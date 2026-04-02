namespace Mezon.Sdk.Client;

public class MezonClientConfig
{
    public string BotId { get; set; } = "";
    public string Token { get; set; } = "";
    public string Host { get; set; } = "gw.mezon.ai";
    public string Port { get; set; } = "443";
    public bool UseSsl { get; set; } = true;
    public int TimeoutMs { get; set; } = 7000;
}
