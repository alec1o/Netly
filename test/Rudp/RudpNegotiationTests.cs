using System.Net;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Rudp;

public class RudpNegotiationTests
{
    private static async Task<RudpServer> Echo(Host host)
    {
        var server = new RudpServer();
        server.OnAccept(c => c.OnData((s, t) =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            c.SendData(buffer, t);
        }));
        await server.OpenAsync(host);
        return server;
    }

    [Fact]
    public Task BothSidesUseTheSameDatagram() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = await Echo(host);
        var client = new RudpClient();
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        Assert.Equal(client.MaxDatagram, server.Clients.Single().MaxDatagram);
        Assert.InRange(client.MaxDatagram, 512, 8192);

        await client.CloseAsync();
        await server.CloseAsync();
    });

    [Fact]
    public Task TheSmallerBufferOfThePeersWins() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = await Echo(host);

        // the client has small buffers (changed by the user before connecting), the server has big ones
        var client = new RudpClient();
        client.OnModify(socket =>
        {
            // linux doubles what is set and keeps a minimum, other systems keep it: any small value works
            socket.SendBufferSize = 1024;
            socket.ReceiveBufferSize = 1024;
        });
        var received = Wait.Signal<byte[]>();
        client.OnData((s, _) =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            received.TrySetResult(buffer);
        });
        await client.OpenAsync(host);
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        var peer = server.Clients.Single();

        // the server would use 8192 alone, with the client it uses what the client can hold
        Assert.Equal(client.MaxDatagram, peer.MaxDatagram);
        Assert.True(peer.MaxDatagram < 8192, $"{peer.MaxDatagram}");

        var payload = Net.Payload(60 * 1024, 5);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, TimeSpan.FromSeconds(30), "echo with the negotiated size"));

        await client.CloseAsync();
        await server.CloseAsync();
    });

    private static byte[] Syn(byte version, ushort announced)
    {
        var buffer = new byte[RudpWire.SynSize];
        buffer[0] = RudpWire.Handshake;
        buffer[1] = RudpWire.Syn;
        buffer[2] = version;
        RudpWire.Put32(buffer, 3, 0xCAFE);
        RudpWire.Put16(buffer, 7, announced);
        return buffer;
    }

    private static byte[]? Receive(Socket socket)
    {
        socket.ReceiveTimeout = 1500;
        var buffer = new byte[2048];

        try
        {
            var size = socket.Receive(buffer);
            return buffer.AsSpan(0, size).ToArray();
        }
        catch (SocketException)
        {
            return null;
        }
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)65535)]
    public Task AnnouncedDatagramIsAlwaysUsable(ushort announced) => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = await Echo(host);
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        raw.Connect(host.EndPoint);

        raw.Send(Syn(RudpWire.Version, announced));
        var answer = Receive(raw);

        // a lie (zero, tiny or bigger than the real limit) never breaks the handshake
        Assert.NotNull(answer);
        Assert.Equal(RudpWire.SynAckSize, answer!.Length);
        Assert.Equal(RudpWire.SynAck, answer[1]);
        Assert.InRange((int)RudpWire.Get16(answer, 26), RudpWire.MinDatagram, 8192);

        await server.CloseAsync();
    });

    [Fact]
    public Task SynOfAnotherVersionIsIgnored() => Guard.Run(async () =>
    {
        var host = Net.UdpHost();
        var server = await Echo(host);
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        raw.Connect(host.EndPoint);

        raw.Send(Syn(1, 8192));

        // the old protocol version has no negotiation: no answer and no connection
        Assert.Null(Receive(raw));
        Assert.Empty(server.Clients);

        await server.CloseAsync();
    });
}
