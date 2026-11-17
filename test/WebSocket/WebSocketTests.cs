using System.Net.WebSockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.WebSocket;

public class WebSocketTests
{
    private static async Task<(HttpServer server, string url, Task<WebSocketClient> session)> Start(
        Action<WebSocketClient>? onSession = null)
    {
        var session = Wait.Signal<WebSocketClient>();
        var server = new HttpServer();

        server.Map.WebSocket("/ws", (_, socket) =>
        {
            var ws = (WebSocketClient)socket;
            onSession?.Invoke(ws);
            session.TrySetResult(ws);
        });

        var url = $"127.0.0.1:{Net.FreeTcpPort()}";
        await server.OpenAsync(new Uri($"http://{url}"));

        return (server, url, session.Task);
    }

    [Fact]
    public void DefaultConfig()
    {
        var client = new WebSocketClient();

        Assert.False(client.IsOpened);
        Assert.NotNull(client.Host);
        Assert.Null(client.Request);
    }

    [Fact]
    public async Task HandshakeAndClose()
    {
        var (server, url, session) = await Start();
        var client = new WebSocketClient();
        int open = 0, close = 0, modify = 0, error = 0;
        client.OnOpen(() => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnModify(_ => Interlocked.Increment(ref modify));
        client.OnError(_ => Interlocked.Increment(ref error));

        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        var peer = await Wait.For(TaskToSignal(session), what: "server session");
        Assert.True(client.IsOpened);
        Assert.True(peer.IsOpened);
        Assert.NotNull(peer.Request);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await client.CloseAsync();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);

        await Wait.Until(() => !peer.IsOpened, what: "server session closed");
        await server.CloseAsync();
    }

    [Fact]
    public async Task CloseAfterPeerAbort_RaisesCloseOnceWithoutError()
    {
        var (server, url, session) = await Start();
        var client = new WebSocketClient();
        int close = 0, error = 0;
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnError(_ => Interlocked.Increment(ref error));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));
        await Wait.For(TaskToSignal(session), what: "server session");

        await server.CloseAsync();
        await Wait.Until(() => !client.IsOpened, what: "client sees server gone");
        await client.CloseAsync();
        await client.CloseAsync();
        await Wait.Until(() => close > 0, what: "client close event");

        Assert.Equal(1, close);
        Assert.Equal(0, error);
    }

    [Theory]
    [InlineData(WebSocketDelivery.Text)]
    [InlineData(WebSocketDelivery.Binary)]
    public async Task EchoData(WebSocketDelivery type)
    {
        var (server, url, _) = await Start(ws => ws.OnData((data, t) => ws.SendData(data.GetBytes(), t)));
        var client = new WebSocketClient();
        var received = Wait.Signal<(byte[] data, WebSocketDelivery type)>();
        client.OnData((data, t) => received.TrySetResult((data.GetBytes(), t)));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        var payload = type == WebSocketDelivery.Text ? Encoding.UTF8.GetBytes("olá ✓ websocket") : Net.Payload(5000);
        client.SendData(payload, type);

        var (data, receivedType) = await Wait.For(received, what: "echo");
        Assert.Equal(payload, data);
        Assert.Equal(type, receivedType);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, url, _) = await Start(ws => ws.OnEvent((name, data, t) => ws.SendEvent(name, data.GetBytes(), t)));
        var client = new WebSocketClient();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.OnEvent((name, data, _) => received.TrySetResult((name, data.GetBytes())));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        var payload = Net.Payload(256, 9);
        client.SendEvent("chat.message", payload, WebSocketDelivery.Binary);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("chat.message", name);
        Assert.Equal(payload, data);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task LargeMessageIsReassembled()
    {
        var (server, url, _) = await Start(ws => ws.OnData((data, t) => ws.SendData(data.GetBytes(), t)));
        var client = new WebSocketClient();
        var received = Wait.Signal<byte[]>();
        client.OnData((data, _) => received.TrySetResult(data.GetBytes()));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        var payload = Net.Payload(1024 * 1024);
        client.SendData(payload, WebSocketDelivery.Binary);

        Assert.Equal(payload, await Wait.For(received, what: "1MB echo"));

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task ManyMessagesKeepOrder()
    {
        const int total = 200;
        var (server, url, _) = await Start(ws => ws.OnData((data, t) => ws.SendData(data.GetBytes(), t)));
        var client = new WebSocketClient();
        var received = new List<string>();
        client.OnData((data, _) =>
        {
            lock (received) received.Add(Encoding.UTF8.GetString(data.GetBytes()));
        });
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        for (var i = 0; i < total; i++) client.SendData(i.ToString(), WebSocketDelivery.Text);

        await Wait.Until(() => { lock (received) return received.Count == total; }, what: "all messages");
        lock (received) Assert.Equal(Enumerable.Range(0, total).Select(i => i.ToString()), received);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task ServerBroadcastReachesEveryClient()
    {
        const int total = 5;
        var (server, url, _) = await Start();
        var count = 0;
        var clients = new List<WebSocketClient>();

        for (var i = 0; i < total; i++)
        {
            var client = new WebSocketClient();
            client.OnData((data, _) =>
            {
                if (Encoding.UTF8.GetString(data.GetBytes()) == "hello all") Interlocked.Increment(ref count);
            });
            await client.OpenAsync(new Uri($"ws://{url}/ws"));
            clients.Add(client);
        }

        await Task.Delay(300);
        server.BroadcastWebSocketData(Encoding.UTF8.GetBytes("hello all"), WebSocketDelivery.Text);

        await Wait.Until(() => count == total, what: "broadcast delivered");

        foreach (var client in clients) await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task ServerCloseDisconnectsClient()
    {
        var (server, url, _) = await Start();
        var client = new WebSocketClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        await server.CloseAsync();

        await Wait.For(closed, what: "client close after server close");
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task ClientObservesServerSideClose()
    {
        var (server, url, session) = await Start();
        var client = new WebSocketClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(new Uri($"ws://{url}/ws"));

        var peer = await Wait.For(TaskToSignal(session));
        await peer.CloseAsync();

        await Wait.For(closed, what: "client close after session close");

        await server.CloseAsync();
    }

    [Fact]
    public async Task CloseWithStatus()
    {
        var (server, url, session) = await Start();
        var serverStatus = Wait.Signal<WebSocketCloseStatus>();
        var client = new WebSocketClient();
        await client.OpenAsync(new Uri($"ws://{url}/ws"));
        var peer = await Wait.For(TaskToSignal(session));
        peer.OnClose(status => serverStatus.TrySetResult(status));

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure);

        Assert.Equal(WebSocketCloseStatus.NormalClosure, await Wait.For(serverStatus, what: "close status"));

        await server.CloseAsync();
    }

    [Fact]
    public async Task UnknownPathIsRejected()
    {
        var (server, url, _) = await Start();
        var client = new WebSocketClient();
        var events = new List<string>();
        var finished = Wait.Signal<bool>();
        client.OnOpen(() => { lock (events) events.Add("open"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            finished.TrySetResult(true);
        });
        client.OnError(e =>
        {
            lock (events) events.Add("error");
            finished.TrySetResult(true);
        });

        await client.OpenAsync(new Uri($"ws://{url}/empty"));

        try
        {
            await Wait.For(finished, what: "rejected connection");
        }
        catch (TimeoutException)
        {
            string log;
            lock (events) log = string.Join(",", events);
            throw new Exception($"events: [{log}] opened: {client.IsOpened}");
        }

        Assert.False(client.IsOpened);

        await server.CloseAsync();
    }

    [Fact]
    public async Task ConnectionRefusedRaisesError()
    {
        var client = new WebSocketClient();
        var error = Wait.Signal<Exception>();
        client.OnError(e => error.TrySetResult(e));

        await client.OpenAsync(new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/ws"));

        Assert.NotNull(await Wait.For(error, what: "connection error"));
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task SendBeforeOpen_CallsOnError()
    {
        var client = new WebSocketClient();
        var error = false;
        client.OnError(_ => error = true);

        client.SendData("not sent", WebSocketDelivery.Text);
        await client.CloseAsync();

        Assert.False(client.IsOpened);
        Assert.True(error);
    }

    private static TaskCompletionSource<T> TaskToSignal<T>(Task<T> task)
    {
        var source = Wait.Signal<T>();
        task.ContinueWith(t => source.TrySetResult(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        return source;
    }
}
