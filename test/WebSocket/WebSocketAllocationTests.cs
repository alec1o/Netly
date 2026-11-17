using NetlyTest.Support;

namespace NetlyTest.WebSocket;

public class WebSocketAllocationTests
{
    private static async Task<(WebSocketServer server, Uri uri)> Start(Action<WebSocketClient> onAccept,
        Func<long, Stream>? allocation = null)
    {
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");

        if (allocation != null) server.OnDynamicAllocation(allocation);
        server.OnAccept(onAccept);
        await server.OpenAsync(uri);

        return (server, uri);
    }

    [Fact]
    public async Task FirstCallIsZero_ThenGrowsByDoubling_AndContentIsKept()
    {
        var calls = new List<long>();
        var received = Wait.Signal<byte[]>();
        var payload = Net.Payload(40_000);

        var (server, uri) = await Start(c => c.OnData((s, _) =>
        {
            var data = new byte[s.Length];
            s.Position = 0;
            s.Read(data, 0, data.Length);
            received.TrySetResult(data);
        }), size =>
        {
            lock (calls) calls.Add(size);
            return new MemoryStream();
        });

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "message"));

        lock (calls)
        {
            Assert.Equal(0, calls[0]);
            Assert.True(calls.Count > 1, "the message is bigger than the first stream, it must grow");
            Assert.True(calls.Skip(1).All(x => x > 0));
            Assert.Equal(calls.Skip(1).OrderBy(x => x), calls.Skip(1));
        }

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task NullKeepsTheCurrentStream()
    {
        var calls = 0;
        var created = new List<MemoryStream>();
        var received = Wait.Signal<int>();

        var (server, uri) = await Start(c => c.OnData((s, _) => received.TrySetResult((int)s.Length)), size =>
        {
            Interlocked.Increment(ref calls);
            if (size != 0) return null!;

            var stream = new MemoryStream();
            lock (created) created.Add(stream);
            return stream;
        });

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(Net.Payload(100_000));

        Assert.Equal(100_000, await Wait.For(received, what: "message"));
        Assert.True(calls > 1);
        lock (created) Assert.Single(created);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task AllocationThrowing_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");

        server.OnDynamicAllocation(_ => throw new IOException("disk full"));
        server.OnAccept(c =>
        {
            c.OnError(_ => { lock (events) events.Add("error"); });
            c.OnClose(() =>
            {
                lock (events) events.Add("close");
                closed.TrySetResult(true);
            });
        });
        await server.OpenAsync(uri);

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(Net.Payload(100));

        Assert.True(await Wait.For(closed, what: "close after the allocation failed"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    }

    [Fact]
    public async Task FirstCallNull_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        var (server, uri) = await Start(c =>
        {
            c.OnError(_ => { lock (events) events.Add("error"); });
            c.OnClose(() =>
            {
                lock (events) events.Add("close");
                closed.TrySetResult(true);
            });
        }, _ => null!);

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(Net.Payload(100));

        Assert.True(await Wait.For(closed, what: "close after null"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    }

    [Fact]
    public async Task UserCallbackThrowing_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        var (server, uri) = await Start(c =>
        {
            c.OnError(_ => { lock (events) events.Add("error"); });
            c.OnClose(() =>
            {
                lock (events) events.Add("close");
                closed.TrySetResult(true);
            });
            c.OnData((_, _) => throw new InvalidOperationException("user callback"));
        });

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(Net.Payload(100));

        Assert.True(await Wait.For(closed, what: "close after the exception of the app"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    }

    [Fact]
    public async Task MessageAboveTheLimit_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var server = new WebSocketServer { FramingSize = 4096 };
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");

        server.OnAccept(c =>
        {
            c.OnError(_ => { lock (events) events.Add("error"); });
            c.OnClose(() =>
            {
                lock (events) events.Add("close");
                closed.TrySetResult(true);
            });
        });
        await server.OpenAsync(uri);

        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        client.SendData(Net.Payload(20_000));

        Assert.True(await Wait.For(closed, what: "close after the message above the limit"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    }

    [Fact]
    public async Task OpenFailure_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var client = new WebSocketClient();

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });

        client.Open(new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat"));

        Assert.True(await Wait.For(closed, what: "close after the open failed"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);
    }

    [Fact]
    public async Task DynamicAllocation_CannotChangeWhileServerIsOpened()
    {
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");

        server.OnDynamicAllocation(_ => new MemoryStream());
        await server.OpenAsync(uri);

        Assert.Throws<InvalidOperationException>(() => server.OnDynamicAllocation(_ => new MemoryStream()));

        await server.CloseAsync();
    }
}
