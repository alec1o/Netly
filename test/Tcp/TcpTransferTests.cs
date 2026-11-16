using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpTransferTests
{
    private static async Task<(TCP.Server server, Host host)> EchoServer(bool framing = true)
    {
        var host = Net.TcpHost();
        var server = new TCP.Server(framing);
        server.On.Accept(client =>
        {
            client.On.Data(stream => client.To.Data(stream.GetBytes()));
            client.On.Event((name, stream) => client.To.Event(name, stream.GetBytes()));
        });
        await server.To.Open(host);
        return (server, host);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(36)]
    [InlineData(1024)]
    [InlineData(64 * 1024)]
    [InlineData(1024 * 1024)]
    public async Task EchoData(int size)
    {
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Data(s => received.TrySetResult(s.GetBytes()));
        await client.To.Open(host);

        var payload = Net.Payload(size);
        client.To.Data(payload);

        Assert.Equal(payload, await Wait.For(received, what: $"echo of {size} bytes"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task SmallMessagesPingPongIsFast()
    {
        const int rounds = 100;
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var pong = new SemaphoreSlim(0);
        client.On.Data(_ => pong.Release());
        await client.To.Open(host);

        var watch = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < rounds; i++)
        {
            client.To.Data("ping");
            Assert.True(await pong.WaitAsync(TimeSpan.FromSeconds(5)), $"round {i}");
        }

        // nagle + delayed ack made every round about 80 ms
        Assert.True(watch.ElapsedMilliseconds / rounds < 20, $"{watch.ElapsedMilliseconds / rounds} ms by round trip");

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task EchoEvent()
    {
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var received = Wait.Signal<(string name, byte[] data)>();
        client.On.Event((name, s) => received.TrySetResult((name, s.GetBytes())));
        await client.To.Open(host);

        var payload = Net.Payload(300, 7);
        client.To.Event("player.move", payload);

        var (name, data) = await Wait.For(received, what: "event echo");
        Assert.Equal("player.move", name);
        Assert.Equal(payload, data);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task StringOverloadsUseUtf8ByDefault()
    {
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var data = Wait.Signal<string>();
        var eventData = Wait.Signal<string>();
        client.On.Data(s => data.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));
        client.On.Event((_, s) => eventData.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));
        await client.To.Open(host);

        client.To.Data("olá, mundo ✓");
        client.To.Event("greet", "olá, evento ✓");

        Assert.Equal("olá, mundo ✓", await Wait.For(data));
        Assert.Equal("olá, evento ✓", await Wait.For(eventData));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task FramingKeepsMessageBoundaries()
    {
        const int total = 200;
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var messages = new List<byte[]>();
        client.On.Data(s =>
        {
            lock (messages) messages.Add(s.GetBytes());
        });
        await client.To.Open(host);

        var sent = Enumerable.Range(1, total).Select(i => Net.Payload(i, i)).ToList();
        foreach (var message in sent) client.To.Data(message);

        await Wait.Until(() => { lock (messages) return messages.Count == total; }, what: "all framed messages");

        // order and boundaries preserved
        lock (messages)
        {
            for (var i = 0; i < total; i++) Assert.Equal(sent[i], messages[i]);
        }

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ConcurrentSendersDoNotCorruptMessages()
    {
        const int senders = 8, perSender = 50;
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var received = new List<byte[]>();
        client.On.Data(s =>
        {
            lock (received) received.Add(s.GetBytes());
        });
        await client.To.Open(host);

        var expected = new List<byte[]>();
        for (var s = 0; s < senders; s++)
        for (var m = 0; m < perSender; m++)
            expected.Add(Net.Payload(100 + m, s * 1000 + m));

        await Task.WhenAll(Enumerable.Range(0, senders).Select(s => Task.Run(() =>
        {
            for (var m = 0; m < perSender; m++) client.To.Data(expected[s * perSender + m]);
        })));

        await Wait.Until(() => { lock (received) return received.Count == expected.Count; }, what: "all concurrent messages");

        // each sent message must arrive intact (order across senders is not defined)
        lock (received)
        {
            var remaining = received.Select(Convert.ToBase64String).ToList();
            foreach (var message in expected) Assert.True(remaining.Remove(Convert.ToBase64String(message)));
            Assert.Empty(remaining);
        }

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task EmptyPayloadsAreIgnored()
    {
        var (server, host) = await EchoServer();
        var client = new TCP.Client();
        var count = 0;
        client.On.Data(_ => Interlocked.Increment(ref count));
        client.On.Error(e => throw e);
        await client.To.Open(host);

        client.To.Data(Array.Empty<byte>());
        client.To.Data(string.Empty);
        client.To.Data((byte[])null!);

        await Wait.Never(() => count > 0, TimeSpan.FromMilliseconds(300), "data callback for empty payload");

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task BroadcastReachesEveryClient()
    {
        const int total = 10;
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var clients = new List<TCP.Client>();
        var dataCount = 0;
        var eventCount = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new TCP.Client();
            client.On.Data(s =>
            {
                Assert.Equal("broadcast", Encoding.UTF8.GetString(s.GetBytes()));
                Interlocked.Increment(ref dataCount);
            });
            client.On.Event((name, s) =>
            {
                Assert.Equal("news", name);
                Assert.Equal("payload", Encoding.UTF8.GetString(s.GetBytes()));
                Interlocked.Increment(ref eventCount);
            });
            await client.To.Open(host);
            clients.Add(client);
        }

        await Wait.Until(() => server.Clients.Count == total, what: "server accepted all");

        server.To.DataBroadcast("broadcast");
        server.To.EventBroadcast("news", "payload");

        await Wait.Until(() => dataCount == total && eventCount == total, what: "broadcast delivered");

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }

    [Fact]
    public async Task ServerToClientWithoutFraming()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server(false);
        var accepted = Wait.Signal<global::Netly.Interfaces.ITCP.Client>();
        server.On.Accept(c => accepted.TrySetResult(c));
        await server.To.Open(host);

        var client = new TCP.Client(false);
        var received = new MemoryStream();
        client.On.Data(s =>
        {
            lock (received) received.Write(s.GetBytes(), 0, (int)s.Length);
        });
        await client.To.Open(host);

        var serverSide = await Wait.For(accepted);
        var payload = Net.Payload(4096, 3);
        serverSide.To.Data(payload);

        // without framing the stream may be split, only the sequence of bytes is guaranteed.
        await Wait.Until(() => { lock (received) return received.Length == payload.Length; }, what: "raw bytes");
        lock (received) Assert.Equal(payload, received.ToArray());

        await client.To.Close();
        await server.To.Close();
    }
}
