using System.Text;
using NetlyTest.Support;

namespace NetlyTest.WebSocket;

public class WebSocketServerTests
{
    private static async Task<(WebSocketServer server, Uri uri)> Start(Action<WebSocketClient>? onAccept = null)
    {
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");

        if (onAccept != null) server.OnAccept(onAccept);

        await server.OpenAsync(uri);
        Assert.True(server.IsOpened);

        return (server, uri);
    }

    [Fact]
    public void DefaultConfig()
    {
        var server = new WebSocketServer();

        Assert.False(server.IsOpened);
        Assert.False(string.IsNullOrWhiteSpace(server.Id));
        Assert.Empty(server.Clients);
    }

    [Fact]
    public async Task AcceptEchoAndCleanup()
    {
        var (server, uri) = await Start(c => c.OnData((data, type) => c.SendData(data.GetBytes(), type)));
        var client = new WebSocketClient();
        var echoed = Wait.Signal<string>();
        client.OnData((data, _) => echoed.TrySetResult(Encoding.UTF8.GetString(data.GetBytes())));

        await client.OpenAsync(uri);
        await Wait.Until(() => server.Clients.Count == 1, what: "server accept");
        Assert.True(server.Clients.Single().IsServer);

        client.SendData("hello", WebSocketDelivery.Text);
        Assert.Equal("hello", await Wait.For(echoed, what: "echo"));

        await client.CloseAsync();
        await Wait.Until(() => server.Clients.Count == 0, what: "client removed");

        await server.CloseAsync();
    }

    [Fact]
    public async Task OtherPathIsNotAccepted()
    {
        var (server, uri) = await Start();
        var client = new WebSocketClient();
        var failed = Wait.Signal<bool>();
        client.OnError(_ => failed.TrySetResult(true));
        client.OnClose(() => failed.TrySetResult(true));

        client.Open(new Uri($"ws://127.0.0.1:{uri.Port}/other"));

        await Wait.For(failed, what: "refused connection");
        Assert.False(client.IsOpened);
        Assert.Empty(server.Clients);

        await server.CloseAsync();
    }

    [Fact]
    public async Task ServerCanReopenAfterClose()
    {
        var (server, uri) = await Start();

        for (var i = 0; i < 3; i++)
        {
            var client = new WebSocketClient();
            await client.OpenAsync(uri);
            Assert.True(client.IsOpened);
            await client.CloseAsync();

            await server.CloseAsync();
            Assert.False(server.IsOpened);
            await server.OpenAsync(uri);
            Assert.True(server.IsOpened);
        }

        await server.CloseAsync();
    }

    [Fact]
    public async Task BroadcastWithCondition()
    {
        var (server, uri) = await Start();
        var received = new System.Collections.Concurrent.ConcurrentBag<string>();
        var clients = new List<WebSocketClient>();

        for (var i = 0; i < 3; i++)
        {
            var client = new WebSocketClient();
            var name = $"c{i}";
            client.Headers["x-name"] = name;
            client.OnData((data, _) => received.Add($"{name}:{Encoding.UTF8.GetString(data.GetBytes())}"));
            client.OnEvent((e, _, _) => received.Add($"{name}:event:{e}"));
            await client.OpenAsync(uri);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Count == 3, what: "all accepted");

        server.BroadcastData(Encoding.UTF8.GetBytes("all"), WebSocketDelivery.Text);
        server.BroadcastEvent("news", new byte[] { 1 }, WebSocketDelivery.Binary,
            c => c.Request.Headers["x-name"] == "c1");

        await Wait.Until(() => received.Count == 4, what: "broadcast delivered");
        Assert.Contains("c1:event:news", received);
        Assert.DoesNotContain("c0:event:news", received);
        Assert.DoesNotContain("c2:event:news", received);

        foreach (var c in clients) await c.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task ServerClose_ClosesClients()
    {
        var (server, uri) = await Start();
        var client = new WebSocketClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(uri);
        await Wait.Until(() => server.Clients.Count == 1, what: "server accept");

        await server.CloseAsync();

        await Wait.For(closed, what: "client close after server close");
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task SendOverloads_Stream_Segments_Offset()
    {
        var (server, uri) = await Start(c => c.OnData((data, type) => c.SendData(data.GetBytes(), type)));
        var client = new WebSocketClient();
        var received = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        client.OnData((data, _) => received.Enqueue(data.GetBytes()));
        await client.OpenAsync(uri);

        var bytes = Net.Payload(100);
        var stream = new MemoryStream(bytes) { Position = 90 };

        client.SendData(bytes, 10, 20);
        await Wait.Until(() => received.Count == 1, what: "first echo");
        client.SendData(new ArraySegment<byte>(bytes, 30, 5));
        await Wait.Until(() => received.Count == 2, what: "second echo");
        client.SendData(new List<ArraySegment<byte>> { new(bytes, 0, 3), new(bytes, 50, 4) });
        await Wait.Until(() => received.Count == 3, what: "third echo");
        client.SendData(stream);
        await Wait.Until(() => received.Count == 4, what: "fourth echo");

        var all = received.ToArray();
        Assert.Equal(bytes.AsSpan(10, 20).ToArray(), all[0]);
        Assert.Equal(bytes.AsSpan(30, 5).ToArray(), all[1]);
        Assert.Equal(bytes.AsSpan(0, 3).ToArray().Concat(bytes.AsSpan(50, 4).ToArray()).ToArray(), all[2]);
        Assert.Equal(bytes.AsSpan(90, 10).ToArray(), all[3]);
        Assert.Equal(90, stream.Position);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task Send_InvalidArguments_Throw()
    {
        var client = new WebSocketClient();

        Assert.Throws<ArgumentNullException>(() => client.SendData((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => client.SendEvent("name", (byte[])null!));
        Assert.Throws<ArgumentException>(() => client.SendEvent("", new byte[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(new byte[4], 3, 4));
        Assert.Throws<ArgumentNullException>(() => client.Open(null!));

        // closed client ignores the send
        client.SendData(new byte[4]);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task EventWithoutContent_IsDelivered()
    {
        var (server, uri) = await Start(c => c.OnEvent((name, data, type) => c.SendEvent(name, data.GetBytes(), type)));
        var client = new WebSocketClient();
        var received = Wait.Signal<(string name, long length)>();
        client.OnEvent((name, data, _) => received.TrySetResult((name, data.Length)));
        await client.OpenAsync(uri);

        client.SendEvent("empty");

        Assert.Equal(("empty", 0L), await Wait.For(received, what: "empty event echo"));

        await client.CloseAsync();
        await server.CloseAsync();
    }
}
