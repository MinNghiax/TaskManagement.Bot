using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Data.Configurations
{
    public class PendingTeamRequestConfig : IEntityTypeConfiguration<PendingTeamRequest>
    {
        private static readonly ValueComparer<List<string>> StringListComparer = new(
            (left, right) => left == right || (left != null && right != null && left.SequenceEqual(right)),
            values => values == null ? 0 : values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
            values => values == null ? new List<string>() : values.ToList());

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
                )
                .Metadata.SetValueComparer(StringListComparer);

            builder.Property(x => x.AcceptedUserIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .Metadata.SetValueComparer(StringListComparer);
        }
    }
}
