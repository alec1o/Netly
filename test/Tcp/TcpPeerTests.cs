using TcpClient = Netly.TcpClient;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpPeerTests
{
    private static async Task<(TcpServer server, Host host)> EchoServer()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var opened = Wait.Signal<bool>();

        server.OnAccept(client => client.OnData(stream => client.SendData(Read(stream))));
        server.OnOpen(() => opened.TrySetResult(true));
        server.OnError(_ => opened.TrySetResult(false));
        server.Open(host);

        Assert.True(await Wait.For(opened, what: "server open"));
        return (server, host);
    }

    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    private static Socket RawConnect(Host host)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(host.EndPoint);
        return socket;
    }

    private static async Task CloseServer(TcpServer server)
    {
        if (!server.IsOpened) return;

        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();
        await Wait.For(closed, what: "server close");
    }

    // true when the remote side closed the connection (read 0 or reset)
    private static async Task<bool> IsClosedByRemote(Socket socket, int timeoutMs = 5000)
    {
        var limit = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var buffer = new byte[16];

        while (DateTime.UtcNow < limit)
        {
            try
            {
                if (socket.Poll(50_000, SelectMode.SelectRead) && socket.Receive(buffer) == 0) return true;
            }
            catch (SocketException)
            {
                return true;
            }

            await Task.Delay(10);
        }

        return false;
    }

    private static async Task AssertServerStillWorks(Host host)
    {
        var client = new TcpClient(true);
        var opened = Wait.Signal<bool>();
        var echoed = Wait.Signal<string>();

        client.OnData(s => echoed.TrySetResult(System.Text.Encoding.UTF8.GetString(Read(s))));
        client.OnOpen(() => opened.TrySetResult(true));
        client.OnError(_ => opened.TrySetResult(false));
        client.Open(host);
        Assert.True(await Wait.For(opened, what: "client open"));

        client.SendData(System.Text.Encoding.UTF8.GetBytes("still alive"));

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after attack"));
        client.Close();
    }

    [Fact]
    public async Task InvalidPrefix_ClosesConnection()
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);

        raw.Send(Helper.ArrayConcat(new byte[] { 9, 9, 9, 9 }, BitConverter.GetBytes(4L), new byte[4]));

        Assert.True(await IsClosedByRemote(raw), "connection with invalid prefix must be closed");
        await AssertServerStillWorks(host);
        await CloseServer(server);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task InvalidSize_ClosesConnection(long size)
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);

        raw.Send(Framing.Header(Framing.Version, (ulong)size));

        Assert.True(await IsClosedByRemote(raw));
        await AssertServerStillWorks(host);
        await CloseServer(server);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(1024L * 1024 * 1024 * 8)]
    [InlineData(Framing.DefaultSize + 1)]
    public async Task HugeSize_ClosesConnectionWithoutAllocating(long size)
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);
        var before = GC.GetTotalMemory(true);

        raw.Send(Framing.Header(Framing.Version, (ulong)size));

        Assert.True(await IsClosedByRemote(raw), $"size {size} must be refused");
        Assert.True(GC.GetTotalMemory(true) - before < 64 * 1024 * 1024, "must not allocate the announced size");
        await AssertServerStillWorks(host);
        await CloseServer(server);
    }

    [Fact]
    public async Task PartialHeaderAndIdle_DoesNotAffectOtherClients()
    {
        var (server, host) = await EchoServer();
        using var slow = RawConnect(host);

        // slowloris style: only part of the header, then silence
        slow.Send(Framing.Create(10).Take(3).ToArray());

        await AssertServerStillWorks(host);
        Assert.False(await IsClosedByRemote(slow, 500), "server must not close an idle valid-so-far connection immediately");

        await CloseServer(server);
    }

    [Fact]
    public async Task ByteByByteValidMessage_IsAccepted()
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);
        raw.NoDelay = true;

        var body = new byte[] { 1, 2, 3, 4, 5 };
        var frame = Helper.ArrayConcat(Framing.Create(body.Length), body);

        foreach (var b in frame)
        {
            raw.Send(new[] { b });
            await Task.Delay(2);
        }

        var received = new byte[frame.Length];
        var offset = 0;
        raw.ReceiveTimeout = 5000;

        while (offset < received.Length) offset += raw.Receive(received, offset, received.Length - offset, SocketFlags.None);

        Assert.Equal(frame, received);
        await CloseServer(server);
    }

    [Fact]
    public async Task ManyGarbageConnections_ServerSurvivesAndCleansUp()
    {
        const int total = 100;
        var (server, host) = await EchoServer();
        var random = new Random(3);

        for (var i = 0; i < total; i++)
        {
            using var raw = RawConnect(host);
            var garbage = new byte[64];
            random.NextBytes(garbage);
            raw.Send(garbage);
        }

        await Wait.Until(() => server.Clients.Count == 0, what: "server cleaned garbage clients");
        await AssertServerStillWorks(host);
        await CloseServer(server);
    }

    [Fact]
    public async Task AbruptReset_IsCleanedUp()
    {
        var (server, host) = await EchoServer();
        var raw = RawConnect(host);

        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        // linger 0 sends RST instead of FIN
        raw.LingerState = new LingerOption(true, 0);
        raw.Close();

        await Wait.Until(() => server.Clients.Count == 0, what: "server removed reset client");
        await CloseServer(server);
    }

    [Fact]
    public async Task ClientDisconnectInTheMiddleOfMessage_IsCleanedUp()
    {
        var (server, host) = await EchoServer();
        var raw = RawConnect(host);

        raw.Send(Helper.ArrayConcat(Framing.Create(1000), new byte[100]));
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");
        raw.Close();

        await Wait.Until(() => server.Clients.Count == 0, what: "server removed client");
        await CloseServer(server);
    }

    [Fact]
    public async Task ServerAcceptsManyClients()
    {
        const int total = 20;
        var (server, host) = await EchoServer();
        var clients = new List<TcpClient>();
        var opened = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new TcpClient(true);
            client.OnOpen(() => Interlocked.Increment(ref opened));
            client.Open(host);
            clients.Add(client);
        }

        await Wait.Until(() => opened == total && server.Clients.Count == total, what: "all clients connected");
        Assert.All(clients, c => Assert.True(c.IsOpened));
        Assert.Equal(total, server.Clients.Select(c => c.Id).Distinct().Count());

        clients.ForEach(c => c.Close());
        await CloseServer(server);
    }

    [Fact]
    public async Task SmallMessagesPingPongIsFast()
    {
        const int rounds = 100;
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var pong = new SemaphoreSlim(0);
        var opened = Wait.Signal<bool>();

        client.OnData(_ => pong.Release());
        client.OnOpen(() => opened.TrySetResult(true));
        client.Open(host);
        Assert.True(await Wait.For(opened, what: "client open"));

        var watch = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < rounds; i++)
        {
            client.SendData(new byte[] { 1, 2, 3, 4 });
            Assert.True(await pong.WaitAsync(TimeSpan.FromSeconds(5)), $"round {i}");
        }

        // nagle + delayed ack made every round about 80 ms
        Assert.True(watch.ElapsedMilliseconds / rounds < 20, $"{watch.ElapsedMilliseconds / rounds} ms by round trip");

        client.Close();
        await CloseServer(server);
    }

    // local ip of this machine: prefer a real interface, loopback is the fallback (macos has only 127.0.0.1)
    private static IPAddress LocalIp()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
            .ToList();

        return addresses.FirstOrDefault(x => !IPAddress.IsLoopback(x)) ?? IPAddress.Loopback;
    }

    [Fact]
    public async Task ClientCanBindToSpecificLocalIpBeforeConnect()
    {
        var ip = LocalIp();
        var port = Net.FreeTcpPort();
        var server = new TcpServer(true);
        var accepted = Wait.Signal<TcpClient>();
        var serverOpened = Wait.Signal<bool>();

        server.OnAccept(c => accepted.TrySetResult(c));
        server.OnOpen(() => serverOpened.TrySetResult(true));
        server.Open(new Host(IPAddress.Any, port));
        Assert.True(await Wait.For(serverOpened, what: "server open"));

        var client = new TcpClient(true);
        var opened = Wait.Signal<bool>();
        // OnModify runs before connect: the socket can be bound to a local ip (and port) here
        client.OnModify(socket => socket.Bind(new IPEndPoint(ip, 0)));
        client.OnOpen(() => opened.TrySetResult(true));
        client.OnError(_ => opened.TrySetResult(false));
        client.Open(new Host(ip, port));

        Assert.True(await Wait.For(opened, what: "client open"));

        var peer = await Wait.For(accepted, what: "server accept");
        Assert.Equal(ip, peer.Host.Address);

        client.Close();
        await CloseServer(server);
    }

    [Fact]
    public async Task BindToUnassignableIpRaisesError()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var failed = Wait.Signal<Exception>();

        client.OnError(e => failed.TrySetResult(e));
        client.OnModify(socket => socket.Bind(new IPEndPoint(IPAddress.Parse("203.0.113.1"), 0)));
        client.Open(host);

        Assert.NotNull(await Wait.For(failed, what: "bind error"));
        Assert.False(client.IsOpened);

        await CloseServer(server);
    }

    [Fact]
    public async Task InvalidDataReceived_CallsOnErrorThenOnClose()
    {
        var host = Net.TcpHost();
        var listener = new TcpListener(host.IPEndPoint);
        listener.Start();

        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var client = new TcpClient(true);

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });

        client.Open(host);

        // framing is active on the client, the peer sends data without a header
        using var raw = await listener.AcceptSocketAsync();
        raw.Send(new byte[64]);

        Assert.True(await Wait.For(closed, what: "close after invalid data"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        listener.Stop();
    }

    [Fact]
    public async Task ClientHeaderNotCompleted_CallsOnErrorThenOnClose()
    {
        var host = Net.TcpHost();
        var listener = new TcpListener(host.IPEndPoint);
        listener.Start();

        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var client = new TcpClient(true) { HeaderTimeout = TimeSpan.FromMilliseconds(300) };

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });
        client.Open(host);

        // the peer starts a header and stops
        using var raw = await listener.AcceptSocketAsync();
        raw.Send(Framing.Create(10).Take(3).ToArray());

        Assert.True(await Wait.For(closed, what: "close after the header timeout"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        listener.Stop();
    }

    [Fact]
    public async Task ServerHeaderNotCompleted_ClosesOnlyThatConnection()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true) { HeaderTimeout = TimeSpan.FromMilliseconds(300) };
        var opened = Wait.Signal<bool>();

        server.OnAccept(client => client.OnData(stream => client.SendData(Read(stream))));
        server.OnOpen(() => opened.TrySetResult(true));
        server.Open(host);
        Assert.True(await Wait.For(opened, what: "server open"));

        using var slow = RawConnect(host);
        slow.Send(Framing.Create(10).Take(3).ToArray());

        Assert.True(await IsClosedByRemote(slow, 3000), "the header was not completed, the connection must close");
        await AssertServerStillWorks(host);
        await CloseServer(server);
    }

    [Fact]
    public async Task HeaderTimeout_DoesNotCloseASlowButCompleteMessage()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true) { HeaderTimeout = TimeSpan.FromMilliseconds(300) };
        var opened = Wait.Signal<bool>();
        var received = Wait.Signal<int>();

        server.OnAccept(client => client.OnData(stream => received.TrySetResult((int)stream.Length)));
        server.OnOpen(() => opened.TrySetResult(true));
        server.Open(host);
        Assert.True(await Wait.For(opened, what: "server open"));

        using var raw = RawConnect(host);
        raw.NoDelay = true;
        var frame = Helper.ArrayConcat(Framing.Create(20), new byte[20]);

        // the header arrives in two parts inside the time, the body can be slow
        raw.Send(frame.Take(4).ToArray());
        await Task.Delay(100);
        raw.Send(frame.Skip(4).Take(6).ToArray());
        await Task.Delay(600);
        raw.Send(frame.Skip(10).ToArray());

        Assert.Equal(20, await Wait.For(received, what: "slow message"));
        await CloseServer(server);
    }

    [Fact]
    public async Task OpenFailure_CallsOnErrorThenOnClose()
    {
        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var client = new TcpClient(true);

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });

        client.Open(Net.TcpHost());

        Assert.True(await Wait.For(closed, what: "close after the open failed"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);
    }
}
