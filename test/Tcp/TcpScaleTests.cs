using TcpClient = Netly.TcpClient;
using NetlyTest.Support;
using Xunit.Abstractions;

namespace NetlyTest.Tcp;

// the heap of the whole process is measured, nothing else may run while it does
[CollectionDefinition(nameof(TcpScaleTests), DisableParallelization = true)]
public class TcpScaleCollection;

[Collection(nameof(TcpScaleTests))]
public class TcpScaleTests(ITestOutputHelper output)
{
    [Fact]
    public async Task IdleConnections_UseLittleManagedMemory()
    {
        const int total = 1000;
        var host = Net.TcpHost();
        var server = new TcpServer();
        var serverOpen = Wait.Signal<bool>();
        server.OnOpen(() => serverOpen.TrySetResult(true));
        server.Open(host);
        Assert.True(await Wait.For(serverOpen, what: "server open"));

        var before = Collect();

        var clients = new List<TcpClient>();
        var opened = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new TcpClient();
            client.OnOpen(() => Interlocked.Increment(ref opened));
            client.Open(host);
            clients.Add(client);
        }

        await Wait.Until(() => opened == total && server.Clients.Count == total, TimeSpan.FromSeconds(60),
            "all connections");

        var after = Collect();
        var perSocket = (after - before) / (total * 2);

        output.WriteLine($"{perSocket} bytes of managed memory by socket ({total} client + {total} server-side)");

        // the receive buffer of the socket (128 KB) must not be allocated for every idle connection
        Assert.True(perSocket < 16 * 1024, $"{perSocket} bytes by socket");

        foreach (var client in clients) client.Close();
        server.Close();
    }

    [Fact]
    public async Task IdleConnections_StillReceiveAfterBeingIdle()
    {
        var host = Net.TcpHost();
        var server = new TcpServer();
        var received = Wait.Signal<byte[]>();
        var serverOpen = Wait.Signal<bool>();
        server.OnAccept(c => c.OnData(s =>
        {
            var buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            received.TrySetResult(buffer);
        }));
        server.OnOpen(() => serverOpen.TrySetResult(true));
        server.Open(host);
        Assert.True(await Wait.For(serverOpen, what: "server open"));

        var client = new TcpClient();
        var clientOpen = Wait.Signal<bool>();
        client.OnOpen(() => clientOpen.TrySetResult(true));
        client.Open(host);
        Assert.True(await Wait.For(clientOpen, what: "client open"));

        // the connection stays idle, then data arrives
        await Task.Delay(500);
        var payload = Net.Payload(100 * 1024, 4);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "data after idle"));

        client.Close();
        server.Close();
    }

    private static long Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(true);
    }
}
