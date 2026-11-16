using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Udp;

public class UdpTests
{
    // UDP has no handshake: the server only knows a client after its first datagram.
    private static readonly byte[] Hello = Encoding.ASCII.GetBytes("netly-udp-hello");

    private static bool IsHello(byte[] data) => data.AsSpan().SequenceEqual(Hello);

    private static async Task<(UDP.Server server, NHost host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new UDP.Server();
        server.On.Accept(client =>
        {
            client.On.Data(s =>
            {
                var data = s.GetBytes();
                if (!IsHello(data)) client.To.Data(data);
            });
            client.On.Event((name, s) => client.To.Event(name, s.GetBytes()));
        });
        await server.To.Open(host);
        return (server, host);
    }

    [Fact]
    public void DefaultConfig()
    {
        var client = new UDP.Client();
        Assert.False(client.IsOpened);
        Assert.False(string.IsNullOrWhiteSpace(client.Id));
        Assert.NotNull(client.Host);
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);

        var server = new UDP.Server();
        Assert.False(server.IsOpened);
        Assert.False(string.IsNullOrWhiteSpace(server.Id));
        Assert.NotNull(server.Host);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
        Assert.Empty(server.Clients);
    }

    [Fact]
    public async Task ServerOpenAndClose()
    {
        var host = Net.UdpHost();
        var server = new UDP.Server();
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
    }

    [Fact]
    public async Task ServerOpenOnUnassignableAddressFails()
    {
        var server = new UDP.Server();
        Exception? failure = null;
        server.On.Error(e => failure = e);

        await server.To.Open(new NHost("1.1.1.1", 0));

        Assert.False(server.IsOpened);
        Assert.NotNull(failure);
    }

    [Fact]
    public async Task ClientOpenAndClose()
    {
        var (server, host) = await EchoServer();
        var client = new UDP.Client();
        int open = 0, close = 0, modify = 0, error = 0;
        client.On.Open(() => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Modify(_ => Interlocked.Increment(ref modify));
        client.On.Error(_ => Interlocked.Increment(ref error));

        await client.To.Open(host);

        Assert.True(client.IsOpened);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);

        await server.To.Close();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(36)]
    [InlineData(1024)]
    [InlineData(8 * 1024)]
    public async Task EchoData(int size)
    {
        var (server, host) = await EchoServer();
        var client = new UDP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Data(s => received.TrySetResult(s.GetBytes()));
        await client.To.Open(host);
        client.To.Data(Hello);
        await Wait.Until(() => server.Clients.Length == 1, what: "server accept");

        var payload = Net.Payload(size);
        client.To.Data(payload);

        Assert.Equal(payload, await Wait.For(received, what: $"echo of {size} bytes"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, host) = await EchoServer();
        var client = new UDP.Client();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.On.Event((name, s) => received.TrySetResult((name, s.GetBytes())));
        await client.To.Open(host);
        client.To.Data(Hello);
        await Wait.Until(() => server.Clients.Length == 1, what: "server accept");

        var payload = Net.Payload(200, 5);
        client.To.Event("ping", payload);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("ping", name);
        Assert.Equal(payload, data);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task StringOverloadsUseUtf8ByDefault()
    {
        var (server, host) = await EchoServer();
        var client = new UDP.Client();
        var received = Wait.Signal<string>();
        client.On.Data(s => received.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));
        await client.To.Open(host);
        client.To.Data(Hello);
        await Wait.Until(() => server.Clients.Length == 1, what: "server accept");

        client.To.Data("olá ✓");

        Assert.Equal("olá ✓", await Wait.For(received));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ServerAcceptsManyClients()
    {
        const int total = 20;
        var (server, host) = await EchoServer();
        var clients = Enumerable.Range(0, total).Select(_ => new UDP.Client()).ToList();

        await Task.WhenAll(clients.Select(c => c.To.Open(host)));
        foreach (var c in clients) c.To.Data(Hello);
        await Wait.Until(() => server.Clients.Length == total, what: "all accepts");

        Assert.Equal(total, server.Clients.Select(c => c.Id).Distinct().Count());

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }

    [Fact]
    public async Task BroadcastReachesEveryClient()
    {
        const int total = 10;
        var host = Net.UdpHost();
        var server = new UDP.Server();
        await server.To.Open(host);

        int dataCount = 0, eventCount = 0;
        var clients = new List<UDP.Client>();

        for (var i = 0; i < total; i++)
        {
            var client = new UDP.Client();
            client.On.Data(s =>
            {
                if (Encoding.UTF8.GetString(s.GetBytes()) == "broadcast") Interlocked.Increment(ref dataCount);
            });
            client.On.Event((name, _) =>
            {
                if (name == "news") Interlocked.Increment(ref eventCount);
            });
            await client.To.Open(host);
            client.To.Data(Hello);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Length == total, what: "server accepted all");

        server.To.DataBroadcast("broadcast");
        server.To.EventBroadcast("news", "payload");

        await Wait.Until(() => dataCount == total && eventCount == total, what: "broadcast delivered");

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }

    [Fact]
    public async Task ServerSendsToSpecificClient()
    {
        var host = Net.UdpHost();
        var server = new UDP.Server();
        var peers = new System.Collections.Concurrent.ConcurrentDictionary<string, NHost>();
        server.On.Accept(c => c.On.Data(s =>
        {
            var text = Encoding.UTF8.GetString(s.GetBytes());
            if (text.StartsWith("i-am-")) peers[text] = c.Host;
        }));
        await server.To.Open(host);

        var a = new UDP.Client();
        var b = new UDP.Client();
        var receivedA = Wait.Signal<string>();
        var receivedB = 0;
        a.On.Data(s => receivedA.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));
        b.On.Data(_ => Interlocked.Increment(ref receivedB));

        await a.To.Open(host);
        await b.To.Open(host);
        a.To.Data("i-am-a");
        b.To.Data("i-am-b");
        await Wait.Until(() => peers.Count == 2, what: "server identified both clients");

        server.To.Data(peers["i-am-a"], "only-a");

        Assert.Equal("only-a", await Wait.For(receivedA, what: "targeted delivery"));
        await Wait.Never(() => receivedB > 0, TimeSpan.FromMilliseconds(300), "delivery to other client");

        await a.To.Close();
        await b.To.Close();
        await server.To.Close();
    }
}
