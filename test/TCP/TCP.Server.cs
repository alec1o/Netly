using Netly.Core;
using Xunit.Abstractions;

namespace test.TCP;

using Netly;

public class TestTCP
{
    private static readonly Host Host = new Host("127.0.0.1", 9876);

    private readonly ITestOutputHelper _output;

    public TestTCP(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DefaultServerProps()
    {
        TCP.Server server = new TCP.Server();
        Assert.True(server.IsFraming);
        Assert.False(server.IsOpened);
        Assert.False(server.IsEncrypted);

        server = new TCP.Server(isFraming: true);
        Assert.True(server.IsFraming);

        server = new TCP.Server(isFraming: false);
        Assert.False(server.IsFraming);
    }

    [Fact]
    public async void OpenServer()
    {
        TCP.Server server = new TCP.Server();
        bool receiveOpenCallback = false;
        string errorMessage = string.Empty;

        server.On.Open(() => { receiveOpenCallback = true; });

        server.On.Error(e => { errorMessage = e.Message; });

        await server.To.Open(Host);

        Assert.True(server.IsOpened);
        Assert.True(receiveOpenCallback, errorMessage);

        await server.To.Close();
    }

    [Fact]
    public async void CloseServer()
    {
        TCP.Server server = new TCP.Server();

        bool isClosed = false;

        server.On.Close(() => { isClosed = true; });

        await server.To.Open(Host);

        Assert.True(server.IsOpened);

        await server.To.Close();

        Assert.False(server.IsOpened);
        Assert.True(isClosed);
    }

    [Fact]
    private async void AcceptClient()
    {
        var server = new TCP.Server();

        bool isConnected = false;
        bool isDisconnected = false;
        // data send
        byte[] client1DataSend = NE.GetBytes("Netly Library: Client 1");
        byte[] client2DataSend = NE.GetBytes("Netly Library: Client 2");
        byte[] client3DataSend = NE.GetBytes("Netly Library: Client 3");
        // data receive
        byte[] client1DataReceived = Array.Empty<byte>();
        byte[] client2DataReceived = Array.Empty<byte>();
        byte[] client3DataReceived = Array.Empty<byte>();

        // event send
        (string name, byte[] data) client1EventSend = ("Event Name Client 1", NE.GetBytes("Event Client 1"));
        (string name, byte[] data) client2EventSend = ("Event Name Client 2", NE.GetBytes("Event Client 2"));
        (string name, byte[] data) client3EventSend = ("Event Name Client 3", NE.GetBytes("Event Client 3"));
        // event receive
        (string name, byte[] data) client1EventReceived = (string.Empty, Array.Empty<byte>());
        (string name, byte[] data) client2EventReceived = (string.Empty, Array.Empty<byte>());
        (string name, byte[] data) client3EventReceived = (string.Empty, Array.Empty<byte>());

        int clients = 0;

        server.On.Accept((c) =>
        {
            clients++;
            int id = clients;

            _output.WriteLine("* [server] client accept");

            c.On.Open(() =>
            {
                isConnected = true;
                _output.WriteLine("* [server] client open");
            });

            c.On.Close(() =>
            {
                isDisconnected = true;
                _output.WriteLine("* [server] client close");
            });

            c.On.Data(data =>
            {
                switch (id)
                {
                    case 1:
                        client1DataReceived = data;
                        break;
                    case 2:
                        client2DataReceived = data;
                        break;
                    case 3:
                        client3DataReceived = data;
                        break;
                }
            });

            c.On.Event((name, data) =>
            {
                _output.WriteLine($"client {id}, event ({name}) {NE.GetString(data)}");
                switch (id)
                {
                    case 1:
                        client1EventReceived = (name, data);
                        break;
                    case 2:
                        client2EventReceived = (name, data);
                        break;
                    case 3:
                        client3EventReceived = (name, data);
                        break;
                }
            });
        });

        await server.To.Open(Host);

        Assert.True(server.IsOpened);

        var client1 = new TCP.Client();
        var client2 = new TCP.Client();
        var client3 = new TCP.Client();

        Assert.False(client1.IsOpened);
        Assert.False(client2.IsOpened);
        Assert.False(client3.IsOpened);

        await client1.To.Open(Host);
        await client2.To.Open(Host);
        await client3.To.Open(Host);

        Assert.True(client1.IsOpened);
        Assert.True(client2.IsOpened);
        Assert.True(client3.IsOpened);

        await Task.Delay(100);

        Assert.NotEmpty(server.Clients);
        Assert.Equal(3, server.Clients.Length);

        // data
        client1.To.Data(client1DataSend);
        client2.To.Data(client2DataSend);
        client3.To.Data(client3DataSend);
        // event
        client1.To.Event(client1EventSend.name, client1EventSend.data);
        client2.To.Event(client2EventSend.name, client2EventSend.data);
        client3.To.Event(client3EventSend.name, client3EventSend.data);

        await Task.Delay(100);

        // data
        Assert.Equal(client1DataSend, client1DataReceived);
        Assert.Equal(client2DataSend, client2DataReceived);
        Assert.Equal(client3DataSend, client3DataReceived);
        // event
        {
            Assert.Equal(client1EventSend.name, client1EventReceived.name);
            Assert.Equal(client1EventSend.data, client1EventReceived.data);
        }
        {
            Assert.Equal(client2EventSend.name, client2EventReceived.name);
            Assert.Equal(client2EventSend.data, client2EventReceived.data);
        }
        {
            Assert.Equal(client3EventSend.name, client3EventReceived.name);
            Assert.Equal(client3EventSend.data, client3EventReceived.data);
        }

        await client1.To.Close();
        await client2.To.Close();
        await client3.To.Close();

        Assert.False(client1.IsOpened);
        Assert.False(client2.IsOpened);
        Assert.False(client3.IsOpened);

        await Task.Delay(100); // wait for server receive callbacks

        Assert.Empty(server.Clients);
        Assert.True(isConnected);
        Assert.True(isDisconnected);

        await server.To.Close();
    }
}