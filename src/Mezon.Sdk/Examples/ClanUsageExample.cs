using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Examples;

public static class ClanUsageExample
{
    public static async Task GetClanInfoAsync(MezonClient client, string clanId)
    {
        try
        {
            var clan = client.Clans.Get(clanId);

            if (clan == null)
            {
                clan = await client.Clans.FetchAsync(clanId);
                
                if (clan == null)
                {
                    Console.WriteLine($"Clan {clanId} not found or not accessible");
                    return;
                }
            }

            Console.WriteLine($"Clan Name: {clan.Name}");
            Console.WriteLine($"Clan ID: {clan.Id}");

            await clan.LoadChannelsAsync();

            Console.WriteLine($"Channels: {clan.Channels.Cache.Count}");
            Console.WriteLine($"Users: {clan.Users.Cache.Count}");

            foreach (var (channelId, channel) in clan.Channels.Cache)
            {
                Console.WriteLine($"  - Channel: {channel.ChannelLabel} (ID: {channelId})");
                Console.WriteLine($"    Type: {channel.Type}, Private: {channel.ChannelPrivate}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get info for clan {clanId}: {ex.Message}");
        }
    }

    public static void ListAllClans(MezonClient client)
    {
        Console.WriteLine("Available Clans:");
        
        if (client.Clans.Cache.Count == 0)
        {
            Console.WriteLine("  No clans available");
            return;
        }

        foreach (var (clanId, clan) in client.Clans.Cache)
        {
            Console.WriteLine($"  - {clan.Name} (ID: {clanId})");
        }
    }

    public static Clan? GetDMClan(MezonClient client)
    {
        var dmClan = client.Clans.Get("0");
        
        if (dmClan != null)
        {
            Console.WriteLine("DM Clan available for direct messaging");
            Console.WriteLine($"DM Users: {dmClan.Users.Cache.Count}");
            return dmClan;
        }
        else
        {
            Console.WriteLine("DM Clan not available");
            return null;
        }
    }

    public static void ExploreClan(Clan clan)
    {
        Console.WriteLine("Clan Properties:");
        Console.WriteLine($"  ID: {clan.Id}");
        Console.WriteLine($"  Name: {clan.Name}");
        Console.WriteLine($"  Welcome Channel: {clan.WelcomeChannelId}");

        Console.WriteLine($"  Channels Cache Size: {clan.Channels.Cache.Count}");
        Console.WriteLine($"  Users Cache Size: {clan.Users.Cache.Count}");

        var clientId = clan.GetClientId();
        Console.WriteLine($"  Client ID: {clientId}");
    }

    public static async Task OnClientReadyAsync(MezonClient client)
    {
        Console.WriteLine("Client ready, listing clans:");
        ListAllClans(client);

        var firstClan = client.Clans.Cache.Values.FirstOrDefault(c => c.Id != "0");
        if (firstClan != null)
        {
            await GetClanInfoAsync(client, firstClan.Id);
        }

        GetDMClan(client);
    }
}
