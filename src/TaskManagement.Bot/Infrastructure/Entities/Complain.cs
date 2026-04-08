using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Infrastructure.Entities
{
    public class Complain : BaseEntity
    {
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }

        public required string UserId { get; set; }

        public required string Reason { get; set; }

        public EComplainType Type { get; set; }

        public EComplainStatus Status { get; set; } = EComplainStatus.Pending;

        public DateTime? NewDueDate { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string? RejectReason { get; set; }
    }
}