namespace Mezon.Sdk.Socket;

using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Mezon.Sdk.Proto;

public class WebSocketAdapterPb : IWebSocketAdapter
{
    private ClientWebSocket? _ws;
    public bool IsOpen => _ws?.State == WebSocketState.Open;

    public async Task ConnectAsync(string url, CancellationToken ct = default)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(url), ct);
    }

    public async Task SendAsync(Envelope envelope, CancellationToken ct = default)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) throw new InvalidOperationException("WebSocket not open");
        var bytes = envelope.ToByteArray();
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, ct);
    }

    public async IAsyncEnumerable<Envelope> ReceiveAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_ws == null) yield break;
        var buffer = new byte[65536];
        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            int total = 0;
            byte[] current = buffer;
            do
            {
                if (total >= current.Length)
                {
                    var bigger = new byte[current.Length * 2];
                    Array.Copy(current, bigger, total);
                    current = bigger;
                }
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(current, total, current.Length - total), ct);
                if (result.MessageType == WebSocketMessageType.Close) yield break;
                total += result.Count;
            } while (!result.EndOfMessage);

            Envelope envelope;
            try { envelope = Envelope.Parser.ParseFrom(current, 0, total); }
            catch { continue; }
            yield return envelope;
        }
    }

    public async Task CloseAsync()
    {
        if (_ws?.State == WebSocketState.Open)
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _ws?.Dispose();
    }
}
