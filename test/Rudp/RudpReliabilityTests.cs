using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpReliabilityTests
{
    private static async Task<(RudpServer server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(client =>
        {
            client.OnData((stream, type) => client.SendData(stream.GetBytes(), type));
        });
        await server.OpenAsync(host);
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
        var client = new RudpClient();
        var received = new List<byte[]>();
        client.OnData((stream, _) =>
        {
            lock (received) received.Add(stream.GetBytes());
        });

        await client.OpenAsync(target);
        Assert.True(client.IsOpened, "handshake");

        foreach (var message in messages) client.SendData(message, RudpDelivery.Reliable);

        await Wait.Until(() => { lock (received) return received.Count >= messages.Count; }, timeout,
            $"{messages.Count} messages");
        await Task.Delay(300);

        lock (received)
        {
            Assert.Equal(messages.Count, received.Count);
            for (var i = 0; i < messages.Count; i++) Assert.Equal(messages[i], received[i]);
        }

        await client.CloseAsync();
    }

    [Fact]
    public Task ManySmallMessagesInOrder() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();

        await AssertReliableEcho(host, Messages(1000, 4, 200, 1), TimeSpan.FromSeconds(20));

        await server.CloseAsync();
    });

    [Theory]
    [InlineData(1024 * 1024)]
    [InlineData(8 * 1024 * 1024)]
    public Task BigMessageIsFragmentedAndReassembled(int size) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();

        await AssertReliableEcho(host, new List<byte[]> { Net.Payload(size, 3) }, TimeSpan.FromSeconds(60));

        await server.CloseAsync();
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

        await server.CloseAsync();
    }, TimeSpan.FromSeconds(90));

    [Fact]
    public Task SurvivesReorderingAndDuplicates() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host, 0.02, 0.25, 40, 9);

        await AssertReliableEcho(proxy.Host, Messages(300, 4, 2500, 3), TimeSpan.FromSeconds(60));

        await server.CloseAsync();
    }, TimeSpan.FromSeconds(90));

    [Fact]
    public Task HandshakeSurvivesLoss() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        using var proxy = new UdpProxy(host, 0.5, seed: 4);
        var client = new RudpClient { HandshakeTimeout = 10000 };

        await client.OpenAsync(proxy.Host);

        Assert.True(client.IsOpened);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task BothSidesSendAtTheSameTime() => Guard.Run(async () =>
    {
        const int total = 500;
        var host = Net.UdpHost();
        var server = new RudpServer();
        var serverReceived = new List<int>();
        RudpClient? peer = null;
        server.OnAccept(c =>
        {
            peer = c;
            c.OnData((stream, _) =>
            {
                lock (serverReceived) serverReceived.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
            });
        });
        await server.OpenAsync(host);

        var client = new RudpClient();
        var clientReceived = new List<int>();
        client.OnData((stream, _) =>
        {
            lock (clientReceived) clientReceived.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
        });
        await client.OpenAsync(host);
        await Wait.Until(() => peer != null, what: "accept");

        var a = Task.Run(() =>
        {
            for (var i = 0; i < total; i++) client.SendData(BitConverter.GetBytes(i), RudpDelivery.Reliable);
        });
        var b = Task.Run(() =>
        {
            for (var i = 0; i < total; i++) peer!.SendData(BitConverter.GetBytes(i), RudpDelivery.Reliable);
        });
        await Task.WhenAll(a, b);

        await Wait.Until(() => { lock (serverReceived) lock (clientReceived) return serverReceived.Count == total && clientReceived.Count == total; },
            TimeSpan.FromSeconds(20), "both directions");

        lock (serverReceived) Assert.Equal(Enumerable.Range(0, total), serverReceived);
        lock (clientReceived) Assert.Equal(Enumerable.Range(0, total), clientReceived);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task ConcurrentSendersDoNotCorruptMessages() => Guard.Run(async () =>
    {
        const int senders = 8, perSender = 100;
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = new List<byte[]>();
        client.OnData((stream, _) =>
        {
            lock (received) received.Add(stream.GetBytes());
        });
        await client.OpenAsync(host);

        var expected = Messages(senders * perSender, 10, 4000, 6);

        await Task.WhenAll(Enumerable.Range(0, senders).Select(s => Task.Run(() =>
        {
            for (var m = 0; m < perSender; m++) client.SendData(expected[s * perSender + m], RudpDelivery.Reliable);
        })));

        await Wait.Until(() => { lock (received) return received.Count == expected.Count; }, TimeSpan.FromSeconds(30), "all messages");

        lock (received)
        {
            var remaining = received.Select(Convert.ToBase64String).ToList();
            foreach (var message in expected) Assert.True(remaining.Remove(Convert.ToBase64String(message)));
            Assert.Empty(remaining);
        }

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task SequencedNeverGoesBackwards() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        var received = new List<int>();
        server.OnAccept(c => c.OnData((stream, type) =>
        {
            Assert.Equal(RudpDelivery.Sequenced, type);
            lock (received) received.Add(BitConverter.ToInt32(stream.GetBytes(), 0));
        }));
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host, 0.1, 0.2, 30, 5);
        var client = new RudpClient();
        await client.OpenAsync(proxy.Host);

        for (var i = 1; i <= 300; i++) client.SendData(BitConverter.GetBytes(i), RudpDelivery.Sequenced);

        await Task.Delay(1000);

        lock (received)
        {
            Assert.NotEmpty(received);
            for (var i = 1; i < received.Count; i++) Assert.True(received[i] > received[i - 1], $"{received[i - 1]} then {received[i]}");
        }

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Theory]
    [InlineData(RudpDelivery.Unreliable)]
    [InlineData(RudpDelivery.Sequenced)]
    public Task BigDatagramMessageIsFragmented(RudpDelivery type) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = Wait.Signal<byte[]>();
        client.OnData((stream, _) => received.TrySetResult(stream.GetBytes()));
        await client.OpenAsync(host);

        var payload = Net.Payload(100 * 1024, 8);
        client.SendData(payload, type);

        Assert.Equal(payload, await Wait.For(received, what: "fragmented datagram echo"));

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task EventsKeepNameAndPayloadUnderLoss() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(c => c.OnEvent((name, stream, type) => c.SendEvent(name, stream.GetBytes(), type)));
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host, 0.1, seed: 12);
        var client = new RudpClient();
        var received = new List<(string name, byte[] data)>();
        client.OnEvent((name, stream, _) =>
        {
            lock (received) received.Add((name, stream.GetBytes()));
        });
        await client.OpenAsync(proxy.Host);

        var payloads = Messages(100, 5, 3000, 14);
        for (var i = 0; i < payloads.Count; i++) client.SendEvent($"event.{i}", payloads[i], RudpDelivery.Reliable);

        await Wait.Until(() => { lock (received) return received.Count == payloads.Count; }, TimeSpan.FromSeconds(30), "events");

        lock (received)
            for (var i = 0; i < payloads.Count; i++)
            {
                Assert.Equal($"event.{i}", received[i].name);
                Assert.Equal(payloads[i], received[i].data);
            }

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task OverheadIsSmall() => Guard.Run(async () =>
    {
        const int size = 4 * 1024 * 1024;
        var host = Net.UdpHost();
        var server = new RudpServer();
        var done = Wait.Signal<bool>();
        server.OnAccept(c => c.OnData((stream, _) =>
        {
            if (stream.Length == size) done.TrySetResult(true);
        }));
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host);
        var client = new RudpClient();
        await client.OpenAsync(proxy.Host);

        client.SendData(Net.Payload(size, 2), RudpDelivery.Reliable);

        await Wait.For(done, TimeSpan.FromSeconds(30), "transfer");
        await Task.Delay(200);

        // data direction: payload plus headers (21 bytes by packet), at most 2% extra
        Assert.True(proxy.ToServerBytes < size * 1.02, $"to server: {proxy.ToServerBytes} bytes for {size}");
        // ack direction: cumulative acks, at most 1% of the data
        Assert.True(proxy.ToClientBytes < size * 0.01, $"to client: {proxy.ToClientBytes} bytes for {size}");

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task OverheadIsSmall_WhenThePeerHasASmallReceiveBuffer() => Guard.Run(async () =>
    {
        // linux runners cap the receive buffer (rmem_max 208 KB): a burst above it is dropped and sent again
        const int size = 4 * 1024 * 1024;
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(c => c.OnOpen(() => c.SendData(Net.Payload(size, 3), RudpDelivery.Reliable)));
        await server.OpenAsync(host);

        using var proxy = new UdpProxy(host);
        var done = Wait.Signal<bool>();
        var client = new RudpClient();
        client.OnModify(socket => socket.ReceiveBufferSize = 106496);
        client.OnData((stream, _) =>
        {
            if (stream.Length == size) done.TrySetResult(true);
        });
        await client.OpenAsync(proxy.Host);

        await Wait.For(done, TimeSpan.FromSeconds(30), "transfer");

        Assert.True(proxy.ToClientBytes < size * 1.02, $"to client: {proxy.ToClientBytes} bytes for {size}");

        await client.CloseAsync();
        await server.CloseAsync();
    });
}
