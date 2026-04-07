using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> entity)
    {
        entity.ToTable("Reminders");

        entity.Property(e => e.TriggerAt)
            .IsRequired();

        entity.Property(e => e.TargetUserId)
            .IsRequired();

        entity.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(e => e.StateSnapshot)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(e => e.NextTriggerAt);

        entity.HasOne(e => e.Task)
            .WithMany(t => t.Reminders)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ReminderRule)
            .WithMany()
            .HasForeignKey(e => e.ReminderRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.TaskId);
        entity.HasIndex(e => e.ReminderRuleId);
        entity.HasIndex(e => e.TargetUserId);
        entity.HasIndex(e => e.TriggerAt);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => new { e.Status, e.TriggerAt });
    }
}