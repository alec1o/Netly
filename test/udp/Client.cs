using System.Net.Sockets;
using Netly;
using Netly.Core;
using test;

// ReSharper disable once CheckNamespace
namespace com.netly.udp;

public class Client
{
    private static readonly Host MyHost = new Host("127.0.0.1", 12444);

    [Fact]
    public void Dependencies()
    {
        Assert.NotNull(MyHost);
    }

    [Fact]
    public void Instantiate()
    {
        UDP.Client client = new UDP.Client();

        Assert.NotNull(client);
    }

    [Fact]
    public void Proprieties()
    {
        UDP.Client client = new UDP.Client();

        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
        Assert.NotNull(client.Id);
        Assert.NotNull(client.Host);

        Assert.False(client.IsOpened);

        Assert.NotEmpty(client.Id);

        Assert.Equal(Host.Default.ToString(), client.Host.ToString());
    }

    [Fact]
    public async void Connect()
    {
        bool error = false;
        bool connected = false;
        bool modify = false;

        UDP.Client client = new UDP.Client();

        client.On.Modify(_ => modify = true);
        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);

        await client.To.Open(MyHost);
        await client.To.Close();

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

        UDP.Client client = new UDP.Client();

        client.On.Modify(_ => modify = true);
        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);
        client.On.Close(() => disconnected = true);

        await client.To.Open(MyHost);
        await client.To.Close();

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

        UDP.Client client = new UDP.Client();

        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);


        if (!OperatingSystem.IsWindows())
        {
            /*
             * WARNING:
             * windows allow open udp connection at 0 port.
             * this test is disabled for run in windows because will fail.
            */
            await client.To.Open(new Host("127.0.0.1", 0));
            await client.To.Close();
        }

        await client.To.Open(null);
        await client.To.Close();

        Assert.True(error);
        Assert.False(connected);
    }

    [Fact]
    public async void Modify()
    {
        bool connected = false;
        bool modify = false;
        bool disconnect = false;
        bool initSocketConnectedState = false;
        bool connectedSocketConnectedState = false;
        bool disconnectedSocketConnectedState = true;

        Socket? mySocket = null;

        UDP.Client client = new UDP.Client();

        client.On.Modify(socket =>
        {
            modify = true;
            mySocket = socket ?? null;
            initSocketConnectedState = socket?.Connected ?? false;
        });

        client.On.Open(() =>
        {
            connected = true;
            connectedSocketConnectedState = mySocket?.Connected ?? false;
        });

        client.On.Close(() =>
        {
            disconnect = true;
            disconnectedSocketConnectedState = mySocket?.Connected ?? true;
        });

        await client.To.Open(MyHost);
        await client.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.True(disconnect);
        Assert.NotNull(mySocket);
        Assert.True(connectedSocketConnectedState);
        Assert.False(initSocketConnectedState);
        Assert.False(disconnectedSocketConnectedState);

        // check if object is disposable
        try
        {
            await mySocket.ConnectAsync(MyHost.EndPoint);
            Assert.Fail("Socket isn't disposable");
        }
        catch
        {
            // success: is disposed
        }
    }

    [Fact(Skip = "TODO: wait for server")]
    public void SendData()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "TODO: wait for server")]
    public void SendEvent()
    {
        throw new NotImplementedException();
    }
}