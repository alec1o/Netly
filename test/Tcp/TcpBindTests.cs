using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpBindTests
{
    // local ip of this machine: prefer a real interface, loopback is the fallback (macos has only 127.0.0.1)
    private static IPAddress LocalIp()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
            .ToList();

        return addresses.FirstOrDefault(x => !IPAddress.IsLoopback(x)) ?? IPAddress.Loopback;
    }

    [Fact]
    public async Task ClientCanBindToSpecificLocalIpBeforeConnect()
    {
        var ip = LocalIp();
        var port = Net.FreeTcpPort();
        var server = new TCP.Server();
        var accepted = Wait.Signal<global::Netly.Interfaces.ITCP.Client>();
        server.On.Accept(c => accepted.TrySetResult(c));
        await server.To.Open(new Host(IPAddress.Any, port));

        var client = new TCP.Client();
        // On.Modify runs before connect: the socket can be bound to a local ip (and port) here.
        client.On.Modify(socket => socket.Bind(new IPEndPoint(ip, 0)));
        Exception? error = null;
        client.On.Error(e => error = e);

        await client.To.Open(new Host(ip, port));

        Assert.Null(error);
        Assert.True(client.IsOpened);

        var peer = await Wait.For(accepted, what: "server accept");
        Assert.Equal(ip, peer.Host.Address);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task BindToUnassignableIpRaisesError()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var client = new TCP.Client();
        Exception? error = null;
        client.On.Error(e => error = e);
        client.On.Modify(socket => socket.Bind(new IPEndPoint(IPAddress.Parse("203.0.113.1"), 0)));

        await client.To.Open(host);

        Assert.False(client.IsOpened);
        Assert.NotNull(error);

        await server.To.Close();
    }
}
