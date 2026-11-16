using System.Net.WebSockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.WebSocket;

public class WebSocketTests
{
    private static async Task<(HTTP.Server server, string url, Task<HTTP.WebSocket> session)> Start(
        Action<HTTP.WebSocket>? onSession = null)
    {
        var session = Wait.Signal<HTTP.WebSocket>();
        var server = new HTTP.Server();

        server.Map.WebSocket("/ws", (_, socket) =>
        {
            var ws = (HTTP.WebSocket)socket;
            onSession?.Invoke(ws);
            session.TrySetResult(ws);
        });

        var url = $"127.0.0.1:{Net.FreeTcpPort()}";
        await server.To.Open(new Uri($"http://{url}"));

        return (server, url, session.Task);
    }

    [Fact]
    public void DefaultConfig()
    {
        var client = new HTTP.WebSocket();

        Assert.False(client.IsOpened);
        Assert.NotNull(client.Host);
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
        Assert.Null(client.ServerRequest);
    }

    [Fact]
    public async Task HandshakeAndClose()
    {
        var (server, url, session) = await Start();
        var client = new HTTP.WebSocket();
        int open = 0, close = 0, modify = 0, error = 0;
        client.On.Open(() => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Modify(_ => Interlocked.Increment(ref modify));
        client.On.Error(_ => Interlocked.Increment(ref error));

        await client.To.Open(new Uri($"ws://{url}/ws"));

        var peer = await Wait.For(TaskToSignal(session), what: "server session");
        Assert.True(client.IsOpened);
        Assert.True(peer.IsOpened);
        Assert.NotNull(peer.ServerRequest);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);

        await Wait.Until(() => !peer.IsOpened, what: "server session closed");
        await server.To.Close();
    }

    [Theory]
    [InlineData(HTTP.MessageType.Text)]
    [InlineData(HTTP.MessageType.Binary)]
    public async Task EchoData(HTTP.MessageType type)
    {
        var (server, url, _) = await Start(ws => ws.On.Data((data, t) => ws.To.Data(data, t)));
        var client = new HTTP.WebSocket();
        var received = Wait.Signal<(byte[] data, HTTP.MessageType type)>();
        client.On.Data((data, t) => received.TrySetResult((data, t)));
        await client.To.Open(new Uri($"ws://{url}/ws"));

        var payload = type == HTTP.MessageType.Text ? Encoding.UTF8.GetBytes("olá ✓ websocket") : Net.Payload(5000);
        client.To.Data(payload, type);

        var (data, receivedType) = await Wait.For(received, what: "echo");
        Assert.Equal(payload, data);
        Assert.Equal(type, receivedType);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, url, _) = await Start(ws => ws.On.Event((name, data, t) => ws.To.Event(name, data, t)));
        var client = new HTTP.WebSocket();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.On.Event((name, data, _) => received.TrySetResult((name, data)));
        await client.To.Open(new Uri($"ws://{url}/ws"));

        var payload = Net.Payload(256, 9);
        client.To.Event("chat.message", payload, HTTP.MessageType.Binary);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("chat.message", name);
        Assert.Equal(payload, data);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task LargeMessageIsReassembled()
    {
        var (server, url, _) = await Start(ws => ws.On.Data((data, t) => ws.To.Data(data, t)));
        var client = new HTTP.WebSocket();
        var received = Wait.Signal<byte[]>();
        client.On.Data((data, _) => received.TrySetResult(data));
        await client.To.Open(new Uri($"ws://{url}/ws"));

        var payload = Net.Payload(1024 * 1024);
        client.To.Data(payload, HTTP.MessageType.Binary);

        Assert.Equal(payload, await Wait.For(received, what: "1MB echo"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ManyMessagesKeepOrder()
    {
        const int total = 200;
        var (server, url, _) = await Start(ws => ws.On.Data((data, t) => ws.To.Data(data, t)));
        var client = new HTTP.WebSocket();
        var received = new List<string>();
        client.On.Data((data, _) =>
        {
            lock (received) received.Add(Encoding.UTF8.GetString(data));
        });
        await client.To.Open(new Uri($"ws://{url}/ws"));

        for (var i = 0; i < total; i++) client.To.Data(i.ToString(), HTTP.MessageType.Text);

        await Wait.Until(() => { lock (received) return received.Count == total; }, what: "all messages");
        lock (received) Assert.Equal(Enumerable.Range(0, total).Select(i => i.ToString()), received);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ServerBroadcastReachesEveryClient()
    {
        const int total = 5;
        var (server, url, _) = await Start();
        var count = 0;
        var clients = new List<HTTP.WebSocket>();

        for (var i = 0; i < total; i++)
        {
            var client = new HTTP.WebSocket();
            client.On.Data((data, _) =>
            {
                if (Encoding.UTF8.GetString(data) == "hello all") Interlocked.Increment(ref count);
            });
            await client.To.Open(new Uri($"ws://{url}/ws"));
            clients.Add(client);
        }

        await Task.Delay(300);
        server.To.WebsocketDataBroadcast("hello all", HTTP.MessageType.Text);

        await Wait.Until(() => count == total, what: "broadcast delivered");

        foreach (var client in clients) await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ServerCloseDisconnectsClient()
    {
        var (server, url, _) = await Start();
        var client = new HTTP.WebSocket();
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));
        await client.To.Open(new Uri($"ws://{url}/ws"));

        await server.To.Close();

        await Wait.For(closed, what: "client close after server close");
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task ClientObservesServerSideClose()
    {
        var (server, url, session) = await Start();
        var client = new HTTP.WebSocket();
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));
        await client.To.Open(new Uri($"ws://{url}/ws"));

        var peer = await Wait.For(TaskToSignal(session));
        await peer.To.Close();

        await Wait.For(closed, what: "client close after session close");

        await server.To.Close();
    }

    [Fact]
    public async Task CloseWithStatus()
    {
        var (server, url, session) = await Start();
        var serverStatus = Wait.Signal<WebSocketCloseStatus>();
        var client = new HTTP.WebSocket();
        await client.To.Open(new Uri($"ws://{url}/ws"));
        var peer = await Wait.For(TaskToSignal(session));
        peer.On.Close(status => serverStatus.TrySetResult(status));

        await client.To.Close(WebSocketCloseStatus.NormalClosure);

        Assert.Equal(WebSocketCloseStatus.NormalClosure, await Wait.For(serverStatus, what: "close status"));

        await server.To.Close();
    }

    [Fact]
    public async Task UnknownPathIsRejected()
    {
        var (server, url, _) = await Start();
        var client = new HTTP.WebSocket();
        var events = new List<string>();
        var finished = Wait.Signal<bool>();
        client.On.Open(() => { lock (events) events.Add("open"); });
        client.On.Close(() =>
        {
            lock (events) events.Add("close");
            finished.TrySetResult(true);
        });
        client.On.Error(e =>
        {
            lock (events) events.Add("error");
            finished.TrySetResult(true);
        });

        await client.To.Open(new Uri($"ws://{url}/empty"));

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

        await server.To.Close();
    }

    [Fact]
    public async Task ConnectionRefusedRaisesError()
    {
        var client = new HTTP.WebSocket();
        var error = Wait.Signal<Exception>();
        client.On.Error(e => error.TrySetResult(e));

        await client.To.Open(new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/ws"));

        Assert.NotNull(await Wait.For(error, what: "connection error"));
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task SendBeforeOpenIsIgnored()
    {
        var client = new HTTP.WebSocket();
        var error = false;
        client.On.Error(_ => error = true);

        client.To.Data("ignored", HTTP.MessageType.Text);
        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.False(error);
    }

    private static TaskCompletionSource<T> TaskToSignal<T>(Task<T> task)
    {
        var source = Wait.Signal<T>();
        task.ContinueWith(t => source.TrySetResult(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        return source;
    }
}
