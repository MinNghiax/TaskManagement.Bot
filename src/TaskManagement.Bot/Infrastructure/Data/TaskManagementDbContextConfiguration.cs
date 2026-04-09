using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TaskManagement.Bot.Infrastructure.Data;

public static class TaskManagementDbContextConfiguration
{
    private const string DefaultConnectionName = "DefaultConnection";

    public static IConfiguration BuildConfiguration()
    {
        var basePath = ResolveBasePath(AppContext.BaseDirectory);

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
    }

    public static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration)
    {
        optionsBuilder.UseSqlServer(GetRequiredConnectionString(configuration));
    }

    public static string GetRequiredConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionName}' not found.");
    }

    private static string ResolveBasePath(string basePath)
    {
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        if (basePath.Contains(binSegment, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
        }

        return basePath;
    }
}
