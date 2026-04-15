using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Configurations;

namespace TaskManagement.Bot.Infrastructure.Data
{
    public class TaskManagementDbContext : DbContext
    {
        public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }

        public DbSet<TaskChannel> TaskChannels { get; set; }
        public DbSet<TaskClan> TaskClans { get; set; }

        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<PendingTeamRequest> PendingTeamRequests { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        public DbSet<ReminderRule> ReminderRules { get; set; }

        public DbSet<Complain> Complains { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Team)
                .WithMany(t => t.Tasks)
                .HasForeignKey(t => t.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
