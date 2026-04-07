using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Data;

public class TaskManagementDbContext : DbContext
{
    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
        : base(options) { }

    // Định nghĩa 3 DbSet chính
    public DbSet<Task> Tasks { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Complain> Complains { get; set; }
    public DbSet<ReminderRule> ReminderRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
    }
}