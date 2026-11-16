using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpTests
{
    private static async Task<(RudpServer server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(client =>
        {
            client.OnData((stream, type) => client.SendData(stream.GetBytes(), type));
            client.OnEvent((name, stream, type) => client.SendEvent(name, stream.GetBytes(), type));
        });
        await server.OpenAsync(host);
        return (server, host);
    }

    [Fact]
    public void DefaultConfig()
    {
        var server = new RudpServer();
        Assert.NotEmpty(server.Id);
        Assert.False(server.IsOpened);
        Assert.Equal(5000, server.HandshakeTimeout);
        Assert.Equal(5000, server.NoResponseTimeout);
        Assert.Empty(server.Clients);

        var client = new RudpClient();
        Assert.False(client.IsOpened);
        Assert.Equal(5000, client.HandshakeTimeout);
        Assert.Equal(5000, client.NoResponseTimeout);
    }

    [Fact]
    public Task ServerOpenAndClose() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        int open = 0, close = 0, modify = 0, error = 0;
        server.OnOpen(() => Interlocked.Increment(ref open));
        server.OnClose(() => Interlocked.Increment(ref close));
        server.OnModify(_ => Interlocked.Increment(ref modify));
        server.OnError(_ => Interlocked.Increment(ref error));

        await server.OpenAsync(host);

        Assert.True(server.IsOpened);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await server.CloseAsync();

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    });

    [Fact]
    public Task ServerCanReopenAfterClose() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();

        for (var i = 0; i < 3; i++)
        {
            await server.OpenAsync(host);
            Assert.True(server.IsOpened);
            await server.CloseAsync();
            Assert.False(server.IsOpened);
        }
    });

    [Fact]
    public Task ClientHandshakeAndClose() => Guard.Run(async () =>
    {
        var accepted = Wait.Signal<RudpClient>();
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(c => accepted.TrySetResult(c));
        await server.OpenAsync(host);

        var client = new RudpClient();
        int open = 0, close = 0, modify = 0, error = 0;
        client.OnOpen(() => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnModify(_ => Interlocked.Increment(ref modify));
        client.OnError(_ => Interlocked.Increment(ref error));

        await client.OpenAsync(host);

        var peer = await Wait.For(accepted, what: "server accept");
        Assert.True(client.IsOpened);
        Assert.NotEmpty(client.Id);
        Assert.NotEmpty(peer.Id);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await client.CloseAsync();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);

        await Wait.Until(() => !peer.IsOpened, TimeSpan.FromSeconds(15), "server session closed");
        await server.CloseAsync();
    });

    [Fact]
    public Task ClientToUnreachableServerRaisesError() => Guard.Run(async () =>
    {
        var client = new RudpClient { HandshakeTimeout = 1000 };
        var failed = Wait.Signal<bool>();
        var opened = false;
        client.OnOpen(() => opened = true);
        client.OnError(_ => failed.TrySetResult(true));

        await client.OpenAsync(Net.UdpHost());

        await Wait.For(failed, TimeSpan.FromSeconds(15), "handshake error");
        Assert.False(opened);
        Assert.False(client.IsOpened);
    });

    [Theory]
    [InlineData(RudpDelivery.Reliable)]
    [InlineData(RudpDelivery.Sequenced)]
    [InlineData(RudpDelivery.Unreliable)]
    public Task EchoData(RudpDelivery type) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = Wait.Signal<(byte[] data, RudpDelivery type)>();
        client.OnData((stream, t) => received.TrySetResult((stream.GetBytes(), t)));
        await client.OpenAsync(host);

        var payload = Net.Payload(512);
        client.SendData(payload, type);

        var (data, receivedType) = await Wait.For(received, what: $"{type} echo");
        Assert.Equal(payload, data);
        Assert.Equal(type, receivedType);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task EchoEvent() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.OnEvent((name, stream, _) => received.TrySetResult((name, stream.GetBytes())));
        await client.OpenAsync(host);

        var payload = Net.Payload(200, 4);
        client.SendEvent("rudp.event", payload, RudpDelivery.Reliable);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("rudp.event", name);
        Assert.Equal(payload, data);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task StringOverloadsUseUtf8ByDefault() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = Wait.Signal<string>();
        client.OnData((stream, _) => received.TrySetResult(Encoding.UTF8.GetString(stream.GetBytes())));
        await client.OpenAsync(host);

        client.SendData("olá ✓ rudp", RudpDelivery.Reliable);

        Assert.Equal("olá ✓ rudp", await Wait.For(received));

        await client.CloseAsync();
        await server.CloseAsync();
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
        var client = new RudpClient();
        var received = new List<string>();
        client.OnData((stream, _) =>
        {
            lock (received) received.Add(Encoding.UTF8.GetString(stream.GetBytes()));
        });
        await client.OpenAsync(host);

        for (var i = 0; i < total; i++) client.SendData(i.ToString(), RudpDelivery.Reliable);

        await Wait.Until(() => { lock (received) return received.Count >= total; }, TimeSpan.FromSeconds(20), "all reliable messages");
        await Task.Delay(500);

        lock (received) Assert.Equal(Enumerable.Range(0, total).Select(i => i.ToString()), received);

        await client.CloseAsync();
        await server.CloseAsync();
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
        var client = new RudpClient();
        var received = Wait.Signal<byte[]>();
        client.OnData((stream, _) => received.TrySetResult(stream.GetBytes()));
        await client.OpenAsync(host);

        var payload = Net.Payload(size);
        client.SendData(payload, RudpDelivery.Reliable);

        Assert.Equal(payload, await Wait.For(received, TimeSpan.FromSeconds(20), "large echo"));

        await client.CloseAsync();
        await server.CloseAsync();
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
        var clients = Enumerable.Range(0, total).Select(_ => new RudpClient()).ToList();

        await Task.WhenAll(clients.Select(c => c.OpenAsync(host)));

        await Wait.Until(() => server.Clients.Count == total, what: "all accepts");
        Assert.All(clients, c => Assert.True(c.IsOpened));
        Assert.Equal(total, clients.Select(c => c.Id).Distinct().Count());

        await Task.WhenAll(clients.Select(c => c.CloseAsync()));
        await server.CloseAsync();
    }

    [Fact]
    public Task BroadcastReachesEveryClient() => Guard.Run(async () =>
    {
        const int total = 5;
        var host = Net.UdpHost();
        var server = new RudpServer();
        await server.OpenAsync(host);

        var count = 0;
        var clients = new List<RudpClient>();

        for (var i = 0; i < total; i++)
        {
            var client = new RudpClient();
            client.OnData((stream, _) =>
            {
                if (Encoding.UTF8.GetString(stream.GetBytes()) == "broadcast") Interlocked.Increment(ref count);
            });
            await client.OpenAsync(host);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Count == total, what: "server accepted all");

        server.BroadcastData("broadcast", RudpDelivery.Reliable);

        await Wait.Until(() => count == total, TimeSpan.FromSeconds(15), "broadcast delivered");

        await Task.WhenAll(clients.Select(c => c.CloseAsync()));
        await server.CloseAsync();
    });

    [Fact]
    public Task ClientObservesServerClose() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(host);

        await server.CloseAsync();

        await Wait.For(closed, TimeSpan.FromSeconds(20), "client close after server close");
        Assert.False(client.IsOpened);
    });
}
