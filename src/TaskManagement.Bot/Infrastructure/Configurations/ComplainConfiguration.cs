using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Configurations
{
    public class ComplainConfiguration : IEntityTypeConfiguration<Complain>
    {
        public void Configure(EntityTypeBuilder<Complain> builder)
        {
            //Primary Key
            builder.HasKey(x => x.Id);

            //Relationship: TaskItem (1) - (n) Complain
            builder.HasOne(x => x.TaskItem)
                   .WithMany(t => t.Complains)
                   .HasForeignKey(x => x.TaskItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            //Required fields
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

            //Optional fields
            builder.Property(x => x.NewDueDate)
                   .IsRequired(false);

            builder.Property(x => x.ApprovedBy)
                   .IsRequired(false);

            builder.Property(x => x.ApprovedAt)
                   .IsRequired(false);

            builder.Property(x => x.RejectReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            //Index (tối ưu query)
            builder.HasIndex(x => x.TaskItemId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);
        }
    }
}