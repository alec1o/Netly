using System.Diagnostics;
using NetlyTest.Support;
using Xunit.Abstractions;

namespace NetlyTest.WebSocket;

// threads of the process are counted, nothing else may run while it measures
[CollectionDefinition(nameof(WebSocketScaleTests), DisableParallelization = true)]
public class WebSocketScaleCollection;

[Collection(nameof(WebSocketScaleTests))]
public class WebSocketScaleTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ManyIdleClients_DoNotNeedAThreadEach()
    {
        const int total = 200;
        var server = new WebSocketServer();
        var uri = new Uri($"ws://127.0.0.1:{Net.FreeTcpPort()}/chat");
        await server.OpenAsync(uri);

        var before = Process.GetCurrentProcess().Threads.Count;
        var clients = new List<WebSocketClient>();
        var opened = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new WebSocketClient();
            client.OnOpen(() => Interlocked.Increment(ref opened));
            client.Open(uri);
            clients.Add(client);
        }

        await Wait.Until(() => opened == total && server.Clients.Count == total, TimeSpan.FromSeconds(60),
            "all connected");
        await Task.Delay(500);

        var grown = Process.GetCurrentProcess().Threads.Count - before;
        output.WriteLine($"{grown} more threads for {total} client + {total} server-side connections");

        // a thread by client would add 200 or more
        Assert.True(grown < 100, $"{grown} more threads");

        var echoed = 0;
        foreach (var client in clients) client.OnData((_, _) => Interlocked.Increment(ref echoed));
        foreach (var peer in server.Clients) peer.SendData(new byte[] { 1, 2, 3 });
        await Wait.Until(() => Volatile.Read(ref echoed) == total, TimeSpan.FromSeconds(30), "data to every client");

        foreach (var client in clients) client.Close();
        await server.CloseAsync();
    }
}
