using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Configurations
{
    public class ComplainConfiguration : IEntityTypeConfiguration<Complain>
    {
        public void Configure(EntityTypeBuilder<Complain> builder)
        {
            builder.ToTable("Complains");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.TaskItem)
                   .WithMany(t => t.Complains)
                   .HasForeignKey(x => x.TaskItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.UserId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Reason)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(x => x.Type)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<int>();

            builder.Property(x => x.NewDueDate)
                   .IsRequired(false);

            builder.Property(x => x.OldDueDate)
                   .IsRequired(false);

            builder.Property(x => x.ApprovedBy)
                   .IsRequired(false);

            builder.Property(x => x.ApprovedAt)
                   .IsRequired(false);

            builder.Property(x => x.RejectReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.HasIndex(x => x.TaskItemId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);
        }
    }
}