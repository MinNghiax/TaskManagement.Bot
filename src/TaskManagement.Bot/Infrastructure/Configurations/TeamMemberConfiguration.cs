using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Configurations
{
    public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> builder)
        {
            builder.ToTable("TeamMembers");

            builder.HasKey(tm => tm.Id);

            builder.Property(tm => tm.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(tm => tm.Role)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(tm => new { tm.Username, tm.TeamId })
                .IsUnique();
        }
    }
}
