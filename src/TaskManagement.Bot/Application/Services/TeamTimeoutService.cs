using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManagement.Bot.Infrastructure.Data;

namespace TaskManagement.Bot.Application.Services;

public class TeamTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TeamTimeoutService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            var teams = await context.Teams
                .Where(t => !t.IsDeleted)
                .ToListAsync(stoppingToken);

            foreach (var team in teams)
            {
                var isExpired = team.CreatedAt < DateTime.UtcNow.AddMinutes(-30);
                if (!isExpired)
                {
                    continue;
                }

                var hasPending = await context.TeamMembers
                    .AnyAsync(x => x.TeamId == team.Id && x.Status == "Pending", stoppingToken);

                if (!hasPending)
                {
                    continue;
                }

                var project = await context.Projects.FindAsync([team.ProjectId], stoppingToken);
                if (project != null)
                {
                    project.IsDeleted = true;
                }

                team.IsDeleted = true;
            }

            await context.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
