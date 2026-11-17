using TcpClient = Netly.TcpClient;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpLifecycleTests
{
    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    private static async Task<bool> Open(TcpServer server, Host host)
    {
        var result = Wait.Signal<bool>();
        server.OnOpen(() => result.TrySetResult(true));
        server.OnError(_ => result.TrySetResult(false));
        server.Open(host);
        return await Wait.For(result, what: "server open result");
    }

    private static async Task<bool> Open(TcpClient client, Host host)
    {
        var result = Wait.Signal<bool>();
        client.OnOpen(() => result.TrySetResult(true));
        client.OnError(_ => result.TrySetResult(false));
        client.Open(host);
        return await Wait.For(result, what: "client open result");
    }

    private static async Task Close(TcpServer server)
    {
        if (!server.IsOpened) return;

        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();
        await Wait.For(closed, what: "server close");
    }

    private static async Task Close(TcpClient client)
    {
        if (!client.IsOpened) return;

        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.Close();
        await Wait.For(closed, what: "client close");
    }

    [Fact]
    public void DefaultConfig()
    {
        var client = new TcpClient();
        Assert.False(client.IsOpened);
        Assert.True(client.IsFraming);
        Assert.False(client.IsSecure);
        Assert.False(client.IsServer);
        Assert.False(string.IsNullOrWhiteSpace(client.Id));
        Assert.NotNull(client.Host);

        var server = new TcpServer();
        Assert.False(server.IsOpened);
        Assert.True(server.IsFraming);
        Assert.False(server.IsSecure);
        Assert.False(string.IsNullOrWhiteSpace(server.Id));
        Assert.NotNull(server.Host);
        Assert.Empty(server.Clients);
    }

    [Fact]
    public void FramingCanBeDisabled()
    {
        Assert.False(new TcpClient(false).IsFraming);
        Assert.False(new TcpServer(false).IsFraming);
    }

    [Fact]
    public void IdsAreUnique()
    {
        Assert.NotEqual(new TcpClient().Id, new TcpClient().Id);
        Assert.NotEqual(new TcpServer().Id, new TcpServer().Id);
    }

    [Fact]
    public async Task ServerOpenAndClose()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        int open = 0, close = 0, modify = 0, error = 0;

        server.OnOpen(() => Interlocked.Increment(ref open));
        server.OnClose(() => Interlocked.Increment(ref close));
        server.OnModify(_ => Interlocked.Increment(ref modify));
        server.OnError(_ => Interlocked.Increment(ref error));

        Assert.True(await Open(server, host));
        Assert.True(server.IsOpened);
        Assert.Equal(1, open);
        Assert.Equal(1, modify);
        Assert.Equal(0, close);
        Assert.Equal(0, error);
        Assert.Equal(host.Port, server.Host.Port);

        await Close(server);

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    }

    [Fact]
    public async Task ServerOpenOnUnassignableAddressFails()
    {
        var server = new TcpServer();

        Assert.False(await Open(server, new Host("1.1.1.1", 0)));
        Assert.False(server.IsOpened);
    }

    [Fact]
    public async Task ClientOpenAndClose()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        Assert.True(await Open(server, host));

        var client = new TcpClient();
        int open = 0, close = 0, modify = 0, error = 0;
        client.OnOpen(() => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnModify(_ => Interlocked.Increment(ref modify));
        client.OnError(_ => Interlocked.Increment(ref error));

        Assert.True(await Open(client, host));
        await Wait.Until(() => open == 1, what: "OnOpen callback");

        Assert.True(client.IsOpened);
        Assert.Equal(1, open);
        Assert.Equal(1, modify);
        Assert.Equal(0, error);
        Assert.Equal(host.Port, client.Host.Port);

        await Close(client);

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);

        await Close(server);
    }

    [Fact]
    public async Task ClientObservesServerClose()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        var client = new TcpClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));

        Assert.True(await Open(server, host));
        Assert.True(await Open(client, host));
        await Close(server);

        await Wait.For(closed, what: "client OnClose after server close");
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task ServerCloseDoesNotLeakClientsStillBeingAccepted()
    {
        const int rounds = 30, total = 5;

        for (var round = 0; round < rounds; round++)
        {
            var host = Net.TcpHost();
            var server = new TcpServer();
            var closed = 0;
            Assert.True(await Open(server, host));

            var clients = new List<TcpClient>();

            for (var i = 0; i < total; i++)
            {
                var client = new TcpClient();
                client.OnClose(() => Interlocked.Increment(ref closed));
                clients.Add(client);
            }

            await Task.WhenAll(clients.Select(c => Open(c, host)));

            // close without waiting accept, connections can be in flight
            server.Close();

            await Wait.Until(() => closed == total, what: $"all clients closed (round {round})");
        }
    }

    [Fact]
    public async Task IsOpenedDoesNotBlock()
    {
        const int total = 200;
        var host = Net.TcpHost();
        var server = new TcpServer();
        Assert.True(await Open(server, host));

        var clients = Enumerable.Range(0, total).Select(_ => new TcpClient()).ToList();
        foreach (var c in clients) c.Open(host);

        await Wait.Until(() => clients.All(c => c.IsOpened) && server.Clients.Count == total, what: "all connected");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var opened = clients.Count(c => c.IsOpened) + server.Clients.Count(c => c.IsOpened);
        watch.Stop();

        Assert.Equal(total * 2, opened);
        Assert.True(watch.ElapsedMilliseconds < 200, $"IsOpened took {watch.ElapsedMilliseconds} ms");

        foreach (var c in clients) c.Close();
        await Close(server);
    }

    [Fact]
    public async Task ServerObservesClientClose()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        var accepted = Wait.Signal<TcpClient>();
        var closed = Wait.Signal<bool>();

        server.OnAccept(c =>
        {
            c.OnClose(() => closed.TrySetResult(true));
            accepted.TrySetResult(c);
        });

        Assert.True(await Open(server, host));

        var client = new TcpClient();
        Assert.True(await Open(client, host));
        await Wait.For(accepted, what: "server accept");
        Assert.Single(server.Clients);

        await Close(client);
        await Wait.For(closed, what: "server-side client OnClose");
        await Wait.Until(() => server.Clients.Count == 0, what: "server clients cleanup");

        await Close(server);
    }

    [Fact]
    public async Task FramingKeepsMessageBoundaries()
    {
        const int total = 200;
        var host = Net.TcpHost();
        var server = new TcpServer();
        server.OnAccept(c => c.OnData(s => c.SendData(Read(s))));
        Assert.True(await Open(server, host));

        var client = new TcpClient();
        var messages = new List<byte[]>();
        client.OnData(s =>
        {
            lock (messages) messages.Add(Read(s));
        });
        Assert.True(await Open(client, host));

        var sent = Enumerable.Range(1, total).Select(i => Net.Payload(i, i)).ToList();
        foreach (var message in sent) client.SendData(message);

        await Wait.Until(() => { lock (messages) return messages.Count == total; }, what: "all framed messages");

        // order and boundaries preserved
        lock (messages)
        {
            for (var i = 0; i < total; i++) Assert.Equal(sent[i], messages[i]);
        }

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ServerCanReopenAfterClose()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        var open = 0;
        server.OnOpen(() => Interlocked.Increment(ref open));

        for (var i = 0; i < 3; i++)
        {
            Assert.True(await Open(server, host));
            Assert.True(server.IsOpened);
            await Close(server);
            Assert.False(server.IsOpened);
        }

        Assert.Equal(3, open);
    }
}
