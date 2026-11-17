using System.Net;
using System.Net.Sockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpRobustnessTests
{
    private static async Task<(RudpServer server, Host host)> EchoServer(int noResponse = 5000)
    {
        var host = Net.UdpHost();
        var server = new RudpServer { NoResponseTimeout = noResponse };
        server.OnAccept(client => client.OnData((stream, type) => client.SendData(stream.GetBytes(), type)));
        await server.OpenAsync(host);
        return (server, host);
    }

    private static async Task AssertServerStillWorks(Host host)
    {
        var client = new RudpClient();
        var echoed = Wait.Signal<string>();
        Exception? error = null;
        client.OnError(e => error = e);
        client.OnData((stream, _) => echoed.TrySetResult(Encoding.UTF8.GetString(stream.GetBytes())));
        await client.OpenAsync(host);

        Assert.True(client.IsOpened, $"open failed: {error?.Message}");

        client.SendData("still alive", RudpDelivery.Reliable);

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after attack"));
        await client.CloseAsync();
    }

    [Fact]
    public Task ClientClosesWhenServerStopsAnswering() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host);
        var client = new RudpClient { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(proxy.Host);

        proxy.Blackhole = true;

        await Wait.For(closed, TimeSpan.FromSeconds(10), "close by timeout");
        Assert.False(client.IsOpened);

        await server.CloseAsync();
    });

    [Fact]
    public Task ServerSessionClosesWhenClientDisappears() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        server.OnAccept(c => c.OnClose(() => closed.TrySetResult(true)));
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host);
        var client = new RudpClient();
        await client.OpenAsync(proxy.Host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        proxy.Blackhole = true;

        await Wait.For(closed, TimeSpan.FromSeconds(10), "server session timeout");
        await Wait.Until(() => server.Clients.Count == 0, what: "session removed");

        await server.CloseAsync();
    });

    [Fact]
    public Task IdleConnectionStaysOpen() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer(1500);
        var client = new RudpClient { NoResponseTimeout = 1500 };
        await client.OpenAsync(host);

        // idle for more than twice the no response timeout, keep alive packets must hold the connection
        await Task.Delay(3500);

        Assert.True(client.IsOpened);
        Assert.Single(server.Clients);

        var echoed = Wait.Signal<bool>();
        client.OnData((_, _) => echoed.TrySetResult(true));
        client.SendData("after idle", RudpDelivery.Reliable);
        await Wait.For(echoed, what: "echo after idle");

        await client.CloseAsync();
        await server.CloseAsync();
    }, TimeSpan.FromSeconds(30));

    [Fact]
    public Task IdleTrafficIsSmall() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host);
        var client = new RudpClient();
        await client.OpenAsync(proxy.Host);
        await Task.Delay(100);
        var before = proxy.ToServerPackets + proxy.ToClientPackets;

        await Task.Delay(3000);

        // keep alive, not a ping every 100ms: at most 2 packets by second in each direction
        var idle = proxy.ToServerPackets + proxy.ToClientPackets - before;
        Assert.True(idle <= 12, $"{idle} packets in 3s of idle");

        await client.CloseAsync();
        await server.CloseAsync();
    }, TimeSpan.FromSeconds(30));

    [Fact]
    public Task ClosedClientCanOpenAgain() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var open = 0;
        client.OnOpen(() => Interlocked.Increment(ref open));

        for (var i = 0; i < 5; i++)
        {
            await client.OpenAsync(host);
            Assert.True(client.IsOpened, $"round {i}");
            await client.CloseAsync();
            Assert.False(client.IsOpened);
        }

        Assert.Equal(5, open);
        await server.CloseAsync();
    });

    [Fact]
    public Task SendOnClosedClient_CallsOnError() => Guard.Run(async () =>
    {
        var client = new RudpClient();
        var error = false;
        client.OnError(_ => error = true);

        client.SendData("not sent", RudpDelivery.Reliable);
        await client.CloseAsync();

        Assert.False(client.IsOpened);
        Assert.True(error);
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

        await server.CloseAsync();
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

        await server.CloseAsync();
    });

    [Fact]
    public Task ForgedPacketsCannotCloseOrInjectIntoASession() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        var injected = 0;
        server.OnAccept(c => c.OnData((_, _) => Interlocked.Increment(ref injected)));
        await server.OpenAsync(host);

        // the proxy gives the client a known source endpoint, the attacker uses the same one
        var client = new RudpClient();
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

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

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task SynFloodIsCleanedUp() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer { HandshakeTimeout = 1000 };
        server.OnAccept(client => client.OnData((stream, type) => client.SendData(stream.GetBytes(), type)));
        await server.OpenAsync(host);

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

        await server.CloseAsync();
    });

    [Fact]
    public Task ServerCloseNotifiesClientsFast() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(host);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        await server.CloseAsync();
        await Wait.For(closed, what: "client close");

        Assert.True(watch.ElapsedMilliseconds < 1000, $"close notice took {watch.ElapsedMilliseconds} ms");
    });

    [Fact]
    public Task ManyClientsEchoIndependently() => Guard.Run(async () =>
    {
        const int total = 100;
        var (server, host) = await EchoServer();
        var echoed = 0;
        var clients = new List<RudpClient>();

        for (var i = 0; i < total; i++)
        {
            var client = new RudpClient();
            var expected = $"client-{i}";
            client.OnData((stream, _) =>
            {
                if (Encoding.UTF8.GetString(stream.GetBytes()) == expected) Interlocked.Increment(ref echoed);
            });
            clients.Add(client);
        }

        await Task.WhenAll(clients.Select(c => c.OpenAsync(host)));
        Assert.All(clients, c => Assert.True(c.IsOpened));

        for (var i = 0; i < total; i++) clients[i].SendData($"client-{i}", RudpDelivery.Reliable);

        await Wait.Until(() => echoed == total, TimeSpan.FromSeconds(20), "all echoes");
        Assert.Equal(total, server.Clients.Count);

        await Task.WhenAll(clients.Select(c => c.CloseAsync()));
        await server.CloseAsync();
    });

    [Fact]
    public Task SessionIdsAreUnique() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var clients = Enumerable.Range(0, 20).Select(_ => new RudpClient()).ToList();

        await Task.WhenAll(clients.Select(c => c.OpenAsync(host)));

        Assert.Equal(20, clients.Select(c => c.Id).Distinct().Count());
        Assert.All(clients, c => Assert.True(Guid.TryParse(c.Id, out _)));

        await Task.WhenAll(clients.Select(c => c.CloseAsync()));
        await server.CloseAsync();
    });

    [Fact]
    public Task TimeoutsAreValidated() => Guard.Run(() =>
    {
        var client = new RudpClient();
        var server = new RudpServer();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.HandshakeTimeout = 999);
        Assert.Throws<ArgumentOutOfRangeException>(() => client.NoResponseTimeout = 999);
        Assert.Throws<ArgumentOutOfRangeException>(() => server.HandshakeTimeout = 999);
        Assert.Throws<ArgumentOutOfRangeException>(() => server.NoResponseTimeout = 999);

        client.HandshakeTimeout = 1000;
        server.NoResponseTimeout = 8000;

        Assert.Equal(1000, client.HandshakeTimeout);
        Assert.Equal(8000, server.NoResponseTimeout);

        return Task.CompletedTask;
    });

    [Fact]
    public Task TimeoutsCannotChangeWhileOpened() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        await server.OpenAsync(host);

        Assert.Throws<InvalidOperationException>(() => server.HandshakeTimeout = 2000);

        await server.CloseAsync();
    });
}
