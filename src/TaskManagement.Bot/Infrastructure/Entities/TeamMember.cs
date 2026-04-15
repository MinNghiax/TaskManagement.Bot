using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class TeamMember : BaseEntity
    {
        public string Username { get; set; } = null!;

        public string Role { get; set; }  // PM | Member
        public string Status { get; set; } = "Pending";
        // Pending | Accepted | Rejected

        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
