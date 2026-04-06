using TaskManagement.Bot.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Mezon.Sdk;

// ═══════════════════════════════════════════════════════════════
// TaskManagement.Bot - Mezon Bot Entry Point
// ═══════════════════════════════════════════════════════════════

// Build configuration from appsettings.json
var basePath = AppContext.BaseDirectory;
// If running from bin directory, go up to project root
if (basePath.Contains("\\bin\\"))
{
    basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

// Setup Dependency Injection container
var services = new ServiceCollection();

// Add configuration
services.AddSingleton<IConfiguration>(configuration);

// Add logging
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// Register bot service
services.AddSingleton<IBotService, BotService>();

// Register in-memory task service (for testing without database)
services.AddSingleton<ITaskService, InMemoryTaskService>();

// Build DI provider
var serviceProvider = services.BuildServiceProvider();

// Display welcome message
Console.Clear();
Console.WriteLine(@"
╔═══════════════════════════════════════════════════╗
║    🤖 TaskManagement.Bot - Mezon Connection Test   ║
╚═══════════════════════════════════════════════════╝
");

// Get logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 [Startup] Application initializing...");

// Get bot service and start connection
try
{
    var botService = serviceProvider.GetRequiredService<IBotService>();
    
    // Get cancellation token to allow graceful shutdown
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        logger.LogInformation("\n\n⚠️  [Shutdown] Ctrl+C received, gracefully shutting down...");
        cts.Cancel();
    };

    // Start the bot
    await botService.StartAsync(cts.Token);

    // Keep the application running and listening for messages
    logger.LogInformation("\n✅ [Ready] Bot is running. Press Ctrl+C to stop.\n");
    
    try
    {
        // Keep application alive until cancellation
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when Ctrl+C is pressed
    }

    // Graceful shutdown
    await botService.StopAsync();
    logger.LogInformation("✓ [Shutdown] Application stopped successfully");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "❌ [Fatal Error] Application crashed");
    Environment.Exit(1);
}
finally
{
    await serviceProvider.DisposeAsync();
}
