using System.Net;
using System.Net.Sockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpRobustnessTests
{
    private static async Task<(RUDP.Server server, NHost host)> EchoServer(int noResponse = 5000)
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server { NoResponseTimeout = noResponse };
        server.On.Accept(client => client.On.Data((stream, type) => client.To.Data(stream.GetBytes(), type)));
        await server.To.Open(host);
        return (server, host);
    }

    private static async Task AssertServerStillWorks(NHost host)
    {
        var client = new RUDP.Client();
        var echoed = Wait.Signal<string>();
        Exception? error = null;
        client.On.Error(e => error = e);
        client.On.Data((stream, _) => echoed.TrySetResult(Encoding.UTF8.GetString(stream.GetBytes())));
        await client.To.Open(host);

        Assert.True(client.IsOpened, $"open failed: {error?.Message}");

        client.To.Data("still alive", RUDP.MessageType.Reliable);

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after attack"));
        await client.To.Close();
    }

    [Fact]
    public Task ClientClosesWhenServerStopsAnswering() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host);
        var client = new RUDP.Client { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));
        await client.To.Open(proxy.Host);

        proxy.Blackhole = true;

        await Wait.For(closed, TimeSpan.FromSeconds(10), "close by timeout");
        Assert.False(client.IsOpened);

        await server.To.Close();
    });

    [Fact]
    public Task ServerSessionClosesWhenClientDisappears() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        server.On.Accept(c => c.On.Close(() => closed.TrySetResult(true)));
        await server.To.Open(host);

        using var proxy = new UdpProxy(host);
        var client = new RUDP.Client();
        await client.To.Open(proxy.Host);
        await Wait.Until(() => server.Clients.Length == 1, what: "accept");

        proxy.Blackhole = true;

        await Wait.For(closed, TimeSpan.FromSeconds(10), "server session timeout");
        await Wait.Until(() => server.Clients.Length == 0, what: "session removed");

        await server.To.Close();
    });

    [Fact]
    public Task IdleConnectionStaysOpen() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer(1500);
        var client = new RUDP.Client { NoResponseTimeout = 1500 };
        await client.To.Open(host);

        // idle for more than twice the no response timeout, keep alive packets must hold the connection
        await Task.Delay(3500);

        Assert.True(client.IsOpened);
        Assert.Single(server.Clients);

        var echoed = Wait.Signal<bool>();
        client.On.Data((_, _) => echoed.TrySetResult(true));
        client.To.Data("after idle", RUDP.MessageType.Reliable);
        await Wait.For(echoed, what: "echo after idle");

        await client.To.Close();
        await server.To.Close();
    }, TimeSpan.FromSeconds(30));

    [Fact]
    public Task IdleTrafficIsSmall() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host);
        var client = new RUDP.Client();
        await client.To.Open(proxy.Host);
        await Task.Delay(100);
        var before = proxy.ToServerPackets + proxy.ToClientPackets;

        await Task.Delay(3000);

        // keep alive, not a ping every 100ms: at most 2 packets by second in each direction
        var idle = proxy.ToServerPackets + proxy.ToClientPackets - before;
        Assert.True(idle <= 12, $"{idle} packets in 3s of idle");

        await client.To.Close();
        await server.To.Close();
    }, TimeSpan.FromSeconds(30));

    [Fact]
    public Task ClosedClientCanOpenAgain() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var open = 0;
        client.On.Open(() => Interlocked.Increment(ref open));

        for (var i = 0; i < 5; i++)
        {
            await client.To.Open(host);
            Assert.True(client.IsOpened, $"round {i}");
            await client.To.Close();
            Assert.False(client.IsOpened);
        }

        Assert.Equal(5, open);
        await server.To.Close();
    });

    [Fact]
    public Task SendOnClosedClientIsIgnored() => Guard.Run(async () =>
    {
        var client = new RUDP.Client();
        var error = false;
        client.On.Error(_ => error = true);

        client.To.Data("ignored", RUDP.MessageType.Reliable);
        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.False(error);
    });

    [Fact]
    public Task GarbageDatagramsDoNotBreakServer() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var random = new Random(21);

        for (var i = 1; i < 400; i++)
        {
            var garbage = new byte[i];
            random.NextBytes(garbage);
            raw.SendTo(garbage, host.EndPoint);
        }

        await AssertServerStillWorks(host);
        Assert.Empty(server.Clients.Where(c => c == null));

        await server.To.Close();
    });

    [Theory]
    [InlineData(new byte[] { 6, 0 })] // handshake, truncated syn
    [InlineData(new byte[] { 6, 0, 99, 1, 2, 3, 4 })] // wrong version
    [InlineData(new byte[] { 6, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 })] // syn ack to a server
    [InlineData(new byte[] { 6, 2, 1, 2, 3, 4 })] // handshake ack without connection
    [InlineData(new byte[] { 5, 1, 2, 3, 4 })] // close without connection
    [InlineData(new byte[] { 2, 1, 2, 3, 4, 5, 6, 7, 8 })] // reliable without connection
    [InlineData(new byte[] { 0 })]
    [InlineData(new byte[] { 255, 255, 255 })]
    public Task InvalidFirstPacketCreatesNoConnection(byte[] packet) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        raw.SendTo(packet, host.EndPoint);
        await Task.Delay(100);

        Assert.Empty(server.Clients);
        await AssertServerStillWorks(host);

        await server.To.Close();
    });

    [Fact]
    public Task ForgedPacketsCannotCloseOrInjectIntoASession() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        var injected = 0;
        server.On.Accept(c => c.On.Data((_, _) => Interlocked.Increment(ref injected)));
        await server.To.Open(host);

        // the proxy gives the client a known source endpoint, the attacker uses the same one
        var client = new RUDP.Client();
        await client.To.Open(host);
        await Wait.Until(() => server.Clients.Length == 1, what: "accept");

        // attacker does not know the token: guess packets from another socket (other endpoint) and blind tokens
        using var attacker = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var random = new Random(30);

        for (var i = 0; i < 200; i++)
        {
            var packet = new byte[40];
            random.NextBytes(packet);
            packet[0] = (byte)(i % 2 == 0 ? 5 : 2); // close or reliable data
            attacker.SendTo(packet, host.EndPoint);
        }

        await Task.Delay(300);

        Assert.True(client.IsOpened);
        Assert.Single(server.Clients);
        Assert.Equal(0, injected);

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task SynFloodIsCleanedUp() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server { HandshakeTimeout = 1000 };
        server.On.Accept(client => client.On.Data((stream, type) => client.To.Data(stream.GetBytes(), type)));
        await server.To.Open(host);

        // many syn from different sources that never complete the handshake
        for (var i = 0; i < 200; i++)
        {
            using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var syn = new byte[7];
            syn[0] = 6;
            syn[1] = 0;
            syn[2] = 1;
            BitConverter.GetBytes(i).CopyTo(syn, 3);
            raw.SendTo(syn, host.EndPoint);
        }

        Assert.Empty(server.Clients);
        await AssertServerStillWorks(host);

        await server.To.Close();
    });

    [Fact]
    public Task ServerCloseNotifiesClientsFast() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));
        await client.To.Open(host);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        await server.To.Close();
        await Wait.For(closed, what: "client close");

        Assert.True(watch.ElapsedMilliseconds < 1000, $"close notice took {watch.ElapsedMilliseconds} ms");
    });

    [Fact]
    public Task ManyClientsEchoIndependently() => Guard.Run(async () =>
    {
        const int total = 100;
        var (server, host) = await EchoServer();
        var echoed = 0;
        var clients = new List<RUDP.Client>();

        for (var i = 0; i < total; i++)
        {
            var client = new RUDP.Client();
            var expected = $"client-{i}";
            client.On.Data((stream, _) =>
            {
                if (Encoding.UTF8.GetString(stream.GetBytes()) == expected) Interlocked.Increment(ref echoed);
            });
            clients.Add(client);
        }

        await Task.WhenAll(clients.Select(c => c.To.Open(host)));
        Assert.All(clients, c => Assert.True(c.IsOpened));

        for (var i = 0; i < total; i++) clients[i].To.Data($"client-{i}", RUDP.MessageType.Reliable);

        await Wait.Until(() => echoed == total, TimeSpan.FromSeconds(20), "all echoes");
        Assert.Equal(total, server.Clients.Length);

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    });

    [Fact]
    public Task SessionIdsAreUnique() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var clients = Enumerable.Range(0, 20).Select(_ => new RUDP.Client()).ToList();

        await Task.WhenAll(clients.Select(c => c.To.Open(host)));

        Assert.Equal(20, clients.Select(c => c.Id).Distinct().Count());
        Assert.All(clients, c => Assert.True(Guid.TryParse(c.Id, out _)));

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    });

    [Fact]
    public Task TimeoutsAreValidated() => Guard.Run(() =>
    {
        var client = new RUDP.Client();
        var server = new RUDP.Server();

        Assert.Throws<Exception>(() => client.HandshakeTimeout = 999);
        Assert.Throws<Exception>(() => client.NoResponseTimeout = 999);
        Assert.Throws<Exception>(() => server.HandshakeTimeout = 999);
        Assert.Throws<Exception>(() => server.NoResponseTimeout = 999);

        client.HandshakeTimeout = 1000;
        server.NoResponseTimeout = 8000;

        Assert.Equal(1000, client.HandshakeTimeout);
        Assert.Equal(8000, server.NoResponseTimeout);

        return Task.CompletedTask;
    });
}
