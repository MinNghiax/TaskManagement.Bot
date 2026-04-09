namespace TaskManagement.Bot.Application.DTOs;

using TaskManagement.Bot.Infrastructure.Enums;

public class ComplainDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ComplainType { get; set; }
    public string? CreatedBy { get; set; }
    public string? MezonUserId { get; set; }
    public EComplainStatus Status { get; set; }
    public string? RespondedBy { get; set; }
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int SupportCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateComplainDto
{
    public int TaskId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string ComplainType { get; set; }
    public required string CreatedBy { get; set; }
    public required string MezonUserId { get; set; }
}

public class UpdateComplainStatusDto
{
    public EComplainStatus Status { get; set; }
    public string? Response { get; set; }
    public string? RespondedBy { get; set; }
}
