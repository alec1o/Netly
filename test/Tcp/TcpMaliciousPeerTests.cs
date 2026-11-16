using System.Net;
using System.Net.Sockets;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpMaliciousPeerTests
{
    private static async Task<(TCP.Server server, NHost host)> EchoServer()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        server.On.Accept(client => client.On.Data(stream => client.To.Data(stream.GetBytes())));
        await server.To.Open(host);
        return (server, host);
    }

    private static Socket RawConnect(NHost host)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(host.EndPoint);
        return socket;
    }

    // true when the remote side closed the connection (read 0 or reset)
    private static async Task<bool> IsClosedByRemote(Socket socket, int timeoutMs = 5000)
    {
        var limit = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var buffer = new byte[16];

        while (DateTime.UtcNow < limit)
        {
            try
            {
                if (socket.Poll(50_000, SelectMode.SelectRead) && socket.Receive(buffer) == 0) return true;
            }
            catch (SocketException)
            {
                return true;
            }

            await Task.Delay(10);
        }

        return false;
    }

    private static async Task AssertServerStillWorks(NHost host)
    {
        var client = new TCP.Client();
        var echoed = Wait.Signal<string>();
        client.On.Data(s => echoed.TrySetResult(System.Text.Encoding.UTF8.GetString(s.GetBytes())));
        await client.To.Open(host);

        client.To.Data("still alive");

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after attack"));
        await client.To.Close();
    }

    [Fact]
    public async Task InvalidPrefix_ClosesConnection()
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);

        raw.Send(NHelper.ArrayConcat(new byte[] { 9, 9, 9, 9 }, BitConverter.GetBytes(4L), new byte[4]));

        Assert.True(await IsClosedByRemote(raw), "connection with invalid prefix must be closed");
        await AssertServerStillWorks(host);
        await server.To.Close();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task InvalidSize_ClosesConnection(long size)
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);

        raw.Send(NHelper.ArrayConcat(NFraming.Prefix, BitConverter.GetBytes(size)));

        Assert.True(await IsClosedByRemote(raw));
        await AssertServerStillWorks(host);
        await server.To.Close();
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(1024L * 1024 * 1024 * 8)]
    [InlineData(NFraming.DefaultSize + 1)]
    public async Task HugeSize_ClosesConnectionWithoutAllocating(long size)
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);
        var before = GC.GetTotalMemory(true);

        raw.Send(NHelper.ArrayConcat(NFraming.Prefix, BitConverter.GetBytes(size)));

        Assert.True(await IsClosedByRemote(raw), $"size {size} must be refused");
        Assert.True(GC.GetTotalMemory(true) - before < 64 * 1024 * 1024, "must not allocate the announced size");
        await AssertServerStillWorks(host);
        await server.To.Close();
    }

    [Fact]
    public async Task PartialHeaderAndIdle_DoesNotAffectOtherClients()
    {
        var (server, host) = await EchoServer();
        using var slow = RawConnect(host);

        // slowloris style: only part of the header, then silence
        slow.Send(new[] { NFraming.Prefix[0], NFraming.Prefix[1], NFraming.Prefix[2] });

        await AssertServerStillWorks(host);
        Assert.False(await IsClosedByRemote(slow, 500), "server must not close an idle valid-so-far connection immediately");

        await server.To.Close();
    }

    [Fact]
    public async Task ByteByByteValidMessage_IsAccepted()
    {
        var (server, host) = await EchoServer();
        using var raw = RawConnect(host);
        raw.NoDelay = true;

        var body = new byte[] { 1, 2, 3, 4, 5 };
        var frame = NHelper.ArrayConcat(NFraming.Create(body.Length), body);

        foreach (var b in frame)
        {
            raw.Send(new[] { b });
            await Task.Delay(2);
        }

        var received = new byte[frame.Length];
        var offset = 0;
        raw.ReceiveTimeout = 5000;

        while (offset < received.Length) offset += raw.Receive(received, offset, received.Length - offset, SocketFlags.None);

        Assert.Equal(frame, received);
        await server.To.Close();
    }

    [Fact]
    public async Task ManyGarbageConnections_ServerSurvivesAndCleansUp()
    {
        const int total = 100;
        var (server, host) = await EchoServer();
        var random = new Random(3);

        for (var i = 0; i < total; i++)
        {
            using var raw = RawConnect(host);
            var garbage = new byte[64];
            random.NextBytes(garbage);
            raw.Send(garbage);
        }

        await Wait.Until(() => server.Clients.Count == 0, what: "server cleaned garbage clients");
        await AssertServerStillWorks(host);
        await server.To.Close();
    }

    [Fact]
    public async Task AbruptReset_IsCleanedUp()
    {
        var (server, host) = await EchoServer();
        var raw = RawConnect(host);

        await Wait.Until(() => server.Clients.Count == 1, what: "accept");

        // linger 0 sends RST instead of FIN
        raw.LingerState = new LingerOption(true, 0);
        raw.Close();

        await Wait.Until(() => server.Clients.Count == 0, what: "server removed reset client");
        await server.To.Close();
    }

    [Fact]
    public async Task ClientDisconnectInTheMiddleOfMessage_IsCleanedUp()
    {
        var (server, host) = await EchoServer();
        var raw = RawConnect(host);

        raw.Send(NHelper.ArrayConcat(NFraming.Create(1000), new byte[100]));
        await Wait.Until(() => server.Clients.Count == 1, what: "accept");
        raw.Close();

        await Wait.Until(() => server.Clients.Count == 0, what: "server removed client");
        await server.To.Close();
    }
}
