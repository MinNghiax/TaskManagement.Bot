using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class Team : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string CreatedBy { get; set; } = null!; // PM

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
