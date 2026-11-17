using UdpClient = Netly.UdpClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Udp;

public class UdpTests
{
    // UDP has no handshake: the server only knows a client after its first datagram.
    private static readonly byte[] Hello = Encoding.ASCII.GetBytes("netly-udp-hello");

    private static bool IsHello(byte[] data) => data.AsSpan().SequenceEqual(Hello);

    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    private static async Task<bool> Open(UdpServer server, Host host)
    {
        var result = Wait.Signal<bool>();
        server.OnOpen(() => result.TrySetResult(true));
        server.OnError(_ => result.TrySetResult(false));
        server.Open(host);
        return await Wait.For(result, what: "server open result");
    }

    private static async Task<bool> Open(UdpClient client, Host host)
    {
        var result = Wait.Signal<bool>();
        client.OnOpen(() => result.TrySetResult(true));
        client.OnError(_ => result.TrySetResult(false));
        client.Open(host);
        return await Wait.For(result, what: "client open result");
    }

    private static async Task Close(UdpServer server)
    {
        if (!server.IsOpened) return;

        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();
        await Wait.For(closed, what: "server close");
    }

    private static async Task Close(UdpClient client)
    {
        if (!client.IsOpened) return;

        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.Close();
        await Wait.For(closed, what: "client close");
    }

    private static async Task<(UdpServer server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();

        server.OnAccept(client =>
        {
            client.OnData(s =>
            {
                var data = Read(s);
                if (!IsHello(data)) client.SendData(data);
            });
            client.OnEvent((name, s) => client.SendEvent(name, Read(s)));
        });

        Assert.True(await Open(server, host));
        return (server, host);
    }

    private static async Task<UdpClient> Connect(Host host, UdpServer server, Action<UdpClient>? setup = null)
    {
        var client = new UdpClient();
        setup?.Invoke(client);
        Assert.True(await Open(client, host));

        // the server learns the client by its first datagram
        var known = server.Clients.Count;
        client.SendData(Hello);
        await Wait.Until(() => server.Clients.Count > known, what: "server accept");
        return client;
    }

    [Fact]
    public void DefaultConfig()
    {
        var client = new UdpClient();
        Assert.False(client.IsOpened);
        Assert.False(client.IsServer);
        Assert.False(string.IsNullOrWhiteSpace(client.Id));
        Assert.NotNull(client.Host);

        var server = new UdpServer();
        Assert.False(server.IsOpened);
        Assert.False(string.IsNullOrWhiteSpace(server.Id));
        Assert.NotNull(server.Host);
        Assert.Empty(server.Clients);
    }

    [Fact]
    public async Task ServerOpenAndClose()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        int open = 0, close = 0, modify = 0, error = 0;
        server.OnOpen(() => Interlocked.Increment(ref open));
        server.OnClose(() => Interlocked.Increment(ref close));
        server.OnModify(_ => Interlocked.Increment(ref modify));
        server.OnError(_ => Interlocked.Increment(ref error));

        server.Open(host);
        await Wait.Until(() => server.IsOpened && open == 1, what: "server open");

        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));
        Assert.Equal(host.Port, server.Host.Port);

        await Close(server);

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    }

    [Fact]
    public async Task ServerOpenOnUnassignableAddressFails()
    {
        var server = new UdpServer();

        Assert.False(await Open(server, new Host("1.1.1.1", 0)));
        Assert.False(server.IsOpened);
    }

    [Fact]
    public async Task ServerCanReopenAfterClose()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();

        for (var i = 0; i < 3; i++)
        {
            Assert.True(await Open(server, host));
            Assert.True(server.IsOpened);
            await Close(server);
            Assert.False(server.IsOpened);
        }
    }

    [Fact]
    public async Task ClientOpenAndClose()
    {
        var (server, host) = await EchoServer();
        var client = new UdpClient();
        int open = 0, close = 0, modify = 0, error = 0;
        client.OnOpen(() => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnModify(_ => Interlocked.Increment(ref modify));
        client.OnError(_ => Interlocked.Increment(ref error));

        client.Open(host);
        await Wait.Until(() => client.IsOpened && open == 1, what: "client open");

        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await Close(client);

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);

        await Close(server);
    }

    [Fact]
    public async Task ClientDataBeforeOpen_CallsOnError()
    {
        var client = new UdpClient();
        var error = false;
        client.OnError(_ => error = true);

        client.SendData(Utf8("not sent"));
        client.Close();

        Assert.False(client.IsOpened);
        Assert.True(error);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(36)]
    [InlineData(1024)]
    [InlineData(8 * 1024)]
    public async Task EchoData(int size)
    {
        var (server, host) = await EchoServer();
        var received = Wait.Signal<byte[]>();
        var client = await Connect(host, server, c => c.OnData(s => received.TrySetResult(Read(s))));

        var payload = Net.Payload(size);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: $"echo of {size} bytes"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, host) = await EchoServer();
        var received = Wait.Signal<(string name, byte[] data)>();
        var client = await Connect(host, server, c => c.OnEvent((name, s) => received.TrySetResult((name, Read(s)))));

        var payload = Net.Payload(200, 5);
        client.SendEvent("ping", payload);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("ping", name);
        Assert.Equal(payload, data);

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task EventWithoutContent_IsDelivered()
    {
        var (server, host) = await EchoServer();
        var received = Wait.Signal<(string name, long length)>();
        var client = await Connect(host, server, c => c.OnEvent((name, s) => received.TrySetResult((name, s.Length))));

        client.SendEvent("empty");

        Assert.Equal(("empty", 0L), await Wait.For(received, what: "empty event echo"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task SendData_Overloads()
    {
        var (server, host) = await EchoServer();
        var received = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        var client = await Connect(host, server, c => c.OnData(s => received.Enqueue(Read(s))));

        var bytes = Net.Payload(100);

        client.SendData(bytes, 10, 20);
        client.SendData(new ArraySegment<byte>(bytes, 30, 5));
        client.SendData(new List<ArraySegment<byte>> { new(bytes, 0, 3), new(bytes, 50, 4) });

        var stream = new MemoryStream(bytes) { Position = 90 };
        client.SendData(stream);

        await Wait.Until(() => received.Count == 4, what: "four echoes");

        var all = received.ToArray();
        Assert.Contains(bytes.AsSpan(10, 20).ToArray(), all, new BytesComparer());
        Assert.Contains(bytes.AsSpan(30, 5).ToArray(), all, new BytesComparer());
        Assert.Contains(bytes.AsSpan(0, 3).ToArray().Concat(bytes.AsSpan(50, 4).ToArray()).ToArray(), all,
            new BytesComparer());
        Assert.Contains(bytes.AsSpan(90, 10).ToArray(), all, new BytesComparer());

        // the stream is not consumed
        Assert.Equal(90, stream.Position);

        await Close(client);
        await Close(server);
    }

    private sealed class BytesComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y) => x!.AsSpan().SequenceEqual(y);

        public int GetHashCode(byte[] obj) => obj.Length;
    }

    [Fact]
    public async Task Send_InvalidArguments_Throw()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host, server);

        Assert.Throws<ArgumentNullException>(() => client.SendData((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => client.SendEvent("name", (byte[])null!));
        Assert.Throws<ArgumentException>(() => client.SendEvent("", new byte[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(new byte[4], 3, 4));
        Assert.Throws<ArgumentException>(() => client.SendData(new byte[UdpClient.MaxDatagramSize + 1]));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task OnAllocate_ReceivesDatagramSizeAndStreamIsUsed()
    {
        var (server, host) = await EchoServer();
        var sizes = new System.Collections.Concurrent.ConcurrentBag<long>();
        var received = Wait.Signal<byte[]>();
        var client = await Connect(host, server, c =>
        {
            c.OnAllocate(size =>
            {
                sizes.Add(size);
                return new MemoryStream();
            });
            c.OnData(s => received.TrySetResult(Read(s)));
        });

        var payload = Net.Payload(300);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "echo"));
        Assert.Contains(300L, sizes);

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ServerAcceptsManyClients()
    {
        const int total = 20;
        var (server, host) = await EchoServer();
        var clients = new List<UdpClient>();

        for (var i = 0; i < total; i++)
        {
            var client = new UdpClient();
            Assert.True(await Open(client, host));
            clients.Add(client);
        }

        foreach (var c in clients) c.SendData(Hello);
        await Wait.Until(() => server.Clients.Count == total, what: "all accepts");

        Assert.Equal(total, server.Clients.Select(c => c.Id).Distinct().Count());
        Assert.All(server.Clients, c => Assert.True(c.IsServer));

        foreach (var c in clients) await Close(c);
        await Close(server);
    }

    [Fact]
    public async Task BroadcastReachesEveryClient()
    {
        const int total = 10;
        var host = Net.UdpHost();
        var server = new UdpServer();
        Assert.True(await Open(server, host));

        int dataCount = 0, eventCount = 0;
        var clients = new List<UdpClient>();

        for (var i = 0; i < total; i++)
        {
            var client = new UdpClient();
            client.OnData(s =>
            {
                if (Encoding.UTF8.GetString(Read(s)) == "broadcast") Interlocked.Increment(ref dataCount);
            });
            client.OnEvent((name, _) =>
            {
                if (name == "news") Interlocked.Increment(ref eventCount);
            });
            Assert.True(await Open(client, host));
            client.SendData(Hello);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Count == total, what: "server accepted all");

        server.BroadcastData(Utf8("broadcast"));
        server.BroadcastEvent("news", Utf8("payload"));

        await Wait.Until(() => dataCount == total && eventCount == total, what: "broadcast delivered");

        foreach (var c in clients) await Close(c);
        await Close(server);
    }

    [Fact]
    public async Task BroadcastCondition_FiltersClients()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        var accepted = new System.Collections.Concurrent.ConcurrentDictionary<string, UdpClient>();
        server.OnAccept(c => c.OnData(s => accepted[Encoding.UTF8.GetString(Read(s))] = c));
        Assert.True(await Open(server, host));

        var a = new UdpClient();
        var b = new UdpClient();
        var receivedA = Wait.Signal<string>();
        var receivedB = 0;
        a.OnData(s => receivedA.TrySetResult(Encoding.UTF8.GetString(Read(s))));
        b.OnData(_ => Interlocked.Increment(ref receivedB));

        Assert.True(await Open(a, host));
        Assert.True(await Open(b, host));
        a.SendData(Utf8("i-am-a"));
        b.SendData(Utf8("i-am-b"));
        await Wait.Until(() => accepted.Count == 2, what: "server identified both clients");

        var target = accepted["i-am-a"];
        server.BroadcastData(Utf8("only-a"), c => c.Id == target.Id);

        Assert.Equal("only-a", await Wait.For(receivedA, what: "targeted delivery"));
        await Wait.Never(() => receivedB > 0, TimeSpan.FromMilliseconds(300), "delivery to other client");

        await Close(a);
        await Close(b);
        await Close(server);
    }

    [Fact]
    public async Task ServerSendsToSpecificClient()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        var peers = new System.Collections.Concurrent.ConcurrentDictionary<string, UdpClient>();
        server.OnAccept(c => c.OnData(s =>
        {
            var text = Encoding.UTF8.GetString(Read(s));
            if (text.StartsWith("i-am-")) peers[text] = c;
        }));
        Assert.True(await Open(server, host));

        var a = new UdpClient();
        var b = new UdpClient();
        var receivedA = Wait.Signal<string>();
        var receivedB = 0;
        a.OnData(s => receivedA.TrySetResult(Encoding.UTF8.GetString(Read(s))));
        b.OnData(_ => Interlocked.Increment(ref receivedB));

        Assert.True(await Open(a, host));
        Assert.True(await Open(b, host));
        a.SendData(Utf8("i-am-a"));
        b.SendData(Utf8("i-am-b"));
        await Wait.Until(() => peers.Count == 2, what: "server identified both clients");

        peers["i-am-a"].SendData(Utf8("only-a"));

        Assert.Equal("only-a", await Wait.For(receivedA, what: "targeted delivery"));
        await Wait.Never(() => receivedB > 0, TimeSpan.FromMilliseconds(300), "delivery to other client");

        await Close(a);
        await Close(b);
        await Close(server);
    }

    [Fact]
    public async Task ServerClose_ClosesClients()
    {
        var (server, host) = await EchoServer();
        var closed = Wait.Signal<bool>();
        var client = await Connect(host, server);
        var peer = server.Clients.Single();
        peer.OnClose(() => closed.TrySetResult(true));

        await Close(server);

        await Wait.For(closed, what: "peer close");
        Assert.False(peer.IsOpened);
        Assert.Empty(server.Clients);

        await Close(client);
    }

    [Fact]
    public async Task ClientCloseOfServerSide_ForgetsPeer()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host, server);

        var peer = server.Clients.Single();
        peer.Close();

        await Wait.Until(() => server.Clients.Count == 0, what: "peer removed");
        Assert.False(peer.IsOpened);
        Assert.True(server.IsOpened);

        // the same address is accepted again
        client.SendData(Hello);
        await Wait.Until(() => server.Clients.Count == 1, what: "peer accepted again");

        await Close(client);
        await Close(server);
    }

    /* ---- ROBUSTNESS --- */

    private static async Task AssertServerStillWorks(Host host)
    {
        var echoed = Wait.Signal<string>();
        var client = new UdpClient();
        client.OnData(s => echoed.TrySetResult(Encoding.UTF8.GetString(Read(s))));
        Assert.True(await Open(client, host));
        client.SendData(Hello);
        await Task.Delay(100);
        client.SendData(Utf8("still alive"));

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after garbage"));
        await Close(client);
    }

    [Fact]
    public async Task GarbageDatagrams_DoNotBreakServer()
    {
        var (server, host) = await EchoServer();
        var errors = 0;
        server.OnError(_ => Interlocked.Increment(ref errors));
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var random = new Random(8);

        for (var i = 1; i < 300; i++)
        {
            var garbage = new byte[i];
            random.NextBytes(garbage);
            raw.SendTo(garbage, host.EndPoint);
        }

        await AssertServerStillWorks(host);
        Assert.True(server.IsOpened);

        await Close(server);
    }

    [Fact]
    public async Task EmptyDatagram_IsIgnored()
    {
        var (server, host) = await EchoServer();
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        raw.SendTo(Array.Empty<byte>(), host.EndPoint);

        await AssertServerStillWorks(host);
        Assert.Single(server.Clients);

        await Close(server);
    }

    [Fact]
    public async Task LargeDatagram_IsEchoed()
    {
        var (server, host) = await EchoServer();
        var received = Wait.Signal<byte[]>();
        var client = await Connect(host, server, c => c.OnData(s => received.TrySetResult(Read(s))));

        var payload = Net.Payload(60000);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "60000 bytes echo"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ManyShortLivedClients_ServerKeepsWorking()
    {
        var (server, host) = await EchoServer();

        for (var i = 0; i < 50; i++)
        {
            var client = new UdpClient();
            Assert.True(await Open(client, host));
            client.SendData(Hello);
            await Close(client);
        }

        await AssertServerStillWorks(host);
        await Close(server);
    }

    [Fact]
    public async Task CallbackThrowing_CallsOnErrorThenOnClose()
    {
        var (server, host) = await EchoServer();
        var events = new List<string>();
        var closed = Wait.Signal<bool>();
        var client = await Connect(host, server, c =>
        {
            c.OnError(_ => { lock (events) events.Add("error"); });
            c.OnClose(() =>
            {
                lock (events) events.Add("close");
                closed.TrySetResult(true);
            });
            c.OnData(_ => throw new InvalidOperationException("user callback"));
        });

        client.SendData(Utf8("one"));

        Assert.True(await Wait.For(closed, what: "close after the exception of the app"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);
        Assert.False(client.IsOpened);

        await Close(server);
    }

    [Fact]
    public async Task MaxDatagram_FollowsTheSendBufferOfTheSocket()
    {
        var (server, host) = await EchoServer();

        // default: the buffer is raised, so the limit is the protocol one or what loopback is seen to deliver
        var normal = new UdpClient();
        Assert.True(await Open(normal, host));
        Assert.Equal(Math.Min(UdpClient.MaxDatagramSize, Capabilities.LoopbackDatagram), normal.MaxDatagram);
        normal.SendData(Hello);
        normal.SendData(new byte[normal.MaxDatagram]);

        // the user shrinks the send buffer: the limit follows it, a bigger message is sent in fragments, not lost
        var small = new UdpClient();
        small.OnModify(socket => socket.SendBufferSize = 4096);
        Assert.True(await Open(small, host));
        Assert.InRange(small.MaxDatagram, 512, 16384);
        small.SendData(new byte[UdpClient.MaxDatagramSize]);
        Assert.Throws<ArgumentException>(() => small.SendData(new byte[UdpClient.MaxDatagramSize + 1]));
        small.SendData(Hello);

        // a client of a server uses the socket of the server
        await Wait.Until(() => server.Clients.Count == 2, what: "server accept");
        Assert.All(server.Clients, c => Assert.Equal(normal.MaxDatagram, c.MaxDatagram));

        await Close(normal);
        await Close(small);
        await Close(server);
    }
}
