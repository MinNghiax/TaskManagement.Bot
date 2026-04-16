using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.DTOs;

/// <summary>
/// Enhanced Personal Task Report - shows user's task overview with at-risk and overdue tracking
/// </summary>
public class EnhancedPersonalTaskReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;

    // ===== TASK STATUS BREAKDOWN =====
    public int TotalTasks { get; set; }
    public int ToDoCount { get; set; }
    public int DoingCount { get; set; }
    public int ReviewCount { get; set; }
    public int PausedCount { get; set; }
    public int LateCount { get; set; }
    public int CompletedCount { get; set; }
    public int CanceledCount { get; set; }

    // ===== METRICS & HEALTH =====
    public double CompletionRate { get; set; }
    public int TotalOverdueDays { get; set; }
    public int OverdueTasksCount { get; set; }
    public int AtRiskTasksCount { get; set; }       // Due within 3 days
    public double HealthScore { get; set; }         // 0-100: based on completion + overdue

    // ===== TEAM CONTEXT =====
    public int TeamCount { get; set; }
    public List<string> TeamNames { get; set; } = new();

    // ===== REMINDER & COMPLAINT SUMMARY =====
    public int TotalPendingReminders { get; set; }
    public int TotalActiveComplaints { get; set; }

    // ===== DETAILED LISTS =====
    public List<ComprehensiveTaskReportDto> AllTasks { get; set; } = new();
    public List<ComprehensiveTaskReportDto> OverdueTasks { get; set; } = new();
    public List<ComprehensiveTaskReportDto> AtRiskTasks { get; set; } = new();
}

/// <summary>
/// Team Health Report - shows team's overall task health and member performance
/// </summary>
public class TeamHealthReportDto
{
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;

    // ===== TEAM METRICS =====
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }          // Members with tasks
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double TeamCompletionRate { get; set; }

    // ===== TEAM HEALTH =====
    public int OverdueTasksCount { get; set; }
    public int AtRiskTasksCount { get; set; }
    public double OverduePercentage { get; set; }
    public string TeamHealthStatus { get; set; } = string.Empty;  // "Healthy" / "At Risk" / "Critical"

    // ===== MEMBER BREAKDOWN =====
    public List<MemberTaskBreakdownDto> MemberBreakdowns { get; set; } = new();

    // ===== PRIORITY DISTRIBUTION =====
    public int LowPriorityTasks { get; set; }
    public int MediumPriorityTasks { get; set; }
    public int HighPriorityTasks { get; set; }
    public int CriticalPriorityTasks { get; set; }
}

/// <summary>
/// Member task breakdown for team health
/// </summary>
public class MemberTaskBreakdownDto
{
    public string MemberId { get; set; } = string.Empty;
    public string? MemberName { get; set; }

    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }

    public int OverdueCount { get; set; }
    public int AtRiskCount { get; set; }

    public EPriorityLevel? HighestPriorityTask { get; set; }
    public string? CriticalTaskTitle { get; set; }

    public double WorkloadScore { get; set; }        // Based on open tasks
}

/// <summary>
/// Task Analytics Report - productivity and velocity tracking
/// </summary>
public class TaskAnalyticsReportDto
{
    public string Title { get; set; } = "Task Analytics Report";
    public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
    public ETimeRange? ReportPeriod { get; set; }
    public DateTime? PeriodStartDate { get; set; }
    public DateTime? PeriodEndDate { get; set; }

    // ===== VELOCITY METRICS =====
    public int TasksCreated { get; set; }
    public int TasksCompleted { get; set; }
    public double CompletionVelocity { get; set; }   // Completed / (days in period)
    public double DeliveryRate { get; set; }         // % of created that completed in period

    // ===== PRIORITY ANALYSIS =====
    public int CriticalTasksCompleted { get; set; }
    public int CriticalTasksPending { get; set; }
    public double CriticalCompletionRate { get; set; }

    // ===== COMPLAINT ANALYTICS =====
    public int TotalComplaints { get; set; }
    public int TimeExtensionRequests { get; set; }
    public int CancellationRequests { get; set; }
    public int PauseRequests { get; set; }
    public double ApprovalRate { get; set; }

    // ===== TIME ANALYSIS =====
    public double AverageDaysToComplete { get; set; }
    public int TasksCompletedOnTime { get; set; }
    public int TasksCompletedLate { get; set; }
    public double OnTimeDeliveryRate { get; set; }
}
