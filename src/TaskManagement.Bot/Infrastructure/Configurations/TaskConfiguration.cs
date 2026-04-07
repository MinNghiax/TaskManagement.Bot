using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.AssignedTo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.DueDate)
            .IsRequired(false);

        // Enum → int
        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TaskManagement.Bot.Infrastructure.Enums.TaskStatus.ToDo);

        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .HasDefaultValue(PriorityLevel.Medium);

        // BaseEntity
        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        // Relationships

        // Task - Reminder (1-n)
        builder.HasMany(t => t.Reminders)
            .WithOne(r => r.Task)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Task - Complain (1-n)
        builder.HasMany(t => t.Complains)
            .WithOne(c => c.Task)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Task - Clan (1-n)
        builder.HasMany(t => t.Clans)
            .WithOne(c => c.TaskItem)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Task - Thread (1-n)
        builder.HasMany(t => t.Threads)
            .WithOne(t => t.TaskItem)
            .HasForeignKey(t => t.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedTo);
        builder.HasIndex(t => t.CreatedAt);
    }
}