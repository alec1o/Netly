using System.Net;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Netly;

[Collection(StressCollection.Name)]
public class SocketLimitsTests
{
    private static Socket Udp() => new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    [Fact]
    public void Limits_NeverPassTheProtocolLimit()
    {
        using var socket = Udp();
        socket.SendBufferSize = socket.ReceiveBufferSize = 1024 * 1024;

        Assert.Equal(65507, SocketLimits.Send(socket, 65507));
        Assert.Equal(65507, SocketLimits.Receive(socket, 65507));
        Assert.Equal(1232, SocketLimits.Both(socket, 1232));
    }

    [Fact]
    public void Limits_FollowTheBuffersOfTheSocket()
    {
        using var socket = Udp();
        socket.SendBufferSize = 4096;
        socket.ReceiveBufferSize = 6000;

        var send = SocketLimits.Send(socket, 65507);
        var receive = SocketLimits.Receive(socket, 65507);

        // each system rounds or doubles what is set: the limit is what the socket reports without the doubling
        Assert.Equal(Math.Max(512, socket.SendBufferSize / SocketLimits.BufferFactor), send);
        Assert.Equal(Math.Max(512, socket.ReceiveBufferSize / SocketLimits.BufferFactor), receive);
        Assert.Equal(Math.Min(send, receive), SocketLimits.Both(socket, 65507));
        Assert.True(send < 65507 && receive < 65507);
    }

    [Fact]
    public void Limits_HaveAFloor()
    {
        using var socket = Udp();

        // a tiny protocol limit is not raised, and a tiny buffer never gives a zero size
        Assert.Equal(512, SocketLimits.Send(socket, 100));
        Assert.True(SocketLimits.Receive(socket, 65507) >= 512);
    }

    [Fact]
    public void Limits_ClosedSocket_UsesTheProtocolLimit()
    {
        var socket = Udp();
        socket.Dispose();

        Assert.Equal(1500, SocketLimits.Send(socket, 1500));
        Assert.Equal(1500, SocketLimits.Both(socket, 1500));
    }

    [Fact]
    public async Task Rudp_SmallSocketBuffers_ShrinkThePacketAndKeepTheData()
    {
        var host = Net.UdpHost();
        var server = new RudpServer();
        server.OnAccept(c => c.OnData((s, t) =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            c.SendData(buffer, t);
        }));
        await server.OpenAsync(host);

        var client = new RudpClient();
        var received = Wait.Signal<byte[]>();
        client.OnModify(socket =>
        {
            socket.SendBufferSize = 4096;
            socket.ReceiveBufferSize = 4096;
        });
        client.OnData((s, _) =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            received.TrySetResult(buffer);
        });
        await client.OpenAsync(host);

        var payload = Net.Payload(100 * 1024, 11);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, TimeSpan.FromSeconds(30), "echo with small buffers"));

        await client.CloseAsync();
        await server.CloseAsync();
    }

    [Fact]
    public void BufferFactor_IsDetectedFromTheSystem()
    {
        using var socket = Udp();
        socket.SendBufferSize = 64 * 1024;

        // the factor is what the system does with a buffer that is set: 1 keeps it, 2 doubles it (linux)
        Assert.Contains(SocketLimits.BufferFactor, new[] { 1, 2 });
        Assert.Equal(socket.SendBufferSize == 64 * 1024 ? 1 : 2, SocketLimits.BufferFactor);
    }
}
