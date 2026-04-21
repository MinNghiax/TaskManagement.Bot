using System.Net.WebSockets;

namespace Mezon.Sdk.Socket;

public sealed class WebSocketAdapter : IDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveCts;

    public event EventHandler<WebSocketCloseEventArgs>? OnClose;
    public event EventHandler<WebSocketErrorEventArgs>? OnError;
    public event EventHandler<ArraySegment<byte>>? OnMessage;
#pragma warning disable CS0067 
    public event EventHandler? OnOpen;
#pragma warning restore CS0067

    public bool IsOpen => _socket?.State == WebSocketState.Open;

    public void Connect(string url, CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        _socket.Options.AddSubProtocol("proto3");
        _socket.ConnectAsync(new Uri(url), cancellationToken).Wait(cancellationToken);
        _receiveCts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoop(_receiveCts.Token), _receiveCts.Token);
    }

    public Task ConnectAsync(string url, CancellationToken cancellationToken)
    {
        Connect(url, cancellationToken);
        return Task.CompletedTask;
    }

    public async Task SendAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
    {
        if (_socket?.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not open");
        await _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);
    }

    public void Close()
    {
        _receiveCts?.Cancel();
        if (_socket?.State == WebSocketState.Open)
        {
            try { _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait(); }
            catch {   }
        }
        _socket?.Dispose();
        _socket = null;
    }

    private void ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[65536];
        try
        {
            while (!ct.IsCancellationRequested && _socket?.State == WebSocketState.Open)
            {
                var result = _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).Result;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    OnClose?.Invoke(this, new WebSocketCloseEventArgs((int)(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure), result.CloseStatusDescription ?? ""));
                    break;
                }
                OnMessage?.Invoke(this, new ArraySegment<byte>(buffer, 0, result.Count));
            }
        }
        catch (OperationCanceledException) {   }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new WebSocketErrorEventArgs(ex));
        }
    }

    public void Dispose()
    {
        Close();
        _receiveCts?.Dispose();
    }
}

public class WebSocketCloseEventArgs : EventArgs
{
    public int Code { get; }
    public string Reason { get; }
    public WebSocketCloseEventArgs(int code, string reason) { Code = code; Reason = reason; }
}

public class WebSocketErrorEventArgs(Exception ex) : EventArgs
{
    public Exception Exception { get; } = ex;
}