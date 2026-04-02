namespace TaskManagement.Bot.Shared.Utils;

/// <summary>
/// Shared date/time utilities
/// </summary>
public static class DateTimeHelper
{
    public static string FormatForDisplay(DateTime dateTime) 
        => dateTime.ToString("dd/MM/yyyy HH:mm");
    
    public static bool IsDeadlineExpired(DateTime deadline) 
        => deadline < DateTime.UtcNow;
}
