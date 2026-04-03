namespace TaskManagement.Bot.Application.DTOs;

/// <summary>
/// Report DTO
/// </summary>
public class ReportDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? ReportType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Content { get; set; }
    public string? CreatedBy { get; set; }
    public string? Description { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int PendingTasks { get; set; }
    public int CancelledTasks { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create Report DTO
/// </summary>
public class CreateReportDto
{
    public required string Title { get; set; }
    public required string ReportType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required string Content { get; set; }
    public required string CreatedBy { get; set; }
    public string? Description { get; set; }
}
