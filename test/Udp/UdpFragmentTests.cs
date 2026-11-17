using UdpClient = Netly.UdpClient;
using System.Net.Sockets;
using System.Reflection;
using NetlyTest.Support;

namespace NetlyTest.Udp;

public class UdpFragmentTests
{
    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    // forces a small datagram, so the fragments are used on any machine
    private static void Limit(UdpClient client, int size)
    {
        typeof(UdpClient).GetField("_maxDatagram", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(client, size);
    }

    private static async Task<(UdpServer server, Host host)> Start(Action<UdpClient> onAccept)
    {
        var host = Net.UdpHost();
        var server = new UdpServer();
        var opened = Wait.Signal<bool>();

        server.OnAccept(onAccept);
        server.OnOpen(() => opened.TrySetResult(true));
        server.OnError(_ => opened.TrySetResult(false));
        server.Open(host);

        Assert.True(await Wait.For(opened, what: "server open"));
        return (server, host);
    }

    private static async Task Close(UdpServer server)
    {
        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();
        await Wait.For(closed, what: "server close");
    }

    private static byte[] Fragment(ushort id, int index, int count, byte[] part)
    {
        var datagram = new byte[UdpFragment.HeaderSize + part.Length];

        UdpFragment.Write(datagram, id, index, count);
        Buffer.BlockCopy(part, 0, datagram, UdpFragment.HeaderSize, part.Length);

        return datagram;
    }

    private static Socket Raw(Host host)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(host.EndPoint);
        return socket;
    }

    [Fact]
    public void Header_RoundTrip_AndRejectsAnythingElse()
    {
        var datagram = Fragment(0xABCD, 2, 5, new byte[] { 1, 2, 3 });

        Assert.Equal(new byte[] { 36, 19, 0xAB, 0xCD, 2, 5 }, datagram.Take(6));
        Assert.True(UdpFragment.TryRead(datagram, datagram.Length, out var id, out var index, out var count));
        Assert.Equal((0xABCD, 2, 5), (id, index, count));

        var checksum = (byte[])datagram.Clone();
        checksum[UdpFragment.HeaderSize - 1] ^= 0xFF;
        Assert.False(UdpFragment.TryRead(checksum, checksum.Length, out _, out _, out _));

        var prefix = (byte[])datagram.Clone();
        prefix[0] ^= 0xFF;
        Assert.False(UdpFragment.TryRead(prefix, prefix.Length, out _, out _, out _));

        // header without a body, one fragment only, index out of the count
        Assert.False(UdpFragment.TryRead(datagram, UdpFragment.HeaderSize, out _, out _, out _));
        var single = Fragment(1, 0, 1, new byte[] { 1 });
        Assert.False(UdpFragment.TryRead(single, single.Length, out _, out _, out _));
        var outside = Fragment(1, 3, 3, new byte[] { 1 });
        Assert.False(UdpFragment.TryRead(outside, outside.Length, out _, out _, out _));
    }

    [Fact]
    public async Task MessageAboveTheDatagram_IsSentInFragmentsAndJoined()
    {
        var (server, host) = await Start(client =>
        {
            Limit(client, 600);
            client.OnData(stream => client.SendData(Read(stream)));
            client.OnEvent((name, stream) => client.SendEvent(name, Read(stream)));
        });

        var client = new UdpClient();
        var data = Wait.Signal<byte[]>();
        var evt = Wait.Signal<(string, byte[])>();
        var opened = Wait.Signal<bool>();

        client.OnOpen(() => opened.TrySetResult(true));
        client.OnData(stream => data.TrySetResult(Read(stream)));
        client.OnEvent((name, stream) => evt.TrySetResult((name, Read(stream))));
        client.Open(host);
        Assert.True(await Wait.For(opened, what: "client open"));
        Limit(client, 600);

        var payload = Net.Payload(10_000);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(data, what: "echo of the fragmented data"));

        var body = Net.Payload(20_000, 7);
        client.SendEvent("big", body);

        var (name, received) = await Wait.For(evt, what: "echo of the fragmented event");
        Assert.Equal("big", name);
        Assert.Equal(body, received);

        client.Close();
        await Close(server);
    }

    [Fact]
    public async Task MessageThatFitsTheDatagram_IsSentAsIs()
    {
        var received = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        var (server, host) = await Start(client => client.OnData(stream => received.Enqueue(Read(stream))));

        using var raw = Raw(host);
        var small = Net.Payload(100);

        // a plain datagram of another program is delivered untouched
        raw.Send(small);
        await Wait.Until(() => received.Count == 1, what: "plain datagram");
        Assert.Equal(small, received.Single());

        await Close(server);
    }

    [Fact]
    public async Task FragmentsOutOfOrder_AreJoined()
    {
        var received = Wait.Signal<byte[]>();
        var (server, host) = await Start(client => client.OnData(stream => received.TrySetResult(Read(stream))));

        using var raw = Raw(host);
        var a = Net.Payload(300, 1);
        var b = Net.Payload(200, 2);
        var c = Net.Payload(100, 3);

        raw.Send(Fragment(9, 2, 3, c));
        raw.Send(Fragment(9, 0, 3, a));
        raw.Send(Fragment(9, 0, 3, a));
        raw.Send(Fragment(9, 1, 3, b));

        Assert.Equal(a.Concat(b).Concat(c).ToArray(), await Wait.For(received, what: "joined message"));

        await Close(server);
    }

    [Fact]
    public async Task IncompleteMessage_IsNeverDelivered()
    {
        var received = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        var (server, host) = await Start(client => client.OnData(stream => received.Enqueue(Read(stream))));

        using var raw = Raw(host);

        // two of three fragments, then a plain message: only the plain one arrives
        raw.Send(Fragment(5, 0, 3, Net.Payload(100)));
        raw.Send(Fragment(5, 1, 3, Net.Payload(100)));
        var plain = Net.Payload(50, 4);
        raw.Send(plain);

        await Wait.Until(() => received.Count == 1, what: "plain message");
        await Task.Delay(200);
        Assert.Equal(plain, received.Single());
        Assert.Single(received);

        await Close(server);
    }

    [Fact]
    public async Task DataWithThePrefixOfAFragmentButWrongChecksum_IsDeliveredAsData()
    {
        var received = Wait.Signal<byte[]>();
        var (server, host) = await Start(client => client.OnData(stream => received.TrySetResult(Read(stream))));

        using var raw = Raw(host);
        var datagram = Fragment(1, 0, 2, Net.Payload(40));
        datagram[UdpFragment.HeaderSize - 1] ^= 0xFF;
        raw.Send(datagram);

        Assert.Equal(datagram, await Wait.For(received, what: "data with the prefix"));

        await Close(server);
    }

    [Fact]
    public async Task SendToAPortWithoutServer_NeverRaisesOnError()
    {
        // nobody listens: the system answers with ICMP and reports it on the next send, each system on a different one
        var client = new UdpClient();
        var errors = 0;
        var opened = Wait.Signal<bool>();

        client.OnError(_ => Interlocked.Increment(ref errors));
        client.OnOpen(() => opened.TrySetResult(true));
        client.Open(Net.UdpHost());
        Assert.True(await Wait.For(opened, what: "client open"));

        for (var i = 0; i < 8; i++)
        {
            client.SendData(Net.Payload(50));
            await Task.Delay(40);
        }

        Assert.Equal(0, errors);
        Assert.True(client.IsOpened);

        client.Close();
    }
}
