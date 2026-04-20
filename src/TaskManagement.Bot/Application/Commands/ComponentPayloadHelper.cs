using System.Text.Json;

namespace TaskManagement.Bot.Application.Commands;

internal static class ComponentPayloadHelper
{
    public static string? GetCustomId(JsonElement payload) =>
        GetNestedScalarString(payload, "Data", "CustomId")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "ButtonId")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "buttonId")
        ?? GetNestedScalarString(payload, "buttonId");

    public static string? GetChannelId(JsonElement payload) =>
        GetNestedScalarString(payload, "ChannelId")
        ?? GetNestedScalarString(payload, "channelId")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "ChannelId")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "channelId");

    public static string? GetClanId(JsonElement payload) =>
        GetNestedScalarString(payload, "ClanId")
        ?? GetNestedScalarString(payload, "clanId");

    public static string? GetUserId(JsonElement payload) =>
        GetNestedScalarString(payload, "UserId")
        ?? GetNestedScalarString(payload, "userId")
        ?? GetNestedScalarString(payload, "SenderId")
        ?? GetNestedScalarString(payload, "senderId")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "UserId")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "SenderId")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "userId")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "senderId");

    public static string? GetMessageId(JsonElement payload) =>
        GetNestedScalarString(payload, "MessageId")
        ?? GetNestedScalarString(payload, "messageId")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "MessageId")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "messageId");

    public static string? GetExtraData(JsonElement payload) =>
        GetNestedScalarString(payload, "ExtraData")
        ?? GetNestedScalarString(payload, "extraData")
        ?? GetNestedScalarString(payload, "MessageButtonClicked", "ExtraData")
        ?? GetNestedScalarString(payload, "messageButtonClicked", "extraData");

    public static JsonElement GetValues(JsonElement payload) =>
        GetNestedProperty(payload, "Data", "Values");

    public static JsonElement GetNestedProperty(JsonElement element, params string[] propertyNames)
    {
        var current = element;

        foreach (var propertyName in propertyNames)
        {
            var next = GetPropertyIgnoreCase(current, propertyName);
            if (next == null)
            {
                return default;
            }

            current = next.Value;
        }

        return current;
    }

    public static string? GetNestedScalarString(JsonElement element, params string[] propertyNames)
    {
        var property = GetNestedProperty(element, propertyNames);
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    public static JsonElement? GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }
}
