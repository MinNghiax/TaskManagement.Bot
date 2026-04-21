using System.IdentityModel.Tokens.Jwt;

namespace Mezon.Sdk.Domain;

public sealed class Session
{
    public string Token { get; internal set; } = string.Empty;
    public string RefreshToken { get; internal set; } = string.Empty;
    public string? UserId { get; internal set; }
    public long CreatedAt { get; internal set; }
    public long ExpiresAt { get; internal set; }
    public long RefreshExpiresAt { get; internal set; }
    public string? ApiUrl { get; internal set; }
    public string? WsUrl { get; internal set; }
    public string? IdToken { get; internal set; }
    public Dictionary<string, string> Vars { get; } = new();

    internal Session() { }

    public Session(ApiSession response)
    {
        Token = response.Token ?? string.Empty;
        RefreshToken = response.RefreshToken ?? string.Empty;
        UserId = response.UserId;
        ApiUrl = response.ApiUrl;
        WsUrl = response.WsUrl;
        IdToken = response.IdToken;
        ParseJwt();
    }

    public static Session Restore(string token)
    {
        var session = new Session { Token = token };
        session.ParseJwt();
        return session;
    }

    private void ParseJwt()
    {
        if (string.IsNullOrEmpty(Token)) return;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(Token))
            {
                var jwt = handler.ReadJwtToken(Token);
                CreatedAt = jwt.IssuedAt != default ? jwt.IssuedAt.ToUniversalTime().Ticks : 0L;
                var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                    ExpiresAt = exp;
                var refreshExpClaim = jwt.Claims.FirstOrDefault(c => c.Type == "refresh_exp");
                if (refreshExpClaim != null && long.TryParse(refreshExpClaim.Value, out var rExp))
                    RefreshExpiresAt = rExp;
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "user_id");
                UserId ??= userIdClaim?.Value;
            }
        }
        catch
        {
        }
    }

    public bool IsExpired() => ExpiresAt > 0 && ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool IsRefreshExpired() => RefreshExpiresAt > 0 && RefreshExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void Update(string token, string refreshToken)
    {
        Token = token;
        RefreshToken = refreshToken;
        ParseJwt();
    }
}
