using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpTests
{
    private static async Task<(RUDP.Server server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        server.On.Accept(client =>
        {
            client.On.Data((stream, type) => client.To.Data(stream.GetBytes(), type));
            client.On.Event((name, stream, type) => client.To.Event(name, stream.GetBytes(), type));
        });
        await server.To.Open(host);
        return (server, host);
    }

    [Fact]
    public void DefaultConfig()
    {
        var server = new RUDP.Server();
        Assert.NotEmpty(server.Id);
        Assert.False(server.IsOpened);
        Assert.Equal(5000, server.HandshakeTimeout);
        Assert.Equal(5000, server.NoResponseTimeout);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
        Assert.Empty(server.Clients);

        var client = new RUDP.Client();
        Assert.False(client.IsOpened);
        Assert.Equal(5000, client.HandshakeTimeout);
        Assert.Equal(5000, client.NoResponseTimeout);
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
    }

    [Fact]
    public Task ServerOpenAndClose() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        int open = 0, close = 0, modify = 0, error = 0;
        server.On.Open(() => Interlocked.Increment(ref open));
        server.On.Close(() => Interlocked.Increment(ref close));
        server.On.Modify(_ => Interlocked.Increment(ref modify));
        server.On.Error(_ => Interlocked.Increment(ref error));

        await server.To.Open(host);

        Assert.True(server.IsOpened);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await server.To.Close();

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    });

    [Fact]
    public Task ServerCanReopenAfterClose() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();

        for (var i = 0; i < 3; i++)
        {
            await server.To.Open(host);
            Assert.True(server.IsOpened);
            await server.To.Close();
            Assert.False(server.IsOpened);
        }
    });

    [Fact]
    public Task ClientHandshakeAndClose() => Guard.Run(async () =>
    {
        var accepted = Wait.Signal<global::Netly.Interfaces.IRUDP.Client>();
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        server.On.Accept(c => accepted.TrySetResult(c));
        await server.To.Open(host);

        var client = new RUDP.Client();
        int open = 0, close = 0, modify = 0, error = 0;
        client.On.Open(() => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Modify(_ => Interlocked.Increment(ref modify));
        client.On.Error(_ => Interlocked.Increment(ref error));

        await client.To.Open(host);

        var peer = await Wait.For(accepted, what: "server accept");
        Assert.True(client.IsOpened);
        Assert.NotEmpty(client.Id);
        Assert.NotEmpty(peer.Id);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);

        await Wait.Until(() => !peer.IsOpened, TimeSpan.FromSeconds(15), "server session closed");
        await server.To.Close();
    });

    [Fact]
    public Task ClientToUnreachableServerRaisesError() => Guard.Run(async () =>
    {
        var client = new RUDP.Client { HandshakeTimeout = 1000 };
        var failed = Wait.Signal<bool>();
        var opened = false;
        client.On.Open(() => opened = true);
        client.On.Error(_ => failed.TrySetResult(true));

        await client.To.Open(Net.UdpHost());

        await Wait.For(failed, TimeSpan.FromSeconds(15), "handshake error");
        Assert.False(opened);
        Assert.False(client.IsOpened);
    });

    [Theory]
    [InlineData(RUDP.MessageType.Reliable)]
    [InlineData(RUDP.MessageType.Sequenced)]
    [InlineData(RUDP.MessageType.Unreliable)]
    public Task EchoData(RUDP.MessageType type) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = Wait.Signal<(byte[] data, RUDP.MessageType type)>();
        client.On.Data((stream, t) => received.TrySetResult((stream.GetBytes(), t)));
        await client.To.Open(host);

        var payload = Net.Payload(512);
        client.To.Data(payload, type);

        var (data, receivedType) = await Wait.For(received, what: $"{type} echo");
        Assert.Equal(payload, data);
        Assert.Equal(type, receivedType);

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task EchoEvent() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.On.Event((name, stream, _) => received.TrySetResult((name, stream.GetBytes())));
        await client.To.Open(host);

        var payload = Net.Payload(200, 4);
        client.To.Event("rudp.event", payload, RUDP.MessageType.Reliable);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("rudp.event", name);
        Assert.Equal(payload, data);

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task StringOverloadsUseUtf8ByDefault() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = Wait.Signal<string>();
        client.On.Data((stream, _) => received.TrySetResult(Encoding.UTF8.GetString(stream.GetBytes())));
        await client.To.Open(host);

        client.To.Data("olá ✓ rudp", RUDP.MessageType.Reliable);

        Assert.Equal("olá ✓ rudp", await Wait.For(received));

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task ReliableMessagesKeepOrderAndArriveOnce() => Guard.Run(() => ReliableOrder(2));

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    public Task ReliableManyMessagesKeepOrder(int total) => Guard.Run(() => ReliableOrder(total));

    private static async Task ReliableOrder(int total)
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = new List<string>();
        client.On.Data((stream, _) =>
        {
            lock (received) received.Add(Encoding.UTF8.GetString(stream.GetBytes()));
        });
        await client.To.Open(host);

        for (var i = 0; i < total; i++) client.To.Data(i.ToString(), RUDP.MessageType.Reliable);

        await Wait.Until(() => { lock (received) return received.Count >= total; }, TimeSpan.FromSeconds(20), "all reliable messages");
        await Task.Delay(500);

        lock (received) Assert.Equal(Enumerable.Range(0, total).Select(i => i.ToString()), received);

        await client.To.Close();
        await server.To.Close();
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(8 * 1024)]
    public Task ReliablePayload(int size) => Guard.Run(() => LargePayload(size));

    [Fact]
    public Task LargeReliablePayload() => Guard.Run(() => LargePayload(64 * 1024));

    private static async Task LargePayload(int size)
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Data((stream, _) => received.TrySetResult(stream.GetBytes()));
        await client.To.Open(host);

        var payload = Net.Payload(size);
        client.To.Data(payload, RUDP.MessageType.Reliable);

        Assert.Equal(payload, await Wait.For(received, TimeSpan.FromSeconds(20), "large echo"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public Task ServerAcceptsTwoClients() => Guard.Run(() => AcceptClients(2));

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public Task ServerAcceptsManyClients(int total) => Guard.Run(() => AcceptClients(total));

    private static async Task AcceptClients(int total)
    {
        var (server, host) = await EchoServer();
        var clients = Enumerable.Range(0, total).Select(_ => new RUDP.Client()).ToList();

        await Task.WhenAll(clients.Select(c => c.To.Open(host)));

        await Wait.Until(() => server.Clients.Length == total, what: "all accepts");
        Assert.All(clients, c => Assert.True(c.IsOpened));
        Assert.Equal(total, clients.Select(c => c.Id).Distinct().Count());

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }

    [Fact]
    public Task BroadcastReachesEveryClient() => Guard.Run(async () =>
    {
        const int total = 5;
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        await server.To.Open(host);

        var count = 0;
        var clients = new List<RUDP.Client>();

        for (var i = 0; i < total; i++)
        {
            var client = new RUDP.Client();
            client.On.Data((stream, _) =>
            {
                if (Encoding.UTF8.GetString(stream.GetBytes()) == "broadcast") Interlocked.Increment(ref count);
            });
            await client.To.Open(host);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Length == total, what: "server accepted all");

        server.To.DataBroadcast("broadcast", RUDP.MessageType.Reliable);

        await Wait.Until(() => count == total, TimeSpan.FromSeconds(15), "broadcast delivered");

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    });

    [Fact]
    public Task ClientObservesServerClose() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));
        await client.To.Open(host);

        await server.To.Close();

        await Wait.For(closed, TimeSpan.FromSeconds(20), "client close after server close");
        Assert.False(client.IsOpened);
    });
}
