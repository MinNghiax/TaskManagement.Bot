using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

public class ComprehensiveTaskReportDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string AssignedTo { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;

    public ETaskStatus Status { get; set; }
    public EPriorityLevel Priority { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int DaysOverdue { get; set; }                      
    public int DaysUntilDue { get; set; }                     
    public int TotalDaysAllocated { get; set; }               
    public double ProgressPercentage { get; set; }            
    public string TimeStatusIcon { get; set; } = string.Empty; 

    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }

    public List<string> ClanIds { get; set; } = new();
    public List<string> ChannelIds { get; set; } = new();
    public List<string> ThreadIds { get; set; } = new();

    public int TotalReminders { get; set; }
    public int PendingReminders { get; set; }
    public int SentReminders { get; set; }
    public DateTime? NextReminderAt { get; set; }
    public List<ReminderSummaryDto> Reminders { get; set; } = new();

    public int TotalComplaints { get; set; }
    public int PendingComplaints { get; set; }
    public int ApprovedComplaints { get; set; }
    public int RejectedComplaints { get; set; }
    public List<ComplaintSummaryDto> Complaints { get; set; } = new();

    public string HealthStatus { get; set; } = string.Empty;     
    public string StatusIcon { get; set; } = string.Empty;       
    public string PriorityIcon { get; set; } = string.Empty;     
    public bool IsAtRisk { get; set; }                            
    public bool IsOverdue { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCanceled { get; set; }

    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
}

public class ReminderSummaryDto
{
    public int Id { get; set; }
    public DateTime TriggerAt { get; set; }
    public DateTime? NextTriggerAt { get; set; }
    public EReminderStatus Status { get; set; }
    public string? ReminderRuleName { get; set; }
    public EReminderTriggerType? TriggerType { get; set; }
}

public class ComplaintSummaryDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public EComplainType Type { get; set; }
    public EComplainStatus Status { get; set; }
    public DateTime? NewDueDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
