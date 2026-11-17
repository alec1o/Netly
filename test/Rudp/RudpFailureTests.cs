using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpFailureTests
{
    private static async Task<(RudpServer server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(client => client.OnData((stream, type) => client.SendData(stream.GetBytes(), type)));
        await server.OpenAsync(host);
        return (server, host);
    }

    [Fact]
    public void Wire_IsBigEndianAndVersionThree()
    {
        var buffer = new byte[8];

        RudpWire.Put16(buffer, 0, 0x0102);
        Assert.Equal(new byte[] { 0x01, 0x02 }, buffer.Take(2));
        Assert.Equal(0x0102, RudpWire.Get16(buffer, 0));

        RudpWire.Put32(buffer, 0, 0x01020304);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, buffer.Take(4));
        Assert.Equal(0x01020304u, RudpWire.Get32(buffer, 0));

        RudpWire.Put64(buffer, 0, 0x0102030405060708);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, buffer);
        Assert.Equal(0x0102030405060708UL, RudpWire.Get64(buffer, 0));

        Assert.Equal(3, RudpWire.Version);
    }

    [Theory]
    [InlineData(RudpDelivery.Reliable, 10)]
    [InlineData(RudpDelivery.Reliable, 100_000)]
    [InlineData(RudpDelivery.Unreliable, 10)]
    [InlineData(RudpDelivery.Sequenced, 10)]
    public Task MessageWithFramingHeader_IsDelivered(RudpDelivery delivery, int size) => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var received = Wait.Signal<byte[]>();
        var payload = Net.Payload(size);

        client.OnData((stream, _) => received.TrySetResult(stream.GetBytes()));
        await client.OpenAsync(host);
        client.SendData(payload, delivery);

        Assert.Equal(payload, await Wait.For(received, what: "echo"));

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task UserCallbackThrowing_CallsOnErrorThenOnClose() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });
        client.OnData((_, _) => throw new InvalidOperationException("user callback"));
        await client.OpenAsync(host);

        client.SendData(Net.Payload(8), RudpDelivery.Reliable);

        Assert.True(await Wait.For(closed, what: "close after the exception of the app"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);
        Assert.False(client.IsOpened);

        await server.CloseAsync();
    });

    [Fact]
    public Task AllocationThrowing_CallsOnErrorThenOnClose() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient();
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        client.OnAllocate(_ => throw new IOException("disk full"));
        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });
        await client.OpenAsync(host);

        client.SendData(Net.Payload(8), RudpDelivery.Reliable);

        Assert.True(await Wait.For(closed, what: "close after the allocation failed"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    });

    [Fact]
    public Task OpenFailure_CallsOnErrorThenOnClose() => Guard.Run(async () =>
    {
        var client = new RudpClient { HandshakeTimeout = 1000 };
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });

        client.Open(Net.UdpHost());

        Assert.True(await Wait.For(closed, what: "close after the open failed"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);
    });

    [Fact]
    public void MaxPendingSize_DefaultAndValidation()
    {
        var client = new RudpClient();
        var server = new RudpServer();

        Assert.Equal(64L * 1024 * 1024, client.MaxPendingSize);
        Assert.Equal(64L * 1024 * 1024, server.MaxPendingSize);

        client.MaxPendingSize = 10;
        server.MaxPendingSize = 8192;

        // minimum of 1 KB
        Assert.Equal(1024, client.MaxPendingSize);
        Assert.Equal(8192, server.MaxPendingSize);
    }

    [Fact]
    public Task MaxPendingSize_CannotChangeWhileOpened_AndServerGivesItToItsClients() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer { MaxPendingSize = 4096 };
        await server.OpenAsync(host);
        var client = new RudpClient();
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        Assert.Throws<InvalidOperationException>(() => server.MaxPendingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => client.MaxPendingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => server.Clients.Single().MaxPendingSize = 2048);
        Assert.Equal(4096, server.Clients.Single().MaxPendingSize);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task SendAboveMaxPendingSize_CallsOnErrorThenOnClose() => Guard.Run(async () =>
    {
        var (server, host) = await EchoServer();
        var client = new RudpClient { MaxPendingSize = 2048 };
        var events = new List<string>();
        var closed = Wait.Signal<bool>();

        client.OnError(_ => { lock (events) events.Add("error"); });
        client.OnClose(() =>
        {
            lock (events) events.Add("close");
            closed.TrySetResult(true);
        });
        await client.OpenAsync(host);

        // smaller than the queue works
        client.SendData(Net.Payload(500), RudpDelivery.Reliable);

        // bigger than the queue never fits
        client.SendData(Net.Payload(5000), RudpDelivery.Reliable);

        Assert.True(await Wait.For(closed, what: "close after the queue limit"));
        lock (events) Assert.Equal(new[] { "error", "close" }, events);

        await server.CloseAsync();
    });
}
