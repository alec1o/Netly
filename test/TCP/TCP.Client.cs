using Netly;
using System.Net;
using Byter;

namespace test.TestTCP;

public class TcpClientTest
{
    private static readonly Host Host = new Host(IPAddress.Loopback, 8765);

    [Fact]
    void DefaultProps()
    {
        var client = new TCP.Client();

        Assert.NotEmpty(client.Id);
        Assert.False(client.IsOpened);
        Assert.True(client.IsFraming);
        Assert.False(client.IsEncrypted);
        Assert.NotNull(client.Host);
        var host = new Host(IPAddress.Any, 0);
        Assert.Equal(host.IPEndPoint.ToString(), client.Host.IPEndPoint.ToString());
    }

    [Fact]
    async void Open()
    {
        var server = new TCP.Server();
        await server.To.Open(Host);
        Assert.True(server.IsOpened);
        bool isOpened = false;
        string errorMessage = string.Empty;

        var client = new TCP.Client();

        client.On.Open(() => { isOpened = true; });

        client.On.Error((e) => { errorMessage = e.Message; });

        await client.To.Open(Host);

        Assert.True(client.IsOpened);
        Assert.True(isOpened, errorMessage);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    async void Close()
    {
        var server = new TCP.Server();
        await server.To.Open(Host);
        Assert.True(server.IsOpened);
        bool isClosed = false;
        string errorMessage = string.Empty;

        var client = new TCP.Client();

        client.On.Close(() => { isClosed = true; });

        client.On.Error((e) => { errorMessage = e.Message; });

        await client.To.Open(Host);
        Assert.True(client.IsOpened);

        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.True(isClosed, errorMessage);

        await server.To.Close();
    }

    [Fact]
    async void Echo()
    {
        var server = new TCP.Server();
        server.On.Accept(c =>
        {
            // echo data
            c.On.Data(d => c.To.Data(d));
            // echo event
            c.On.Event((n, d) => c.To.Event(n, d));
        });

        await server.To.Open(Host);
        Assert.True(server.IsOpened);

        byte[] dataSent = "Hello Server".GetBytes();
        byte[] dataReceive = Array.Empty<byte>();
        (string name, byte[] data) eventSent = ("Sample Event", "Netly event is amazing".GetBytes());
        (string name, byte[] data) eventReceive = (string.Empty, Array.Empty<byte>());
        
        var client = new TCP.Client();
        
        client.On.Data((data) =>
        {
            dataReceive = data;
        });
        
        client.On.Event((name, data) =>
        {
            eventReceive = (name, data);
        });

        await client.To.Open(Host);
        
        Assert.True(client.IsOpened);
        
        client.To.Data(dataSent);
        client.To.Event(eventSent.name, eventSent.data);

        await Task.Delay(100);
        
        Assert.Equal(dataSent, dataReceive);
        {
            Assert.Equal(eventSent.name, eventReceive.name);
            Assert.Equal(eventSent.data, eventReceive.data);
        }

        await server.To.Close();
    }
}