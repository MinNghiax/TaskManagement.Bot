using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Bot.Application.DTOs;

namespace TaskManagement.Bot.Application.Sessions
{
    public class UserSession
    {
        public string Step { get; set; } = "";
        public string? ProjectId { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public List<string> TeamMembers { get; set; } = new();
        public CreateTaskDto TempTask { get; set; } = new CreateTaskDto
        {
            Title = "",
            AssignedTo = "",
            CreatedBy = ""
        };
    }
}
