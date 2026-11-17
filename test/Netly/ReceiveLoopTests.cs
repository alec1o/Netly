using TcpClient = Netly.TcpClient;
using System.Net;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Netly;

// the receive loops must not recurse when the system completes a receive inside the Begin call
[Collection(StressCollection.Name)]
public class ReceiveLoopTests
{
    private static async Task Flood(Host host, int count, int size)
    {
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        raw.SendBufferSize = 4 * 1024 * 1024;
        var datagram = new byte[size];

        for (var i = 0; i < count; i++)
        {
            raw.SendTo(datagram, host.EndPoint);

            // let the receiver breathe sometimes, the point is a long run of ready datagrams
            if (i % 2000 == 0) await Task.Yield();
        }
    }

    [Fact]
    public async Task UdpServer_SurvivesAFloodOfDatagrams()
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        var received = 0;
        server.OnAccept(c => c.OnData(_ => Interlocked.Increment(ref received)));
        var opened = Wait.Signal<bool>();
        server.OnOpen(() => opened.TrySetResult(true));
        server.Open(host);
        await Wait.For(opened, what: "server open");

        await Flood(host, 100_000, 32);

        // the process is alive and the server still receives
        await Wait.Until(() => Volatile.Read(ref received) > 1000, TimeSpan.FromSeconds(30), "datagrams delivered");
        Assert.True(server.IsOpened);

        server.Close();
    }

    [Fact]
    public async Task RudpServer_SurvivesAFloodOfGarbage()
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        await server.OpenAsync(host);

        await Flood(host, 100_000, 40);

        Assert.True(server.IsOpened);

        // a real client still connects after the flood
        var client = new RudpClient();
        await client.OpenAsync(host);
        Assert.True(client.IsOpened);

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public async Task TcpClient_ReadsAHugeStreamOfSmallWrites()
    {
        var host = Net.TcpHost();
        var server = new TcpServer(false);
        long total = 0;
        server.OnAccept(c => c.OnData(s => Interlocked.Add(ref total, s.Length)));
        var opened = Wait.Signal<bool>();
        server.OnOpen(() => opened.TrySetResult(true));
        server.Open(host);
        await Wait.For(opened, what: "server open");

        var client = new TcpClient(false);
        var connected = Wait.Signal<bool>();
        client.OnOpen(() => connected.TrySetResult(true));
        client.Open(host);
        await Wait.For(connected, what: "client open");

        const int writes = 50_000;
        var payload = new byte[100];
        for (var i = 0; i < writes; i++) client.SendData(payload);

        await Wait.Until(() => Interlocked.Read(ref total) == writes * 100L, TimeSpan.FromSeconds(60), "all bytes");

        client.Close();
        server.Close();
    }
}
