namespace Mezon.Sdk.Utils;

using System.Threading.Channels;

public class MessageQueue : IAsyncDisposable
{
    private const int MaxPerSecond = 80;
    private readonly Channel<Func<Task>> _channel = Channel.CreateUnbounded<Func<Task>>();
    private readonly Task _worker;
    private readonly CancellationTokenSource _cts = new();

    public MessageQueue() => _worker = RunWorkerAsync(_cts.Token);

    public async Task<T> EnqueueAsync<T>(Func<Task<T>> task)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _channel.Writer.WriteAsync(async () =>
        {
            try { tcs.SetResult(await task()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return await tcs.Task;
    }

    public Task EnqueueAsync(Func<Task> task)
        => EnqueueAsync(async () => { await task(); return (object?)null; });

    private async Task RunWorkerAsync(CancellationToken ct)
    {
        var timestamps = new Queue<long>();
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (timestamps.Count > 0 && now - timestamps.Peek() >= 1000)
                timestamps.Dequeue();
            if (timestamps.Count >= MaxPerSecond)
            {
                var oldest = timestamps.Peek();
                var wait = 1000 - (int)(now - oldest);
                if (wait > 0) await Task.Delay(wait, ct);
            }
            timestamps.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await item();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        try { await _worker; } catch { }
        _cts.Dispose();
    }
}
