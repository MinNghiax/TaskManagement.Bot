# Clan Cache System - C# SDK

C# SDK now has full clan cache support similar to TypeScript SDK!

## Features

### 1. Clan Manager (`client.Clans`)

Access and manage clans with automatic caching:

```csharp
// Get clan from cache
var clan = client.Clans.Get(clanId);

// Fetch clan from API if not in cache
var clan = await client.Clans.FetchAsync(clanId);

// Get all clans
var allClans = client.Clans.GetAll();

// List all cached clans
foreach (var (clanId, clan) in client.Clans.Cache)
{
    Console.WriteLine($"{clan.Name} (ID: {clanId})");
}
```

### 2. Channel Manager (`clan.Channels`)

Each clan has a channel manager with cache:

```csharp
var clan = client.Clans.Get(clanId);

// Load channels into cache
await clan.LoadChannelsAsync();

// Get channel from cache
var channel = clan.Channels.Get(channelId);

// Get all channels
var allChannels = clan.Channels.GetAll();

// Filter by type
var textChannels = clan.Channels.GetByType(0);

// Access cache directly
Console.WriteLine($"Channels: {clan.Channels.Cache.Count}");
foreach (var (channelId, channel) in clan.Channels.Cache)
{
    Console.WriteLine($"  - {channel.ChannelLabel} (ID: {channelId})");
}
```

### 3. User Manager (`clan.Users`)

Each clan has a user manager with cache:

```csharp
var clan = client.Clans.Get(clanId);

// Get user from cache
var user = clan.Users.Get(userId);

// Add user to cache (typically done automatically via events)
clan.Users.Set(userId, userObject);

// Get all users
var allUsers = clan.Users.GetAll();

// Access cache directly
Console.WriteLine($"Users: {clan.Users.Cache.Count}");
```

### 4. DM Clan (Special Clan ID "0")

Direct messages are handled through a special clan:

```csharp
// Get DM clan
var dmClan = client.Clans.Get("0");

if (dmClan != null)
{
    Console.WriteLine("DM Clan available for direct messaging");
    Console.WriteLine($"DM Users: {dmClan.Users.Cache.Count}");
}
```

## Automatic Features

### Auto-Discovery on Login

When you call `LoginAsync()`, the SDK automatically:
1. Fetches all clans the bot is a member of
2. Adds them to the cache
3. Joins each clan via WebSocket
4. Creates the DM clan (ID "0")

```csharp
var client = new MezonClient(options);
await client.LoginAsync(); // Clans are automatically cached!

// Now you can access clans immediately
var firstClan = client.Clans.Cache.Values.FirstOrDefault();
```

### Cache Cleanup on Logout

```csharp
await client.LogoutAsync(); // Automatically clears clan cache
```

## Complete Example

```csharp
using Mezon.Sdk;

var options = new MezonClientOptions
{
    BotId = "your-bot-id",
    Token = "your-token",
    Host = "gw.mezon.ai",
    Port = "443",
    UseSSL = true
};

var client = new MezonClient(options);
await client.LoginAsync();

// List all clans
Console.WriteLine("Available Clans:");
foreach (var (clanId, clan) in client.Clans.Cache)
{
    Console.WriteLine($"  - {clan.Name} (ID: {clanId})");
}

// Get specific clan
var clan = client.Clans.Get("your-clan-id");
if (clan != null)
{
    Console.WriteLine($"\nClan: {clan.Name}");
    
    // Load channels
    await clan.LoadChannelsAsync();
    Console.WriteLine($"Channels: {clan.Channels.Cache.Count}");
    
    // List channels
    foreach (var (channelId, channel) in clan.Channels.Cache)
    {
        Console.WriteLine($"  - {channel.ChannelLabel} (Type: {channel.Type})");
    }
    
    // Get client ID
    var clientId = clan.GetClientId();
    Console.WriteLine($"Bot ID: {clientId}");
}

// Check DM functionality
var dmClan = client.Clans.Get("0");
if (dmClan != null)
{
    Console.WriteLine("\nDM Clan available for direct messaging");
}

await client.LogoutAsync();
```

## API Comparison: TypeScript vs C#

| TypeScript | C# |
|------------|-----|
| `client.clans.cache` | `client.Clans.Cache` |
| `client.clans.get(id)` | `client.Clans.Get(id)` |
| `client.clans.fetch(id)` | `await client.Clans.FetchAsync(id)` |
| `clan.channels.cache` | `clan.Channels.Cache` |
| `clan.channels.get(id)` | `clan.Channels.Get(id)` |
| `clan.users.cache` | `clan.Users.Cache` |
| `clan.users.get(id)` | `clan.Users.Get(id)` |
| `clan.loadChannels()` | `await clan.LoadChannelsAsync()` |
| `clan.getClientId()` | `clan.GetClientId()` |

## Cache Structure

All caches use `ConcurrentDictionary<string, T>` for thread-safe operations:

- `ClanManager.Cache`: `ConcurrentDictionary<string, Clan>`
- `ChannelManager.Cache`: `ConcurrentDictionary<string, ApiChannelDescription>`
- `UserManager.Cache`: `ConcurrentDictionary<string, ApiUser>`

## Notes

- Clans are automatically cached on login
- Channels must be loaded explicitly via `LoadChannelsAsync()`
- Users are typically cached as they interact (via messages, events)
- All cache operations are thread-safe
- Cache is cleared on logout

## See Also

- `src/Mezon.Sdk/Examples/ClanUsageExample.cs` - Complete usage examples
- `src/Mezon.Sdk/Managers/ClanManager.cs` - Clan manager implementation
- `src/Mezon.Sdk/Managers/ChannelManager.cs` - Channel manager implementation
- `src/Mezon.Sdk/Managers/UserManager.cs` - User manager implementation
