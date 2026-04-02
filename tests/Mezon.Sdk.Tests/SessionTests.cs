namespace Mezon.Sdk.Tests;

using System.Text;
using System.Text.Json;
using Mezon.Sdk.Session;
using Xunit;

public class SessionTests
{
    private static string MakeJwt(long exp)
    {
        var payload = JsonSerializer.Serialize(new { exp });
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        return $"header.{encoded}.signature";
    }

    [Fact]
    public void Session_ParsesExpiryFromToken()
    {
        var future = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var jwt = MakeJwt(future);
        var session = new Session(jwt, "refresh");
        Assert.Equal(future, session.ExpiresAt);
        Assert.False(session.IsExpired());
    }

    [Fact]
    public void Session_DetectsExpiredToken()
    {
        var past = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var jwt = MakeJwt(past);
        var session = new Session(jwt, "refresh");
        Assert.True(session.IsExpired());
    }

    [Fact]
    public void Session_StoresUserIdAndUrls()
    {
        var jwt = MakeJwt(9999999999L);
        var session = new Session(jwt, "rt", "user123", "https://api.mezon.ai", "wss://ws.mezon.ai");
        Assert.Equal("user123", session.UserId);
        Assert.Equal("https://api.mezon.ai", session.ApiUrl);
        Assert.Equal("wss://ws.mezon.ai", session.WsUrl);
    }
}
