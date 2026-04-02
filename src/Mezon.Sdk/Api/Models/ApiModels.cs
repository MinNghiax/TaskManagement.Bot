namespace Mezon.Sdk.Api.Models;

using System.Text.Json.Serialization;

public class ApiSession
{
    [JsonPropertyName("token")] public string Token { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("user_id")] public string? UserId { get; set; }
    [JsonPropertyName("api_url")] public string? ApiUrl { get; set; }
    [JsonPropertyName("ws_url")] public string? WsUrl { get; set; }
    [JsonPropertyName("created")] public bool Created { get; set; }
}

public class ApiClanDesc
{
    [JsonPropertyName("clan_id")] public string? ClanId { get; set; }
    [JsonPropertyName("clan_name")] public string? ClanName { get; set; }
    [JsonPropertyName("logo")] public string? Logo { get; set; }
    [JsonPropertyName("welcome_channel_id")] public string? WelcomeChannelId { get; set; }
    [JsonPropertyName("badge_count")] public int? BadgeCount { get; set; }
}

public class ApiClanDescList
{
    [JsonPropertyName("clandesc")] public List<ApiClanDesc>? ClanDesc { get; set; }
}

public class ApiChannelDescription
{
    [JsonPropertyName("channel_id")] public string? ChannelId { get; set; }
    [JsonPropertyName("clan_id")] public string? ClanId { get; set; }
    [JsonPropertyName("channel_label")] public string? ChannelLabel { get; set; }
    [JsonPropertyName("type")] public int? Type { get; set; }
    [JsonPropertyName("channel_private")] public int? ChannelPrivate { get; set; }
    [JsonPropertyName("user_ids")] public List<string>? UserIds { get; set; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; set; }
    [JsonPropertyName("category_name")] public string? CategoryName { get; set; }
    [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
    [JsonPropertyName("meeting_code")] public string? MeetingCode { get; set; }
}

public class ApiChannelDescList
{
    [JsonPropertyName("channeldesc")] public List<ApiChannelDescription>? ChannelDesc { get; set; }
}
