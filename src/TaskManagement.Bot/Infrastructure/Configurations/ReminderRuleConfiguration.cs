using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Configurations;

public class ReminderRuleConfiguration : IEntityTypeConfiguration<ReminderRule>
{
    public void Configure(EntityTypeBuilder<ReminderRule> entity)
    {
        entity.ToTable("ReminderRules");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.UpdatedAt);

        entity.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        entity.Property(e => e.Name)
            .HasMaxLength(200);

        entity.Property(e => e.TriggerType)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(e => e.Value)
            .IsRequired();

        entity.Property(e => e.TaskStatus)
            .HasConversion<int>();

        entity.Property(e => e.RepeatIntervalUnit)
            .HasConversion<int>();

        entity.Property(e => e.RepeatIntervalValue);

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true);

        entity.HasIndex(e => e.TriggerType);
        entity.HasIndex(e => e.TaskStatus);
        entity.HasIndex(e => e.IsActive);

        entity.HasQueryFilter(e => !e.IsDeleted);
    }
}