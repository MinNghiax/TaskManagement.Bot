using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class PendingTeamRequest
    {
        public string ProjectName { get; set; } = "";
        public string TeamName { get; set; } = "";
        public string PMUserId { get; set; } = "";
        public string MessageId { get; set; } = "";
        public string SenderId { get; set; } = "";
        public List<string> MemberUserIds { get; set; } = [];
        public List<string> AcceptedUserIds { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}
