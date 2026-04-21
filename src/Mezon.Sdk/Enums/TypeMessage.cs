namespace Mezon.Sdk.Enums;

/// <summary>
/// Message type constants used in the realtime protocol.
/// </summary>
public static class TypeMessage
{
    public const int Chat = 0;
    public const int ChatUpdate = 1;
    public const int ChatRemove = 2;
    public const int Typing = 3;
    public const int Indicator = 4;
    public const int Welcome = 5;
    public const int CreateThread = 6;
    public const int CreatePin = 7;
    public const int MessageBuzz = 8;
    public const int Topic = 9;
    public const int AuditLog = 10;
    public const int SendToken = 11;
    public const int Ephemeral = 12;
    public const int UpcomingEvent = 13;
    public const int UpdateEphemeralMsg = 14;
    public const int DeleteEphemeralMsg = 15;
}
