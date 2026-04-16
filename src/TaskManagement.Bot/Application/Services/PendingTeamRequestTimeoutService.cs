using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManagement.Bot.Infrastructure.Data;

namespace TaskManagement.Bot.Application.Services;

public class PendingTeamRequestTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PendingTeamRequestTimeoutService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var expiredAt = DateTime.UtcNow.AddMinutes(-30);

            var expiredRequests = await context.PendingTeamRequests
                .Where(x => x.CreatedAt < expiredAt)
                .ToListAsync(stoppingToken);

            if (expiredRequests.Count > 0)
            {
                context.PendingTeamRequests.RemoveRange(expiredRequests);
                await context.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
