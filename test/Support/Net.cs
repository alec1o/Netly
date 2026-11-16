using System.Net;
using System.Net.Sockets;

namespace NetlyTest.Support;

/// <summary>
///     Helpers to create isolated endpoints, so tests can run in parallel without port collisions.
/// </summary>
internal static class Net
{
    public static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static int FreeUdpPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    public static Host TcpHost() => new(IPAddress.Loopback, FreeTcpPort());

    public static Host UdpHost() => new(IPAddress.Loopback, FreeUdpPort());

    public static byte[] Payload(int size, int seed = 1)
    {
        var data = new byte[size];
        new Random(seed).NextBytes(data);
        return data;
    }
}
