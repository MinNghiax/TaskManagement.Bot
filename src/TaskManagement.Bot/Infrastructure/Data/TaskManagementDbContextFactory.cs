using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TaskManagement.Bot.Infrastructure.Data
{
    public class TaskManagementDbContextFactory
        : IDesignTimeDbContextFactory<TaskManagementDbContext>
    {
        public TaskManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TaskManagementDbContext>();

            optionsBuilder.UseSqlServer(GetConnectionString());

            return new TaskManagementDbContext(optionsBuilder.Options);
        }

        public static string GetConnectionString()
        {
            var basePath = AppContext.BaseDirectory;

            if (basePath.Contains("\\bin\\"))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }
    }
}