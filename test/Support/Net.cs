using System.Net;
using System.Net.Sockets;

namespace NetlyTest.Support;

/// <summary>
///     Helpers to create isolated endpoints, so tests can run in parallel without port collisions.
/// </summary>
internal static class Net
{
    // a port is never given twice: a test that closes its socket releases the port and another test could take it
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> Issued = new();

    public static int FreeTcpPort()
    {
        for (;;)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            if (Issued.TryAdd(port, 0)) return port;
        }
    }

    public static int FreeUdpPort()
    {
        for (;;)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var port = ((IPEndPoint)socket.LocalEndPoint!).Port;

            if (Issued.TryAdd(port, 0)) return port;
        }
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
