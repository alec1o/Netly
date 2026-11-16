using NetlyTest.Support;
using Xunit.Abstractions;

namespace NetlyTest.Rudp;

public class RudpScaleTests(ITestOutputHelper output)
{
    [Fact]
    public Task IdleConnections_AreNotUpdatedEveryPass() => Guard.Run(async () =>
    {
        const int total = 200;
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(c => c.OnData((s, type) =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            c.SendData(buffer, type);
        }));
        await server.OpenAsync(host);

        var clients = new List<RudpClient>();
        var echoed = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new RudpClient();
            client.OnData((_, _) => Interlocked.Increment(ref echoed));
            clients.Add(client);
        }

        await Task.WhenAll(clients.Select(c => c.OpenAsync(host)));
        await Wait.Until(() => server.Clients.Count == total, TimeSpan.FromSeconds(30), "all connected");

        // let the handshake traffic finish, then count the updates of a quiet period
        await Task.Delay(500);
        var before = RudpScheduler.Ticks;
        await Task.Delay(2000);
        var ticks = RudpScheduler.Ticks - before;

        output.WriteLine($"{ticks} updates in 2 s for {total * 2} idle connections");

        // updating every connection at every 2 ms pass would be about 400 thousand
        Assert.True(ticks < 40_000, $"{ticks} updates");

        // the connections are alive (keep alive) and still work after being idle
        Assert.All(clients, c => Assert.True(c.IsOpened));
        foreach (var c in clients) c.SendData(Net.Payload(100));
        await Wait.Until(() => Volatile.Read(ref echoed) == total, TimeSpan.FromSeconds(30), "echo from all");

        foreach (var c in clients) c.Close();
        await server.CloseAsync();
    });

    [Fact]
    public Task IdleConnection_SurvivesLongerThanTheNoResponseTimeout() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer { NoResponseTimeout = 1500 };
        await server.OpenAsync(host);
        var client = new RudpClient { NoResponseTimeout = 1500 };
        var closes = 0;
        client.OnClose(() => Interlocked.Increment(ref closes));
        await client.OpenAsync(host);

        // 3 times the timeout without any user data: only the keep alive holds the connection
        await Task.Delay(4500);

        Assert.True(client.IsOpened);
        Assert.Equal(0, Volatile.Read(ref closes));
        Assert.Single(server.Clients);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task SilentPeer_IsClosedByTheNoResponseTimeout() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host);
        var client = new RudpClient { NoResponseTimeout = 1500 };
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(proxy.Host);

        // the network goes silent: the idle connection must still notice it
        proxy.Blackhole = true;

        await Wait.For(closed, TimeSpan.FromSeconds(15), "close by timeout");
        Assert.False(client.IsOpened);

        await server.CloseAsync();
    });
}
