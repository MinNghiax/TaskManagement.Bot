using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Infrastructure.Data;

var services = new ServiceCollection();

services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// ✅ Dùng chung helper
services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(TaskManagementDbContextFactory.GetConnectionString()));

var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("🚀 Connecting to database...");

    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

    dbContext.Database.Migrate();

    logger.LogInformation("✅ Database connected & migrated successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Database connection failed");
}

Console.WriteLine("✔ Ready.");