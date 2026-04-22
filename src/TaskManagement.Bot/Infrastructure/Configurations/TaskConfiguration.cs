using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(t => t.Id);

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

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TaskManagement.Bot.Infrastructure.Enums.ETaskStatus.ToDo);

        builder.Property(t => t.ReviewStartedAt)
            .IsRequired(false);

        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .HasDefaultValue(EPriorityLevel.Medium)
            .HasSentinel((EPriorityLevel)(-1));

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.HasMany(t => t.Reminders)
            .WithOne(r => r.Task)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Complains)
            .WithOne(c => c.TaskItem)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Clans)
            .WithOne(c => c.TaskItem)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Channels)
            .WithOne(t => t.TaskItem)
            .HasForeignKey(t => t.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.ReviewStartedAt);
        builder.HasIndex(t => t.AssignedTo);
        builder.HasIndex(t => t.CreatedAt);
    }
}
