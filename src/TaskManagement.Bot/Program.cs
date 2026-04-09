using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Data;

var configuration = TaskManagementDbContextConfiguration.BuildConfiguration();
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

services.AddDbContext<TaskManagementDbContext>(options =>
    TaskManagementDbContextConfiguration.Configure(options, configuration));
services.AddScoped<ITaskService, TaskService>();
services.AddScoped<IBotService, BotService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    logger.LogInformation("Connecting to database...");

    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
    IBotService? botService = null;

    await dbContext.Database.MigrateAsync(cancellationSource.Token);
    logger.LogInformation("Database connected and migrated successfully.");

    try
    {
        botService = scope.ServiceProvider.GetRequiredService<IBotService>();
        await botService.StartAsync(cancellationSource.Token);

        logger.LogInformation("Bot is running. Press Ctrl+C to stop.");

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        if (botService != null)
        {
            await botService.StopAsync(CancellationToken.None);
        }
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Database or bot startup failed");
    throw;
}

Console.WriteLine("Ready.");
