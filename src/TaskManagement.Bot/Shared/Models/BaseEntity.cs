namespace TaskManagement.Bot.Shared.Models;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
