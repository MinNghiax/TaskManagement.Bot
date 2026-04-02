namespace Mezon.Sdk.Tests;

using Mezon.Sdk.Utils;
using Xunit;

public class CacheManagerTests
{
    [Fact]
    public void CacheManager_SetAndGet()
    {
        var cache = new CacheManager<string, int>();
        cache.Set("a", 42);
        Assert.Equal(42, cache.Get("a"));
    }

    [Fact]
    public void CacheManager_GetReturnsDefaultWhenMissing()
    {
        var cache = new CacheManager<string, string>();
        Assert.Null(cache.Get("missing"));
    }

    [Fact]
    public void CacheManager_Delete()
    {
        var cache = new CacheManager<string, int>();
        cache.Set("x", 1);
        cache.Delete("x");
        Assert.Equal(0, cache.Get("x"));
    }

    [Fact]
    public async Task CacheManager_FetchCallsFetcher()
    {
        var cache = new CacheManager<string, int>(async key =>
        {
            await Task.CompletedTask;
            return key == "42" ? 42 : -1;
        });

        var result = await cache.FetchAsync("42");
        Assert.Equal(42, result);
        // Second fetch should hit cache
        var cached = await cache.FetchAsync("42");
        Assert.Equal(42, cached);
    }
}
