public class WebSocket_xunit
{
    [Fact]
    public void InitWebSocketClient()
    {
        var client = new HTTP.WebSocket();

        Assert.False(client.IsOpened);
        Assert.NotNull(client.Host);
        Assert.Equal("wss://0.0.0.0/", client.Host.ToString());
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
        Assert.Null(client.ServerRequest);
    }

    [Fact]
    public async Task WebSocketConnection()
    {
        // server
        var server = new HTTP.Server();
        HTTP.WebSocket? session = null;

        server.Map.WebSocket("/test", (_, w) => session = (HTTP.WebSocket)w);

        await server.To.Open(new("https://127.0.0.1:6001"));
        Assert.True(server.IsOpened);

        // client
        var client = new HTTP.WebSocket();

        var index = 0;

        (byte[] data, bool isBinary) clientData = ([], false);

        (string name, byte[] data, bool isBinary) clientEvent = ("", [], false);

        client.On.Open(() => index += 1);

        client.On.Modify(_ => index += 2);

        client.On.Error(_ => index += 4);

        client.On.Close(() => index += 8);

        client.On.Data((data, type) =>
        {
            index += 16;
            clientData = (data, type == HTTP.MessageType.Binary);
        });

        client.On.Event((name, data, type) =>
        {
            index += 32;
            clientEvent = (name, data, type == HTTP.MessageType.Binary);
        });

        // test
        Assert.Null(session);

        await client.To.Open(new("ws://127.0.0.1:6001/test"));
        Assert.True(client.IsOpened);
        await Task.Delay(500);
        Assert.NotNull(session);
        Assert.NotNull(session.ServerRequest);
        Assert.Equal(3, index);

        (string name, byte[] data, bool isBinary) sessionEvent = (null!, null!, false);
        session.On.Event((name, data, type) =>
        {
            sessionEvent = (name, data, type == HTTP.MessageType.Binary);
            session.To.Event(name, data, type);
        });

        (byte[] data, bool isBinary) sessionData = (null!, false);

        session.On.Data((data, type) =>
        {
            sessionData = (data, type == HTTP.MessageType.Binary);
            session.To.Data(data, type);
        });

        client.To.Data("Hello World, by To.Data", HTTP.MessageType.Text);
        client.To.Event("Welcome to event", "Hello!", HTTP.MessageType.Binary);

        await Task.Delay(250);

        Assert.NotEmpty(sessionData.data);
        Assert.NotEmpty(clientData.data);
        Assert.NotEmpty(clientEvent.name);
        Assert.NotEmpty(sessionEvent.name);
        Assert.NotEmpty(clientEvent.data);
        Assert.NotEmpty(sessionEvent.data);

        Assert.Equal(sessionData.data, clientData.data);
        Assert.Equal(sessionData.isBinary, clientData.isBinary);

        Assert.Equal(sessionEvent.name, clientEvent.name);
        Assert.Equal(sessionEvent.data, clientEvent.data);
        Assert.Equal(sessionEvent.isBinary, clientEvent.isBinary);

        Assert.Equal(1 + 2 + 16 + 32, index);
        index = 0;

        await client.To.Close();
        Assert.False(client.IsOpened);
        Assert.Equal(8, index);
        index = 0;

        await Task.Delay(100);
        Assert.False(session.IsOpened);


        await client.To.Open(new("ws://127.0.0.1:6001/empty"));
        await Task.Delay(2000);
        Assert.False(client.IsOpened);
        Assert.Equal(1 + 2 + 8, index); // open and close: because path;
        index = 0;

        await client.To.Open(new("ws://127.0.0.1:112/"));
        await Task.Delay(200);
        Assert.False(client.IsOpened);
        Assert.Equal(2 + 4, index); // open and close (NEVER OPENED).
    }
}