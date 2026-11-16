using System.Security.Authentication;

using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpClientTests
{
    private static async Task<(TcpServer server, Host host)> EchoServer(bool framing = true, bool secure = false,
        bool allowUnsecured = false)
    {
        var host = Net.TcpHost();
        var server = new TcpServer(framing);

        server.OnAccept(client =>
        {
            client.OnData(stream => client.SendData(Read(stream)));
            client.OnEvent((name, stream) => client.SendEvent(name, Read(stream)));
        });

        if (secure)
            server.ToSecure(File.ReadAllBytes("certificate.p12"), "alecio", SslProtocols.Tls12, allowUnsecured);

        var opened = Wait.Signal<bool>();
        server.OnOpen(() => opened.TrySetResult(true));
        server.OnError(_ => opened.TrySetResult(false));
        server.Open(host);

        Assert.True(await Wait.For(opened, what: "server open"));
        return (server, host);
    }

    private static async Task<TcpClient> Connect(Host host, bool framing = true, bool secure = false)
    {
        var client = new TcpClient(framing);

        if (secure)
        {
            client.ToSecure(true);
            client.OnSecure((_, _, _) => true);
        }

        var opened = Wait.Signal<bool>();
        client.OnOpen(() => opened.TrySetResult(true));
        client.OnError(_ => opened.TrySetResult(false));
        client.Open(host);

        Assert.True(await Wait.For(opened, what: "client connect"));
        return client;
    }

    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    private static async Task Close(TcpClient client, TcpServer server)
    {
        await Closed(client);
        await Closed(server);
    }

    private static async Task Closed(TcpClient client)
    {
        if (!client.IsOpened) return;

        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.Close();

        await Wait.For(closed, what: "client close");
    }

    private static async Task Closed(TcpServer server)
    {
        if (!server.IsOpened) return;

        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();

        await Wait.For(closed, what: "server close");
    }

    private static async Task Opened(TcpClient client, Host host)
    {
        var opened = Wait.Signal<bool>();
        client.OnOpen(() => opened.TrySetResult(true));
        client.OnError(_ => opened.TrySetResult(false));
        client.Open(host);

        Assert.True(await Wait.For(opened, what: "client open"));
    }

    [Fact]
    public async Task Lifecycle()
    {
        var (server, host) = await EchoServer();
        Assert.True(server.IsOpened);

        var client = await Connect(host);
        Assert.True(client.IsOpened);
        Assert.NotNull(client.Socket);

        await Wait.Until(() => server.Clients.Count == 1, what: "server accept");

        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await Closed(client);

        Assert.True(await Wait.For(closed, what: "client close"));
        Assert.False(client.IsOpened);
        await Wait.Until(() => server.Clients.Count == 0, what: "server removes client");

        await Closed(server);
        Assert.False(server.IsOpened);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(36)]
    [InlineData(64 * 1024)]
    [InlineData(1024 * 1024)]
    public async Task EchoMessage(int size)
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        var payload = Net.Payload(size);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: $"echo of {size} bytes"));
        await Close(client, server);
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var received = Wait.Signal<(string, byte[])>();
        client.OnEvent((name, s) => received.TrySetResult((name, Read(s))));

        var payload = Net.Payload(2048);
        client.SendEvent("ação", payload);

        var (eventName, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("ação", eventName);
        Assert.Equal(payload, data);
        await Close(client, server);
    }

    [Fact]
    public async Task MessageStreamStartsAtZero()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var received = Wait.Signal<(long, long)>();
        client.OnData(s => received.TrySetResult((s.Position, s.Length)));

        client.SendData(Net.Payload(100));

        Assert.Equal((0L, 100L), await Wait.For(received, what: "position"));
        await Close(client, server);
    }

    [Fact]
    public async Task StreamIsDisposedAfterCallback()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var received = Wait.Signal<Stream>();
        client.OnData(s => received.TrySetResult(s));

        client.SendData(Net.Payload(100));

        var stream = await Wait.For(received, what: "stream");
        await Wait.Until(() => !stream.CanRead, what: "stream disposed");
        await Close(client, server);
    }

    [Fact]
    public async Task CustomStreamFactoryReceivesSize()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var sizes = new List<long>();
        var received = Wait.Signal<byte[]>();
        client.OnAllocate(size =>
        {
            lock (sizes) sizes.Add(size);
            return new MemoryStream();
        });
        client.OnData(s => received.TrySetResult(Read(s)));
        client.Open(host);
        await Wait.Until(() => client.IsOpened, what: "connect");

        var payload = Net.Payload(5000);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "echo"));
        lock (sizes) Assert.Contains(5000L, sizes);
        await Close(client, server);
    }

    [Fact]
    public async Task NoFraming_DeliversRawBytes()
    {
        var (server, host) = await EchoServer(false);
        var client = await Connect(host, false);
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        client.SendData(Net.Payload(10));

        Assert.Equal(10, (await Wait.For(received, what: "raw echo")).Length);
        await Close(client, server);
    }

    [Fact]
    public async Task Broadcast_ReachesAllClients()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "server open");

        var count = 0;
        var clients = new List<TcpClient>();

        for (var i = 0; i < 10; i++)
        {
            var client = await Connect(host);
            client.OnData(_ => Interlocked.Increment(ref count));
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Count == 10, what: "all accepted");

        server.BroadcastData(Net.Payload(64));
        await Wait.Until(() => Volatile.Read(ref count) == 10, what: "broadcast");

        foreach (var client in clients) await Closed(client);
        await Closed(server);
    }

    [Fact]
    public async Task ServerClose_DisconnectsClients()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        await Closed(server);

        Assert.True(await Wait.For(closed, what: "client observes close"));
        Assert.False(client.IsOpened);
        Assert.Empty(server.Clients);
    }

    [Fact]
    public async Task Reconnect_OldConnectionDoesNotCloseNewOne()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);

        await Closed(client);
        Assert.False(client.IsOpened);

        var opened = Wait.Signal<bool>();
        client.OnOpen(() => opened.TrySetResult(true));
        client.Open(host);
        Assert.True(await Wait.For(opened, what: "reconnect"));

        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));
        client.SendData(Net.Payload(32));

        Assert.Equal(32, (await Wait.For(received, what: "echo after reconnect")).Length);
        Assert.True(client.IsOpened);
        await Close(client, server);
    }

    [Fact]
    public async Task Open_CalledInsideCallbacks_IsNotIgnored()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var opens = 0;
        var done = Wait.Signal<bool>();

        // close on every open and open again on every close, 3 times
        client.OnOpen(() =>
        {
            if (Interlocked.Increment(ref opens) < 3) client.Close();
            else done.TrySetResult(true);
        });
        client.OnClose(() =>
        {
            if (Volatile.Read(ref opens) < 3) client.Open(host);
        });

        client.Open(host);

        Assert.True(await Wait.For(done, what: "3 opens"));
        await Close(client, server);
    }

    [Fact]
    public async Task ConnectToClosedPort_FailsWithoutThrowing()
    {
        var client = new TcpClient(true);
        var failed = Wait.Signal<Exception>();
        client.OnError(e => failed.TrySetResult(e));

        client.Open(Net.TcpHost());

        Assert.NotNull(await Wait.For(failed, what: "fail"));
        Assert.False(client.IsOpened);
        Assert.Null(client.Socket);
    }

    [Fact]
    public async Task OpenBusyPort_Fails()
    {
        var (server, host) = await EchoServer();
        var other = new TcpServer(true);
        var failed = Wait.Signal<Exception>();
        other.OnError(e => failed.TrySetResult(e));

        other.Open(host);

        Assert.NotNull(await Wait.For(failed, what: "busy port"));
        Assert.False(other.IsOpened);
        await Closed(server);
    }

    [Fact]
    public async Task ServerOpenTwice_IsIgnored()
    {
        var (server, host) = await EchoServer();

        var errors = 0;
        server.OnError(_ => Interlocked.Increment(ref errors));

        server.Open(host);

        await Wait.Never(() => Volatile.Read(ref errors) > 0 || !server.IsOpened, TimeSpan.FromMilliseconds(200),
            "second Open must not fail or close the server");
        Assert.True(server.IsOpened);
        await Closed(server);
    }

    [Fact]
    public async Task ConcurrentSend_KeepsMessagesIntact()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var count = 0;
        var wrong = 0;
        client.OnData(s =>
        {
            if (!Read(s).SequenceEqual(Net.Payload(4096))) Interlocked.Increment(ref wrong);
            Interlocked.Increment(ref count);
        });

        var payload = Net.Payload(4096);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 25; i++) client.SendData(payload);
        })));

        await Wait.Until(() => Volatile.Read(ref count) == 200, what: "200 echoes");
        Assert.Equal(0, wrong);
        await Close(client, server);
    }

    [Fact]
    public async Task FramingSize_IsAppliedToServerClients()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true) { FramingSize = 2048 };
        var received = 0;
        var accepted = Wait.Signal<TcpClient>();
        server.OnAccept(c =>
        {
            c.OnData(_ => Interlocked.Increment(ref received));
            accepted.TrySetResult(c);
        });
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "open");

        var client = await Connect(host);
        var serverSide = await Wait.For(accepted, what: "accept");
        Assert.Equal(2048, serverSide.FramingSize);

        // bigger than FramingSize: server closes the connection
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.SendData(Net.Payload(100_000));

        Assert.True(await Wait.For(closed, what: "server drops oversized message"));
        Assert.Equal(0, received);
        await Closed(server);
    }

    [Fact]
    public async Task InvalidFramingPrefix_ClosesConnection()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host, false);
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));

        // server expects framing, raw garbage is invalid
        client.SendData(Net.Payload(64));

        Assert.True(await Wait.For(closed, what: "server drops garbage"));
        await Closed(server);
    }

    [Fact]
    public async Task UserCallbackThrowing_DoesNotKillConnection()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var calls = 0;
        var second = Wait.Signal<bool>();
        client.OnData(_ =>
        {
            if (Interlocked.Increment(ref calls) == 1) throw new InvalidOperationException("user error");
            second.TrySetResult(true);
        });

        client.SendData(Net.Payload(8));
        await Wait.Until(() => Volatile.Read(ref calls) == 1, what: "first");
        client.SendData(Net.Payload(8));

        Assert.True(await Wait.For(second, what: "second message"));
        Assert.True(client.IsOpened);
        await Close(client, server);
    }

    [Fact]
    public async Task ChangeFramingSizeConnected_Throws()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);

        Assert.Throws<InvalidOperationException>(() => client.FramingSize = 4096);
        Assert.Throws<InvalidOperationException>(() => client.ToSecure(true));
        await Close(client, server);
    }

    /* ---- SEND OVERLOADS --- */

    private static async Task<byte[]> Echo(Action<TcpClient> send, bool framing = true)
    {
        var (server, host) = await EchoServer(framing);
        var client = await Connect(host, framing);
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        send(client);

        var result = await Wait.For(received, what: "echo");
        await Close(client, server);
        return result;
    }

    [Fact]
    public async Task SendData_OffsetAndCount()
    {
        var data = Net.Payload(100);

        var result = await Echo(c => c.SendData(data, 10, 20));

        Assert.Equal(data.Skip(10).Take(20), result);
    }

    [Fact]
    public async Task SendData_ArraySegment()
    {
        var data = Net.Payload(100);

        var result = await Echo(c => c.SendData(new ArraySegment<byte>(data, 5, 50)));

        Assert.Equal(data.Skip(5).Take(50), result);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100_000)]
    public async Task SendData_Gather_JoinsSegments(int size)
    {
        var a = Net.Payload(size, 1);
        var b = Net.Payload(size, 2);

        var result = await Echo(c => c.SendData(new List<ArraySegment<byte>>
        {
            new(a), new(b, 0, size / 2)
        }));

        Assert.Equal(a.Concat(b.Take(size / 2)), result);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(300_000)]
    public async Task SendData_Stream_SendsFromPositionAndRestoresIt(int size)
    {
        var data = Net.Payload(size);
        var stream = new MemoryStream(data) { Position = 7 };

        var result = await Echo(c => c.SendData(stream));

        Assert.Equal(data.Skip(7), result);
        Assert.Equal(7, stream.Position);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task SendData_FileStream()
    {
        var path = Path.GetTempFileName();
        var data = Net.Payload(200_000);
        await File.WriteAllBytesAsync(path, data);

        try
        {
            using var file = File.OpenRead(path);
            var result = await Echo(c => c.SendData(file));
            Assert.Equal(data, result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SendData_WithoutFraming_Stream()
    {
        var data = Net.Payload(2000);

        var result = await Echo(c => c.SendData(new MemoryStream(data)), false);

        Assert.Equal(data, result);
    }

    [Fact]
    public async Task SendEvent_Overloads()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var events = new List<(string, int)>();
        client.OnEvent((name, s) =>
        {
            lock (events) events.Add((name, (int)s.Length));
        });

        var data = Net.Payload(300);
        client.SendEvent("a", data);
        client.SendEvent("b", data, 10, 20);
        client.SendEvent("c", new ArraySegment<byte>(data, 0, 30));
        client.SendEvent("d", new[] { new ArraySegment<byte>(data, 0, 10), new ArraySegment<byte>(data, 10, 15) });
        client.SendEvent("e", new MemoryStream(data));

        await Wait.Until(() => { lock (events) return events.Count == 5; }, what: "5 events");
        lock (events)
            Assert.Equal(new[] { ("a", 300), ("b", 20), ("c", 30), ("d", 25), ("e", 300) }, events.ToArray());

        await Close(client, server);
    }

    [Fact]
    public async Task SendEvent_NameOnly_IsValid()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var received = Wait.Signal<(string, long)>();
        client.OnEvent((name, s) => received.TrySetResult((name, s.Length)));

        client.SendEvent("ping");

        Assert.Equal(("ping", 0L), await Wait.For(received, what: "empty event"));
        await Close(client, server);
    }

    [Fact]
    public async Task SendData_Empty_IsIgnored()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var count = 0;
        client.OnData(_ => Interlocked.Increment(ref count));

        client.SendData(Array.Empty<byte>());
        client.SendData(new MemoryStream());
        client.SendData(new MemoryStream(new byte[] { 1 }) { Position = 1 });
        client.SendData(Net.Payload(5));

        await Wait.Until(() => Volatile.Read(ref count) == 1, what: "only the valid one");
        await Wait.Never(() => Volatile.Read(ref count) > 1, TimeSpan.FromMilliseconds(200), "empty data delivered");
        await Close(client, server);
    }

    [Fact]
    public async Task Send_InvalidArguments_Throw()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);
        var data = new byte[10];

        Assert.Throws<ArgumentNullException>(() => client.SendData((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => client.SendData((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => client.SendData((IList<ArraySegment<byte>>)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(data, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(data, 5, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(data, 0, -1));
        Assert.Throws<ArgumentException>(() => client.SendEvent("", data));
        Assert.Throws<ArgumentException>(() => client.SendEvent(null!));
        Assert.Throws<ArgumentException>(() => client.SendData(new NonSeekable()));
        Assert.Throws<ArgumentNullException>(() => client.Open(null!));
        Assert.Throws<ArgumentNullException>(() => server.Open(null!));

        Assert.True(client.IsOpened);
        await Close(client, server);
    }

    [Fact]
    public async Task Send_WhenClosed_IsIgnored()
    {
        var client = new TcpClient(true);

        client.SendData(Net.Payload(10));
        client.SendEvent("name", Net.Payload(10));
        client.SendEvent("name");

        Assert.False(client.IsOpened);
        await Closed(client);
    }

    /* ---- ALLOCATE --- */

    [Fact]
    public async Task OnAllocate_ReceivesMessageSizeAndStreamIsUsed()
    {
        var path = Path.GetTempFileName();
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var sizes = new List<long>();
        var received = Wait.Signal<byte[]>();

        client.OnAllocate(size =>
        {
            lock (sizes) sizes.Add(size);
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096,
                FileOptions.DeleteOnClose);
        });
        client.OnData(s => received.TrySetResult(Read(s)));
        client.Open(host);
        await Wait.Until(() => client.IsOpened, what: "connect");

        var payload = Net.Payload(100_000);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "echo through file"));
        lock (sizes) Assert.Contains(100_000L, sizes);
        await Wait.Until(() => !File.Exists(path), what: "file deleted after callback");
        await Close(client, server);
    }

    [Fact]
    public async Task ServerOnAllocate_IsUsedForClientData()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var sizes = new List<long>();
        var received = Wait.Signal<byte[]>();
        server.OnAllocate(size =>
        {
            lock (sizes) sizes.Add(size);
            return new MemoryStream();
        });
        server.OnAccept(c => c.OnData(s => received.TrySetResult(Read(s))));
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "open");

        var client = await Connect(host);
        var payload = Net.Payload(777);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "server data"));
        lock (sizes) Assert.Contains(777L, sizes);
        await Closed(client);
        await Closed(server);
    }

    [Fact]
    public async Task OnAllocate_WhileOpened_Throws()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);

        Assert.Throws<InvalidOperationException>(() => client.OnAllocate(size => new MemoryStream()));
        Assert.Throws<InvalidOperationException>(() => server.OnAllocate(size => new MemoryStream()));
        await Close(client, server);
    }

    /* ---- CALLBACKS --- */

    [Fact]
    public async Task OnOpenThrowing_DoesNotCloseConnection()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var errors = 0;
        client.OnOpen(() => throw new InvalidOperationException("user error"));
        client.OnError(_ => Interlocked.Increment(ref errors));
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        await Opened(client, host);
        client.SendData(Net.Payload(16));

        Assert.Equal(16, (await Wait.For(received, what: "echo")).Length);
        Assert.True(client.IsOpened);
        Assert.Equal(0, errors);
        await Close(client, server);
    }

    [Fact]
    public async Task SendInsideOnOpen_IsDelivered()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var received = Wait.Signal<byte[]>();
        client.OnOpen(() => client.SendData(Net.Payload(24)));
        client.OnData(s => received.TrySetResult(Read(s)));

        client.Open(host);

        Assert.Equal(24, (await Wait.For(received, what: "echo")).Length);
        await Close(client, server);
    }

    [Fact]
    public async Task ModifyReceivesSocketBeforeConnect()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var connectedAtModify = Wait.Signal<bool>();
        client.OnModify(socket => connectedAtModify.TrySetResult(socket.Connected));

        await Opened(client, host);

        Assert.False(await Wait.For(connectedAtModify, what: "modify"));
        Assert.True(client.IsOpened);
        await Close(client, server);
    }

    private sealed class NonSeekable : MemoryStream
    {
        public override bool CanSeek => false;
    }

    /* ---- REOPEN --- */

    private static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(30);

    [Fact]
    public void Reopen_IsDisabledByDefault()
    {
        var client = new TcpClient(true);

        Assert.False(client.ReopenEnabled);
        Assert.Equal(5, client.ReopenAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), client.ReopenDelay);
    }

    [Fact]
    public void UseAutoReopen_StoresSettings()
    {
        var client = new TcpClient(true);

        client.UseAutoReopen(true, -1, TimeSpan.FromMilliseconds(250));

        Assert.True(client.ReopenEnabled);
        Assert.Equal(-1, client.ReopenAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(250), client.ReopenDelay);
    }

    [Fact]
    public void UseAutoReopen_InvalidArguments_Throw()
    {
        var client = new TcpClient(true);

        Assert.Throws<ArgumentOutOfRangeException>(() => client.UseAutoReopen(true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.UseAutoReopen(true, -2));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.UseAutoReopen(true, 1, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task UseAutoReopen_WhileOpened_Throws()
    {
        var (server, host) = await EchoServer();
        var client = await Connect(host);

        Assert.Throws<InvalidOperationException>(() => client.UseAutoReopen(true));
        await Close(client, server);
    }

    [Fact]
    public async Task Reopen_AfterServerDropsTheClient()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var accepted = new SemaphoreSlim(0);
        var serverSide = new List<TcpClient>();
        server.OnAccept(c =>
        {
            lock (serverSide) serverSide.Add(c);
            c.OnData(s => c.SendData(Read(s)));
            accepted.Release();
        });
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "server open");

        var client = new TcpClient(true);
        var attempts = new List<int>();
        var closed = Wait.Signal<bool>();
        var reopened = Wait.Signal<bool>();
        var opens = 0;
        client.UseAutoReopen(true, 5, Fast);
        client.OnReopen(a =>
        {
            lock (attempts) attempts.Add(a);
        });
        client.OnClose(() => closed.TrySetResult(true));
        client.OnOpen(() =>
        {
            if (Interlocked.Increment(ref opens) == 2) reopened.TrySetResult(true);
        });
        client.Open(host);
        Assert.True(await accepted.WaitAsync(Wait.Default));

        // the server drops this client
        TcpClient first;
        lock (serverSide) first = serverSide[0];
        first.Close();

        Assert.True(await Wait.For(closed, what: "client observes drop"));
        Assert.True(await Wait.For(reopened, what: "client opens again"));
        Assert.True(await accepted.WaitAsync(Wait.Default));
        lock (attempts) Assert.Equal(new[] { 1 }, attempts.ToArray());

        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));
        client.SendData(Net.Payload(40));

        Assert.Equal(40, (await Wait.For(received, what: "echo after reopen")).Length);
        await Closed(client);
        await Closed(server);
    }

    [Fact]
    public async Task Reopen_GivesUpAfterTheAttemptLimit()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var attempts = new List<int>();
        var errors = 0;
        client.UseAutoReopen(true, 3, Fast);
        client.OnReopen(a =>
        {
            lock (attempts) attempts.Add(a);
        });
        client.OnError(_ => Interlocked.Increment(ref errors));
        await Opened(client, host);
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        // port is gone, every attempt fails
        await Closed(server);

        Assert.True(await Wait.For(closed, what: "client observes close"));
        await Wait.Until(() => Volatile.Read(ref errors) >= 3, what: "3 failed attempts");
        await Wait.Never(() => { lock (attempts) return attempts.Count > 3; }, TimeSpan.FromMilliseconds(300),
            "more than 3 attempts");

        lock (attempts) Assert.Equal(new[] { 1, 2, 3 }, attempts.ToArray());
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task Reopen_UnlimitedAttempts_ConnectsWhenServerComesBack()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        client.UseAutoReopen(true, -1, Fast);
        await Opened(client, host);
        var reopened = Wait.Signal<bool>();
        var attempts = 0;
        client.OnReopen(_ => Interlocked.Increment(ref attempts));
        client.OnOpen(() => reopened.TrySetResult(true));
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        await Closed(server);
        await Wait.Until(() => Volatile.Read(ref attempts) >= 2, what: "failed attempts while server is down");

        var back = new TcpServer(true);
        back.Open(host);

        Assert.True(await Wait.For(reopened, what: "reopen after server is back"));
        Assert.True(client.IsOpened);
        await Closed(client);
        await Closed(back);
    }

    [Fact]
    public async Task Reopen_NotAfterUserClose()
    {
        var (server, host) = await EchoServer();
        var client = new TcpClient(true);
        var attempts = 0;
        client.UseAutoReopen(true, 5, Fast);
        client.OnReopen(_ => Interlocked.Increment(ref attempts));
        await Opened(client, host);

        await Closed(client);

        await Wait.Never(() => Volatile.Read(ref attempts) > 0 || client.IsOpened, TimeSpan.FromMilliseconds(400),
            "reopen after Close");
        await Closed(server);
    }

    [Fact]
    public async Task Reopen_CloseDuringTheWaitCancelsIt()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var serverSide = Wait.Signal<TcpClient>();
        server.OnAccept(c => serverSide.TrySetResult(c));
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "server open");

        var client = new TcpClient(true);
        var attempts = 0;
        var closed = Wait.Signal<bool>();
        client.UseAutoReopen(true, 5, TimeSpan.FromMilliseconds(300));
        client.OnReopen(_ => Interlocked.Increment(ref attempts));
        client.OnClose(() => closed.TrySetResult(true));
        await Opened(client, host);

        (await Wait.For(serverSide, what: "accept")).Close();
        Assert.True(await Wait.For(closed, what: "drop"));

        // user gives up while the client waits to open again
        client.Close();

        await Wait.Never(() => Volatile.Read(ref attempts) > 0 || client.IsOpened, TimeSpan.FromMilliseconds(700),
            "reopen after Close during the wait");
        await Closed(server);
    }

    [Fact]
    public async Task Reopen_DisabledClientStaysClosed()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var serverSide = Wait.Signal<TcpClient>();
        server.OnAccept(c => serverSide.TrySetResult(c));
        server.Open(host);
        await Wait.Until(() => server.IsOpened, what: "server open");

        var client = new TcpClient(true);
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await Opened(client, host);

        (await Wait.For(serverSide, what: "accept")).Close();
        Assert.True(await Wait.For(closed, what: "drop"));

        await Wait.Never(() => client.IsOpened, TimeSpan.FromMilliseconds(300), "client opened without reopen");
        await Closed(server);
    }

    /* ---- TLS --- */

    [Fact]
    public async Task Tls_Echo()
    {
        var (server, host) = await EchoServer(secure: true);
        var client = await Connect(host, secure: true);
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        var payload = Net.Payload(256 * 1024);
        client.SendData(payload);

        Assert.True(client.IsSecure);
        Assert.Equal(payload, await Wait.For(received, what: "tls echo"));
        await Close(client, server);
    }

    [Fact]
    public async Task Tls_UntrustedCertificateIsRejectedByDefault()
    {
        var (server, host) = await EchoServer(secure: true);
        var client = new TcpClient(true);
        var failed = Wait.Signal<Exception>();
        client.ToSecure(true);
        client.OnError(e => failed.TrySetResult(e));

        client.Open(host);

        Assert.NotNull(await Wait.For(failed, what: "reject self-signed"));
        Assert.False(client.IsOpened);
        await Closed(server);
    }

    [Fact]
    public async Task Tls_CallbackCanReject()
    {
        var (server, host) = await EchoServer(secure: true);
        var client = new TcpClient(true);
        var failed = Wait.Signal<Exception>();
        client.ToSecure(true);
        client.OnSecure((_, _, _) => false);
        client.OnError(e => failed.TrySetResult(e));

        client.Open(host);

        Assert.NotNull(await Wait.For(failed, what: "callback rejects"));
        Assert.False(client.IsOpened);
        await Closed(server);
    }

    [Fact]
    public async Task Tls_PlainClientIsRejectedByDefault()
    {
        var (server, host) = await EchoServer(secure: true);
        var client = new TcpClient(true);
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.Open(host);
        await Wait.Until(() => client.IsOpened, what: "tcp connect");

        client.SendData(Net.Payload(64));

        Assert.True(await Wait.For(closed, what: "server drops plain client"));
        Assert.Empty(server.Clients);
        await Closed(server);
    }

    [Fact]
    public async Task Tls_PlainClientIsAcceptedWhenAllowed()
    {
        var (server, host) = await EchoServer(secure: true, allowUnsecured: true);
        var client = await Connect(host);
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        client.SendData(Net.Payload(64));

        Assert.Equal(64, (await Wait.For(received, what: "plain echo")).Length);
        await Close(client, server);
    }
}
