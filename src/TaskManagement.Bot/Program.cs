using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Infrastructure.Data;

var basePath = AppContext.BaseDirectory;

if (basePath.Contains("\\bin\\"))
{
    basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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

Console.WriteLine("✔ Ready for migration. Run EF commands now.");