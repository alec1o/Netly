using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpFramingSizeTests
{
    [Fact]
    public void FramingSize_DefaultAndValidation()
    {
        var client = new RudpClient();
        var server = new RudpServer();

        Assert.Equal(Framing.DefaultSize, client.FramingSize);
        Assert.Equal(Framing.DefaultSize, server.FramingSize);

        client.FramingSize = 1;
        server.FramingSize = 5000;

        // minimum of 1 KB
        Assert.Equal(1024, client.FramingSize);
        Assert.Equal(5000, server.FramingSize);
    }

    [Fact]
    public Task FramingSize_CannotChangeWhileOpened() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        await server.OpenAsync(host);
        var client = new RudpClient();
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        Assert.Throws<InvalidOperationException>(() => server.FramingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => client.FramingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => server.Clients.Single().FramingSize = 2048);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task BiggerMessageClosesTheConnection_SmallerOneWorks() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = new RudpServer { FramingSize = 4096 };
        var received = new System.Collections.Concurrent.ConcurrentQueue<int>();
        server.OnAccept(c => c.OnData((s, _) => received.Enqueue((int)s.Length)));
        await server.OpenAsync(host);

        var client = new RudpClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        // the server clients got the limit of the server
        Assert.Equal(4096, server.Clients.Single().FramingSize);

        // inside the limit, in one packet and fragmented
        client.SendData(Net.Payload(2000));
        client.SendData(Net.Payload(4000));
        await Wait.Until(() => received.Count == 2, what: "messages inside the limit");

        // above the limit (fragmented): the connection is closed
        client.SendData(Net.Payload(20000));

        await Wait.For(closed, what: "close after the message above the limit");
        Assert.Equal(2, received.Count);

        await server.CloseAsync();
    });
}
