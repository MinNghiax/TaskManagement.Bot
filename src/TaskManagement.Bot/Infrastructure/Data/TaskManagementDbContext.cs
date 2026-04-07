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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AssignedTo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<int>(); // Enum → int
            entity.Property(e => e.Priority).HasConversion<int>(); // Enum → int

            // Quan hệ 1-n với Reminder
            entity.HasMany(e => e.Reminders)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ 1-n với Complain
            entity.HasMany(e => e.Complains)
    .WithOne(e => e.TaskItem)
    .HasForeignKey(e => e.TaskItemId)
    .OnDelete(DeleteBehavior.Cascade);

            // Index cho tìm kiếm nhanh
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.MezonUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(1000);
            
            // Index
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.MezonUserId);
            entity.HasIndex(e => e.ReminderTime);
        });

        modelBuilder.Entity<Complain>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Reason)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(e => e.Type)
                  .IsRequired();

            entity.Property(e => e.Status)
                  .HasConversion<int>();

            // FK
            entity.HasOne(e => e.TaskItem)
                  .WithMany(t => t.Complains)
                  .HasForeignKey(e => e.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index
            entity.HasIndex(e => e.TaskItemId);
            entity.HasIndex(e => e.Status);
        });
    }
}