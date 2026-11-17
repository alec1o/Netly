using NetlyTest.Support;

namespace NetlyTest.WebSocket;

public class WebSocketFramingSizeTests
{
    [Fact]
    public void FramingSize_DefaultAndValidation()
    {
        var client = new WebSocketClient();
        var server = new WebSocketServer();

        Assert.Equal(Framing.DefaultSize, client.FramingSize);
        Assert.Equal(Framing.DefaultSize, server.FramingSize);

        client.FramingSize = 10;
        server.FramingSize = 8192;

        // minimum of 1 KB
        Assert.Equal(1024, client.FramingSize);
        Assert.Equal(8192, server.FramingSize);
    }

    [Fact]
    public async Task FramingSize_CannotChangeWhileOpened()
    {
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");
        await server.OpenAsync(uri);
        var client = new WebSocketClient();
        await client.OpenAsync(uri);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        Assert.Throws<InvalidOperationException>(() => server.FramingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => client.FramingSize = 2048);
        Assert.Throws<InvalidOperationException>(() => server.Clients.Single().FramingSize = 2048);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task BiggerMessageClosesTheConnection_SmallerOneWorks()
    {
        var server = new WebSocketServer { FramingSize = 4096 };
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");
        var received = new System.Collections.Concurrent.ConcurrentQueue<int>();
        server.OnAccept(c => c.OnData((s, _) => received.Enqueue((int)s.Length)));
        await server.OpenAsync(uri);

        var client = new WebSocketClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        await client.OpenAsync(uri);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        Assert.Equal(4096, server.Clients.Single().FramingSize);

        client.SendData(Net.Payload(2000));
        await Wait.Until(() => received.Count == 1, what: "message inside the limit");

        // above the limit: the server closes the connection
        client.SendData(Net.Payload(20000));

        await Wait.For(closed, what: "close after the message above the limit");
        Assert.Single(received);

        await server.CloseAsync();
    }

    [Fact]
    public async Task ClientLimit_ClosesWhenTheServerSendsTooMuch()
    {
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");
        server.OnAccept(c => c.SendData(Net.Payload(10000)));
        await server.OpenAsync(uri);

        var client = new WebSocketClient { FramingSize = 4096 };
        var closed = Wait.Signal<bool>();
        var got = 0;
        client.OnClose(() => closed.TrySetResult(true));
        client.OnData((_, _) => Interlocked.Increment(ref got));
        await client.OpenAsync(uri);

        await Wait.For(closed, what: "client closes the connection");
        Assert.Equal(0, got);

        await server.CloseAsync();
    }

    [Fact]
    public async Task BiggerMessage_PeerReceivesMessageTooBig()
    {
        var server = new WebSocketServer { FramingSize = 4096 };
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");
        await server.OpenAsync(uri);

        var client = new WebSocketClient();
        var closed = Wait.Signal<System.Net.WebSockets.WebSocketCloseStatus>();
        client.OnClose(status => closed.TrySetResult(status));
        await client.OpenAsync(uri);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        client.SendData(Net.Payload(20000));

        Assert.Equal(System.Net.WebSockets.WebSocketCloseStatus.MessageTooBig, await Wait.For(closed, what: "close status"));

        await server.CloseAsync();
    }
}
