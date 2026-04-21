using TaskManagement.Bot.Domain;
namespace TaskManagement.Bot.Application.Services;
public interface IMezonUserService
{
    Task<MezonUser?> GetUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);

    Task<MezonUser?> RefreshUserAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);

    Task<string> GetDisplayNameAsync(string userId, string? clanId = null, CancellationToken cancellationToken = default);
    Task<List<MezonUser>> GetClanMembersAsync(string clanId, CancellationToken cancellationToken = default);
    Task<int> PreloadAllClanMembersAsync(CancellationToken cancellationToken = default);
    void CacheUserFromMessage(string userId, string? clanNick, string? displayName, string? username, string? avatarUrl, string? clanId);
    int GetCacheSize();
}