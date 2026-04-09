using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Bot.Infrastructure.Data;

public class TaskManagementDbContextFactory
    : IDesignTimeDbContextFactory<TaskManagementDbContext>
{
    public TaskManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskManagementDbContext>();
        var configuration = TaskManagementDbContextConfiguration.BuildConfiguration();

        TaskManagementDbContextConfiguration.Configure(optionsBuilder, configuration);

        return new TaskManagementDbContext(optionsBuilder.Options);
    }
}
