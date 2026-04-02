namespace TaskManagement.Bot.Shared.Utils;

/// <summary>
/// Shared validation helpers
/// </summary>
public static class ValidationHelper
{
    public static bool IsValidString(string? value) => !string.IsNullOrWhiteSpace(value);
    
    public static bool IsValidDateTime(DateTime dateTime) => dateTime > DateTime.MinValue;
}
