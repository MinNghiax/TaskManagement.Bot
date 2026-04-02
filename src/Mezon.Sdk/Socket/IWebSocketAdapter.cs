namespace Mezon.Sdk.Socket;

using Mezon.Sdk.Proto;

public interface IWebSocketAdapter : IAsyncDisposable
{
    bool IsOpen { get; }
    Task ConnectAsync(string url, CancellationToken ct = default);
    Task SendAsync(Envelope envelope, CancellationToken ct = default);
    IAsyncEnumerable<Envelope> ReceiveAsync(CancellationToken ct = default);
    Task CloseAsync();
}
