using System.Net;
using System.Net.Sockets;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Udp;

public class UdpRobustnessTests
{
    private static readonly byte[] Hello = Encoding.ASCII.GetBytes("netly-udp-hello");

    private static async Task<(UDP.Server server, Host host)> EchoServer()
    {
        var host = Net.UdpHost();
        var server = new UDP.Server();
        server.On.Accept(client => client.On.Data(s =>
        {
            var data = s.GetBytes();
            if (!data.AsSpan().SequenceEqual(Hello)) client.To.Data(data);
        }));
        await server.To.Open(host);
        return (server, host);
    }

    private static async Task AssertServerStillWorks(Host host)
    {
        var client = new UDP.Client();
        var echoed = Wait.Signal<string>();
        client.On.Data(s => echoed.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));
        await client.To.Open(host);
        client.To.Data(Hello);
        await Task.Delay(100);
        client.To.Data("still alive");

        Assert.Equal("still alive", await Wait.For(echoed, what: "echo after garbage"));
        await client.To.Close();
    }

    [Fact]
    public async Task GarbageDatagrams_DoNotBreakServer()
    {
        var (server, host) = await EchoServer();
        var errors = 0;
        server.On.Error(_ => Interlocked.Increment(ref errors));
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var random = new Random(8);

        for (var i = 1; i < 300; i++)
        {
            var garbage = new byte[i];
            random.NextBytes(garbage);
            raw.SendTo(garbage, host.EndPoint);
        }

        await AssertServerStillWorks(host);
        Assert.True(server.IsOpened);

        await server.To.Close();
    }

    [Fact]
    public async Task EmptyDatagram_IsIgnored()
    {
        var (server, host) = await EchoServer();
        using var raw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        raw.SendTo(Array.Empty<byte>(), host.EndPoint);

        await AssertServerStillWorks(host);
        await server.To.Close();
    }

    [Fact]
    public async Task LargeDatagram_IsEchoed()
    {
        var (server, host) = await EchoServer();
        var client = new UDP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Data(s => received.TrySetResult(s.GetBytes()));
        await client.To.Open(host);
        client.To.Data(Hello);
        await Wait.Until(() => server.Clients.Length == 1, what: "accept");

        var payload = Net.Payload(60000);
        client.To.Data(payload);

        Assert.Equal(payload, await Wait.For(received, what: "60000 bytes echo"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ServerCanReopenAfterClose()
    {
        var host = Net.UdpHost();
        var server = new UDP.Server();

        for (var i = 0; i < 3; i++)
        {
            await server.To.Open(host);
            Assert.True(server.IsOpened);
            await server.To.Close();
            Assert.False(server.IsOpened);
        }
    }

    [Fact]
    public async Task ClientDataBeforeOpenIsIgnored()
    {
        var client = new UDP.Client();
        var error = false;
        client.On.Error(_ => error = true);

        client.To.Data("ignored");
        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.False(error);
    }

    [Fact]
    public async Task ManyShortLivedClients_ServerKeepsWorking()
    {
        var (server, host) = await EchoServer();

        for (var i = 0; i < 50; i++)
        {
            var client = new UDP.Client();
            await client.To.Open(host);
            client.To.Data(Hello);
            await client.To.Close();
        }

        await AssertServerStillWorks(host);
        await server.To.Close();
    }
}
