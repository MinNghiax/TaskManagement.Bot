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

        public string Role { get; set; } = "Member"; 
        public string Status { get; set; } = "Pending";

        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
