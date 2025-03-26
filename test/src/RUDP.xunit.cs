using Netly.Interfaces;

public class RUDP_xunit
{
    [Fact]
    public void Init()
    {
        var server = new RUDP.Server();

        Assert.NotEmpty(server.Id);
        Assert.False(server.IsOpened);
        Assert.Equal("0.0.0.0:0", server.Host.ToString());
        Assert.Equal(5000, server.HandshakeTimeout);
        Assert.Equal(5000, server.NoResponseTimeout);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);

        var client = new RUDP.Client();

        Assert.Empty(client.Id);
        Assert.False(client.IsOpened);
        Assert.Equal("0.0.0.0:0", client.Host.ToString());
        Assert.Equal(5000, client.HandshakeTimeout);
        Assert.Equal(5000, client.NoResponseTimeout);
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
    }

    [Fact]
    public async Task RudpConnection()
    {
        // server
        var server = new RUDP.Server();
        IRUDP.Client? session = null;

        server.On.Accept(x => session = x);

        await server.To.Open(new("127.0.0.1", 7001));

        Assert.True(server.IsOpened);

        // client
        var client = new RUDP.Client();

        var index = 0;

        (byte[] data, RUDP.MessageType type) clientData = ([], RUDP.Sequenced);

        (string name, byte[] data, RUDP.MessageType type) clientEvent = ("", [], RUDP.MessageType.Sequenced);

        client.On.Open(() => index += 1);

        client.On.Modify(_ => index += 2);

        client.On.Error(_ => index += 4);

        client.On.Close(() => index += 8);

        client.On.Data((data, type) =>
        {
            index += 16;
            clientData = (data, type);
        });

        client.On.Event((name, data, type) =>
        {
            index += 32;
            clientEvent = (name, data, type);
        });

        // test
        Assert.Null(session);
        await client.To.Open(server.Host);
        Assert.True(client.IsOpened);
        Assert.NotEmpty(client.Id);
        await Task.Delay(500);
        Assert.NotNull(session);
        Assert.NotEmpty(session.Id);
        Assert.Equal(3, index);

        (string name, byte[] data, RUDP.MessageType type) sessionEvent = (null!, null!, RUDP.Sequenced);
        session.On.Event((name, data, type) =>
        {
            sessionEvent = (name, data, type);
            session.To.Event(name, data, type);
        });

        (byte[] data, RUDP.MessageType type) sessionData = (null!, RUDP.Sequenced);

        session.On.Data((data, type) =>
        {
            sessionData = (data, type);
            session.To.Data(data, type);
        });

        client.To.Data("Hello World, by To.Data", RUDP.Reliable);
        client.To.Event("Welcome to event", "Hello!", RUDP.Unreliable);

        await Task.Delay(250);

        Assert.NotEmpty(sessionData.data);
        Assert.NotEmpty(clientData.data);
        Assert.NotEmpty(clientEvent.name);
        Assert.NotEmpty(sessionEvent.name);
        Assert.NotEmpty(clientEvent.data);
        Assert.NotEmpty(sessionEvent.data);

        Assert.Equal(sessionData.data, clientData.data);
        Assert.Equal(sessionData.type, clientData.type);

        Assert.Equal(sessionEvent.name, clientEvent.name);
        Assert.Equal(sessionEvent.data, clientEvent.data);
        Assert.Equal(sessionEvent.type, clientEvent.type);

        Assert.Equal(1 + 2 + 16 + 32, index);
        index = 0;

        await client.To.Close();
        Assert.False(client.IsOpened);
        Assert.Equal(8, index);
        index = 0;

        await Task.Delay(7000);
        Assert.False(session.IsOpened);
        await client.To.Open(new("127.0.0.1", 7002));
        await Task.Delay(200);
        Assert.False(client.IsOpened);
        Assert.Equal(2 + 4, index); // open and close (NEVER OPENED).
    }

}