using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

/// <summary>
/// Comprehensive Task Report DTO - contains all information related to a task
/// Includes task details, team/project context, reminders, complaints, and derived metrics
/// </summary>
public class ComprehensiveTaskReportDto
{
    // ===== TASK CORE INFORMATION =====
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // ===== ASSIGNMENT & PEOPLE =====
    public string AssignedTo { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;

    // ===== STATUS & PRIORITY =====
    public ETaskStatus Status { get; set; }
    public EPriorityLevel Priority { get; set; }

    // ===== DATES & TIMELINE =====
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ===== CALCULATED TIME METRICS =====
    public int DaysOverdue { get; set; }                      // < 0 if not overdue
    public int DaysUntilDue { get; set; }                     // < 0 if overdue
    public int TotalDaysAllocated { get; set; }               // Due - Created
    public double ProgressPercentage { get; set; }            // Based on days passed vs total
    public string TimeStatusIcon { get; set; } = string.Empty; // 🟢/🟡/🔴

    // ===== TEAM & PROJECT CONTEXT =====
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }

    // ===== CLAN & CHANNEL ASSOCIATIONS =====
    public List<string> ClanIds { get; set; } = new();
    public List<string> ChannelIds { get; set; } = new();
    public List<string> ThreadIds { get; set; } = new();

    // ===== REMINDERS INFORMATION =====
    public int TotalReminders { get; set; }
    public int PendingReminders { get; set; }
    public int SentReminders { get; set; }
    public DateTime? NextReminderAt { get; set; }
    public List<ReminderSummaryDto> Reminders { get; set; } = new();

    // ===== COMPLAINTS INFORMATION =====
    public int TotalComplaints { get; set; }
    public int PendingComplaints { get; set; }
    public int ApprovedComplaints { get; set; }
    public int RejectedComplaints { get; set; }
    public List<ComplaintSummaryDto> Complaints { get; set; } = new();

    // ===== HEALTH STATUS INDICATORS =====
    public string HealthStatus { get; set; } = string.Empty;     // "On Track" / "At Risk" / "Overdue" / "Done"
    public string StatusIcon { get; set; } = string.Empty;       // ✅/⚠️/🔴/❌
    public string PriorityIcon { get; set; } = string.Empty;     // 🟢/🟡/🔴/🔥
    public bool IsAtRisk { get; set; }                            // Due within 3 days
    public bool IsOverdue { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCanceled { get; set; }

    // ===== METADATA =====
    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Reminder summary for task report
/// </summary>
public class ReminderSummaryDto
{
    public int Id { get; set; }
    public DateTime TriggerAt { get; set; }
    public DateTime? NextTriggerAt { get; set; }
    public EReminderStatus Status { get; set; }
    public string? ReminderRuleName { get; set; }
    public EReminderTriggerType? TriggerType { get; set; }
}

/// <summary>
/// Complaint summary for task report
/// </summary>
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
