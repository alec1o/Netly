using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpReliabilityTests
{
    private static async Task<(RUDP.Server server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        server.On.Accept(client =>
        {
            client.On.Data((stream, type) => client.To.Data(stream.GetBytes(), type));
        });
        await server.To.Open(host);
        return (server, host);
    }

    private static List<byte[]> Messages(int count, int minSize, int maxSize, int seed)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, count).Select(i =>
        {
            var data = new byte[random.Next(minSize, maxSize)];
            random.NextBytes(data);
            BitConverter.GetBytes(i).CopyTo(data, 0);
            return data;
        }).ToList();
    }

    // sends everything and checks that the echo arrives complete, once and in order
    private static async Task AssertReliableEcho(Host target, List<byte[]> messages, TimeSpan timeout)
    {
        var client = new RUDP.Client();
        var received = new List<byte[]>();
        client.On.Data((stream, _) =>
        {
            lock (received) received.Add(stream.GetBytes());
        });

        await client.To.Open(target);
        Assert.True(client.IsOpened, "handshake");

        foreach (var message in messages) client.To.Data(message, RUDP.MessageType.Reliable);

        await Wait.Until(() => { lock (received) return received.Count >= messages.Count; }, timeout,
            $"{messages.Count} messages");
        await Task.Delay(300);

        lock (received)
        {
            Assert.Equal(messages.Count, received.Count);
            for (var i = 0; i < messages.Count; i++) Assert.Equal(messages[i], received[i]);
        }

        await client.To.Close();
    }

    [Fact]
    public Task ManySmallMessagesInOrder() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();

        await AssertReliableEcho(host, Messages(1000, 4, 200, 1), TimeSpan.FromSeconds(20));

        await server.To.Close();
    });

    [Theory]
    [InlineData(1024 * 1024)]
    [InlineData(8 * 1024 * 1024)]
    public Task BigMessageIsFragmentedAndReassembled(int size) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();

        await AssertReliableEcho(host, new List<byte[]> { Net.Payload(size, 3) }, TimeSpan.FromSeconds(60));

        await server.To.Close();
    }, TimeSpan.FromSeconds(90));

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.15)]
    [InlineData(0.30)]
    public Task SurvivesPacketLoss(double loss) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host, loss, seed: 7);
        var messages = Messages(200, 4, 3000, 2);
        messages.Add(Net.Payload(256 * 1024, 5));

        await AssertReliableEcho(proxy.Host, messages, TimeSpan.FromSeconds(60));

        await server.To.Close();
    }, TimeSpan.FromSeconds(90));

    [Fact]
    public Task SurvivesReorderingAndDuplicates() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host, 0.02, 0.25, 40, 9);

        await AssertReliableEcho(proxy.Host, Messages(300, 4, 2500, 3), TimeSpan.FromSeconds(60));

        await server.To.Close();
    }, TimeSpan.FromSeconds(90));

    [Fact]
    public Task HandshakeSurvivesLoss() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host, 0.5, seed: 4);
        var client = new RUDP.Client { HandshakeTimeout = 10000 };

        await client.To.Open(proxy.Host);

        Assert.True(client.IsOpened);

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task BothSidesSendAtTheSameTime() => Guard.Run(async () =>
    {
        const int total = 500;
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        var serverReceived = new List<int>();
        global::Netly.Interfaces.IRUDP.Client? peer = null;
        server.On.Accept(c =>
        {
            peer = c;
            c.On.Data((stream, _) =>
            {
                lock (serverReceived) serverReceived.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
            });
        });
        await server.To.Open(host);

        var client = new RUDP.Client();
        var clientReceived = new List<int>();
        client.On.Data((stream, _) =>
        {
            lock (clientReceived) clientReceived.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
        });
        await client.To.Open(host);
        await Wait.Until(() => peer != null, what: "accept");

        var a = Task.Run(() =>
        {
            for (var i = 0; i < total; i++) client.To.Data(BitConverter.GetBytes(i), RUDP.MessageType.Reliable);
        });
        var b = Task.Run(() =>
        {
            for (var i = 0; i < total; i++) peer!.To.Data(BitConverter.GetBytes(i), RUDP.MessageType.Reliable);
        });
        await Task.WhenAll(a, b);

        await Wait.Until(() => { lock (serverReceived) lock (clientReceived) return serverReceived.Count == total && clientReceived.Count == total; },
            TimeSpan.FromSeconds(20), "both directions");

        lock (serverReceived) Assert.Equal(Enumerable.Range(0, total), serverReceived);
        lock (clientReceived) Assert.Equal(Enumerable.Range(0, total), clientReceived);

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task ConcurrentSendersDoNotCorruptMessages() => Guard.Run(async () =>
    {
        const int senders = 8, perSender = 100;
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = new List<byte[]>();
        client.On.Data((stream, _) =>
        {
            lock (received) received.Add(stream.GetBytes());
        });
        await client.To.Open(host);

        var expected = Messages(senders * perSender, 10, 4000, 6);

        await Task.WhenAll(Enumerable.Range(0, senders).Select(s => Task.Run(() =>
        {
            for (var m = 0; m < perSender; m++) client.To.Data(expected[s * perSender + m], RUDP.MessageType.Reliable);
        })));

        await Wait.Until(() => { lock (received) return received.Count == expected.Count; }, TimeSpan.FromSeconds(30), "all messages");

        lock (received)
        {
            var remaining = received.Select(Convert.ToBase64String).ToList();
            foreach (var message in expected) Assert.True(remaining.Remove(Convert.ToBase64String(message)));
            Assert.Empty(remaining);
        }

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task SequencedNeverGoesBackwards() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        var received = new List<int>();
        server.On.Accept(c => c.On.Data((stream, type) =>
        {
            Assert.Equal(RUDP.MessageType.Sequenced, type);
            lock (received) received.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
        }));
        await server.To.Open(host);

        using var proxy = new UdpProxy(host, 0.1, 0.2, 30, 5);
        var client = new RUDP.Client();
        await client.To.Open(proxy.Host);

        for (var i = 1; i <= 300; i++) client.To.Data(BitConverter.GetBytes(i), RUDP.MessageType.Sequenced);

        await Task.Delay(1000);

        lock (received)
        {
            Assert.NotEmpty(received);
            for (var i = 1; i < received.Count; i++) Assert.True(received[i] > received[i - 1], $"{received[i - 1]} then {received[i]}");
        }

        await client.To.Close();
        await server.To.Close();
    });

    [Theory]
    [InlineData(RUDP.MessageType.Unreliable)]
    [InlineData(RUDP.MessageType.Sequenced)]
    public Task BigDatagramMessageIsFragmented(RUDP.MessageType type) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RUDP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Data((stream, _) => received.TrySetResult(stream.GetBytes()));
        await client.To.Open(host);

        var payload = Net.Payload(100 * 1024, 8);
        client.To.Data(payload, type);

        Assert.Equal(payload, await Wait.For(received, what: "fragmented datagram echo"));

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task EventsKeepNameAndPayloadUnderLoss() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        server.On.Accept(c => c.On.Event((name, stream, type) => c.To.Event(name, stream.GetBytes(), type)));
        await server.To.Open(host);

        using var proxy = new UdpProxy(host, 0.1, seed: 12);
        var client = new RUDP.Client();
        var received = new List<(string name, byte[] data)>();
        client.On.Event((name, stream, _) =>
        {
            lock (received) received.Add((name, stream.GetBytes()));
        });
        await client.To.Open(proxy.Host);

        var payloads = Messages(100, 5, 3000, 14);
        for (var i = 0; i < payloads.Count; i++) client.To.Event($"event.{i}", payloads[i], RUDP.MessageType.Reliable);

        await Wait.Until(() => { lock (received) return received.Count == payloads.Count; }, TimeSpan.FromSeconds(30), "events");

        lock (received)
            for (var i = 0; i < payloads.Count; i++)
            {
                Assert.Equal($"event.{i}", received[i].name);
                Assert.Equal(payloads[i], received[i].data);
            }

        await client.To.Close();
        await server.To.Close();
    });

    [Fact]
    public Task OverheadIsSmall() => Guard.Run(async () =>
    {
        const int size = 4 * 1024 * 1024;
        var host = Net.UdpHost();
        var server = new RUDP.Server();
        var done = Wait.Signal<bool>();
        server.On.Accept(c => c.On.Data((stream, _) =>
        {
            if (stream.Length == size) done.TrySetResult(true);
        }));
        await server.To.Open(host);

        using var proxy = new UdpProxy(host);
        var client = new RUDP.Client();
        await client.To.Open(proxy.Host);

        client.To.Data(Net.Payload(size, 2), RUDP.MessageType.Reliable);

        await Wait.For(done, TimeSpan.FromSeconds(30), "transfer");
        await Task.Delay(200);

        // data direction: payload plus headers (21 bytes by packet), at most 2% extra
        Assert.True(proxy.ToServerBytes < size * 1.02, $"to server: {proxy.ToServerBytes} bytes for {size}");
        // ack direction: cumulative acks, at most 1% of the data
        Assert.True(proxy.ToClientBytes < size * 0.01, $"to client: {proxy.ToClientBytes} bytes for {size}");

        await client.To.Close();
        await server.To.Close();
    });
}
