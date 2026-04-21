using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Data.Configurations
{
    public class PendingTeamRequestConfig : IEntityTypeConfiguration<PendingTeamRequest>
    {
        public void Configure(EntityTypeBuilder<PendingTeamRequest> builder)
        {
            builder.ToTable("PendingTeamRequests");

            builder.HasKey(x => x.MessageId);

            builder.Property(x => x.ProjectName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.TeamName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PMUserId)
                .IsRequired();

            builder.Property(x => x.SenderId)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.MemberUserIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            builder.Property(x => x.AcceptedUserIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        }
    }
}