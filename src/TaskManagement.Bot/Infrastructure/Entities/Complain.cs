using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class Complain : BaseEntity
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; }

        public int UserId { get; set; }

        public string Reason { get; set; }

        public ComplainType Type { get; set; }

        public ComplainStatus Status { get; set; }

        public DateTime? NewDeadline { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
