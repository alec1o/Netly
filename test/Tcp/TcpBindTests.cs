using System.Net;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpBindTests
{
    [Fact]
    public async Task ClientCanBindToSpecificLocalIpBeforeConnect()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        var accepted = Wait.Signal<global::Netly.Interfaces.ITCP.Client>();
        server.On.Accept(c => accepted.TrySetResult(c));
        await server.To.Open(host);

        var client = new TCP.Client();
        // On.Modify runs before connect: the socket can be bound to a local ip (and port) here.
        client.On.Modify(socket => socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0)));
        Exception? error = null;
        client.On.Error(e => error = e);

        await client.To.Open(host);

        Assert.Null(error);
        Assert.True(client.IsOpened);

        var peer = await Wait.For(accepted, what: "server accept");
        Assert.Equal(IPAddress.Parse("127.0.0.2"), peer.Host.Address);

        await client.To.Close();
        await server.To.Close();
    }
}
