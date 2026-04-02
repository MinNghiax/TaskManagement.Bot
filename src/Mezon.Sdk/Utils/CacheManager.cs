namespace Mezon.Sdk.Utils;

using System.Collections.Concurrent;

public class CacheManager<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _store = new();
    private readonly Func<TKey, Task<TValue?>>? _fetcher;

    public CacheManager(Func<TKey, Task<TValue?>>? fetcher = null) => _fetcher = fetcher;

    public TValue? Get(TKey key) => _store.TryGetValue(key, out var v) ? v : default;
    public void Set(TKey key, TValue value) => _store[key] = value;
    public bool Remove(TKey key) => _store.TryRemove(key, out _);
    public bool Delete(TKey key) => _store.TryRemove(key, out _);
    public IEnumerable<TValue> Values() => _store.Values;
    public TValue? First() => _store.Values.FirstOrDefault();

    public async Task<TValue?> FetchAsync(TKey key)
    {
        if (_store.TryGetValue(key, out var v)) return v;
        if (_fetcher == null) return default;
        var fetched = await _fetcher(key);
        if (fetched != null) _store[key] = fetched;
        return fetched;
    }
}
