using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services;

public class TeamTimeoutService : BackgroundService
{
    private readonly TaskManagementDbContext _context;

    public TeamTimeoutService(TaskManagementDbContext context)
    {
        _context = context;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var teams = await _context.Teams
                .Where(t => !t.IsDeleted)
                .ToListAsync(stoppingToken);

            foreach (var team in teams)
            {
                // check team đã quá 30 phút chưa
                var isExpired = team.CreatedAt < DateTime.UtcNow.AddMinutes(-30);

                if (!isExpired) continue;

                var hasPending = await _context.TeamMembers
                    .AnyAsync(x => x.TeamId == team.Id && x.Status == "Pending", stoppingToken);

                if (hasPending)
                {
                    var project = await _context.Projects.FindAsync(team.ProjectId);

                    if (project != null)
                    {
                        project.IsDeleted = true;
                        team.IsDeleted = true;
                    }
                }
            }

            await _context.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}