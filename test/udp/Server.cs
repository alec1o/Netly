using System.Net;
using System.Net.Sockets;
using Netly;
using Netly.Core;

// ReSharper disable once CheckNamespace
namespace com.netly.udp;

public class Server
{
    private static readonly Host MyHost = Host.Default;

    [Fact]
    public void Dependencies()
    {
        Assert.NotNull(MyHost);
    }

    [Fact]
    public void Instantiate()
    {
        UDP.Server server = new UDP.Server();

        Assert.NotNull(server);
    }

    [Fact]
    public void Proprieties()
    {
        UDP.Server server = new UDP.Server();

        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
        Assert.NotNull(server.Id);
        Assert.NotNull(server.Host);

        Assert.Empty(server.Clients);

        Assert.False(server.IsOpened);

        Assert.NotEmpty(server.Id);

        Assert.Equal(Host.Default.ToString(), server.Host.ToString());
    }

    [Fact]
    public async void Connect()
    {
        bool error = false;
        bool connected = false;
        bool modify = false;

        UDP.Server server = new UDP.Server();

        server.On.Modify(_ => modify = true);
        server.On.Open(() => connected = true);
        server.On.Error(_ => error = true);

        await server.To.Open(MyHost);
        await server.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.False(error);
    }

    [Fact]
    public async void Disconnect()
    {
        bool modify = false;
        bool error = false;
        bool connected = false;
        bool disconnected = false;

        UDP.Server server = new UDP.Server();

        server.On.Modify(_ => modify = true);
        server.On.Open(() => connected = true);
        server.On.Error(_ => error = true);
        server.On.Close(() => disconnected = true);

        await server.To.Open(MyHost);
        await server.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.True(disconnected);
        Assert.False(error);
    }

    [Fact]
    public async void Error()
    {
        bool error = false;
        bool connected = false;

        UDP.Server server = new UDP.Server();

        server.On.Open(() => connected = true);
        server.On.Error(_ => error = true);

        await server.To.Open(null);
        await server.To.Close();

        Assert.True(error);
        Assert.False(connected);
    }

    [Fact]
    public async void BindAtRandomPort()
    {
        Host randomHost = Host.Default;

        UDP.Server server = new UDP.Server();

        Assert.False(server.IsOpened);
        Assert.Equal(randomHost.Port, server.Host.Port);

        await server.To.Open(randomHost);
        Assert.True(server.IsOpened);
        
        await server.To.Close();
        Assert.False(server.IsOpened);

        Assert.NotEqual(randomHost.Port, server.Host.Port);
    }

    [Fact]
    public async void Modify()
    {
        bool connected = false;
        bool modify = false;
        bool disconnect = false;

        Socket? mySocket = null;

        UDP.Server server = new UDP.Server();

        server.On.Modify(socket =>
        {
            modify = true;
            mySocket = socket ?? null;
        });

        server.On.Open(() => { connected = true; });

        server.On.Close(() => { disconnect = true; });

        await server.To.Open(MyHost);
        await server.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.True(disconnect);
        Assert.NotNull(mySocket);

        // check if object is disposable
        try
        {
            mySocket.Bind(MyHost.EndPoint);
            Assert.Fail("Socket isn't disposable");
        }
        catch
        {
            // success: is disposed
        }
    }

    [Fact(Skip = "TODO: wait for client")]
    public void SendData()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "TODO: wait for client")]
    public void SendEvent()
    {
        throw new NotImplementedException();
    }
}