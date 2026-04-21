using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string CreatedBy { get; set; } = null!; 

        public ICollection<Team> Teams { get; set; } = new List<Team>();
    }
}
