using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Data
{
    public class TaskManagementDbContext : DbContext
    {
        public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskClan> TaskClans { get; set; }
        public DbSet<TaskThread> TaskThreads { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<ReminderRule> ReminderRules { get; set; }
        public DbSet<Complain> Complains { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
        }
    }
}