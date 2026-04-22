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

        entity.Property(e => e.TriggerType)
            .HasConversion<int>()
            .IsRequired();

        entity.Property(e => e.IntervalUnit)
            .HasConversion<int>();

        entity.Property(e => e.Value)
            .IsRequired();

        entity.Property(e => e.TaskStatus)
            .HasConversion<int>();

        entity.Property(e => e.IsRepeat)
            .HasDefaultValue(false);

        entity.HasIndex(e => e.TriggerType);
        entity.HasIndex(e => e.TaskStatus);
        entity.HasIndex(e => e.IsRepeat);

        entity.HasQueryFilter(e => !e.IsDeleted);
    }
}
