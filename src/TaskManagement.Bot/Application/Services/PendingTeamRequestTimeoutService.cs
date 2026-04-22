using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Data;

namespace TaskManagement.Bot.Application.Services;

public class PendingTeamRequestTimeoutService : IHostedService, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingTeamRequestTimeoutService> _logger;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;
    private int _started;

    public PendingTeamRequestTimeoutService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingTeamRequestTimeoutService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return Task.CompletedTask;
        }

        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = RunAsync(_stoppingCts.Token);

        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        }
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
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
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending team request timeout processing failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
