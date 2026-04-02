namespace Mezon.Sdk.Session;

using System.Text;
using System.Text.Json;

public class Session
{
    public string Token { get; private set; }
    public string RefreshToken { get; private set; }
    public string? UserId { get; private set; }
    public string? ApiUrl { get; private set; }
    public string? WsUrl { get; private set; }
    public long ExpiresAt { get; private set; }
    public long RefreshExpiresAt { get; private set; }
    public long CreatedAt { get; private set; }

    public Session(string token, string refreshToken, string? userId = null, string? apiUrl = null, string? wsUrl = null)
    {
        Token = token;
        RefreshToken = refreshToken;
        UserId = userId;
        ApiUrl = apiUrl;
        WsUrl = wsUrl;
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ParseAndSetExpiry(token, refreshToken);
    }

    private void ParseAndSetExpiry(string token, string refreshToken)
    {
        ExpiresAt = DecodeJwtExp(token);
        if (!string.IsNullOrEmpty(refreshToken))
            RefreshExpiresAt = DecodeJwtExp(refreshToken);
    }

    private static long DecodeJwtExp(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return 0;
        var payload = parts[1];
        var padded = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
        var bytes = Convert.FromBase64String(padded);
        var json = Encoding.UTF8.GetString(bytes);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("exp", out var exp))
            return exp.GetInt64();
        return 0;
    }

    public bool IsExpired() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() > ExpiresAt;
    public bool IsRefreshExpired() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() > RefreshExpiresAt;
}
