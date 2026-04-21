using Mezon.Sdk.Structures;

namespace Mezon.Sdk.Examples;

/// <summary>
/// Examples of using Clan features similar to TypeScript SDK
/// </summary>
public static class ClanUsageExample
{
    /// <summary>
    /// Get clan info similar to TypeScript SDK's getClanInfo()
    /// </summary>
    public static async Task GetClanInfoAsync(MezonClient client, string clanId)
    {
        try
        {
            // Get clan from cache
            var clan = client.Clans.Get(clanId);

            if (clan == null)
            {
                // Try to fetch if not in cache
                clan = await client.Clans.FetchAsync(clanId);
                
                if (clan == null)
                {
                    Console.WriteLine($"Clan {clanId} not found or not accessible");
                    return;
                }
            }

            Console.WriteLine($"Clan Name: {clan.Name}");
            Console.WriteLine($"Clan ID: {clan.Id}");

            // Load channels if not already loaded
            await clan.LoadChannelsAsync();

            Console.WriteLine($"Channels: {clan.Channels.Cache.Count}");
            Console.WriteLine($"Users: {clan.Users.Cache.Count}");

            // List channels in the clan
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

    /// <summary>
    /// List all available clans similar to TypeScript SDK's listAllClans()
    /// </summary>
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

    /// <summary>
    /// Get the DM "clan" (special clan with ID "0") similar to TypeScript SDK's getDMClan()
    /// </summary>
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

    /// <summary>
    /// Explore clan properties similar to TypeScript SDK's exploreClan()
    /// </summary>
    public static void ExploreClan(Clan clan)
    {
        Console.WriteLine("Clan Properties:");
        Console.WriteLine($"  ID: {clan.Id}");
        Console.WriteLine($"  Name: {clan.Name}");
        Console.WriteLine($"  Welcome Channel: {clan.WelcomeChannelId}");

        // Access cached collections
        Console.WriteLine($"  Channels Cache Size: {clan.Channels.Cache.Count}");
        Console.WriteLine($"  Users Cache Size: {clan.Users.Cache.Count}");

        // Get client ID (bot's user ID)
        var clientId = clan.GetClientId();
        Console.WriteLine($"  Client ID: {clientId}");
    }

    /// <summary>
    /// Example usage after client is ready
    /// </summary>
    public static async Task OnClientReadyAsync(MezonClient client)
    {
        Console.WriteLine("Client ready, listing clans:");
        ListAllClans(client);

        // Get info for the first available clan
        var firstClan = client.Clans.Cache.Values.FirstOrDefault(c => c.Id != "0");
        if (firstClan != null)
        {
            await GetClanInfoAsync(client, firstClan.Id);
        }

        // Check DM functionality
        GetDMClan(client);
    }
}
