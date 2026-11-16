using System.Net;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpLifecycleTests
{
    [Fact]
    public void DefaultConfig()
    {
        var client = new TCP.Client();
        Assert.False(client.IsOpened);
        Assert.True(client.IsFraming);
        Assert.False(client.IsEncrypted);
        Assert.False(string.IsNullOrWhiteSpace(client.Id));
        Assert.NotNull(client.Host);
        Assert.NotNull(client.On);
        Assert.NotNull(client.To);

        var server = new TCP.Server();
        Assert.False(server.IsOpened);
        Assert.True(server.IsFraming);
        Assert.False(server.IsEncrypted);
        Assert.False(string.IsNullOrWhiteSpace(server.Id));
        Assert.NotNull(server.Host);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
        Assert.Empty(server.Clients);
    }

    [Fact]
    public void FramingCanBeDisabled()
    {
        Assert.False(new TCP.Client(false).IsFraming);
        Assert.False(new TCP.Server(false).IsFraming);
    }

    [Fact]
    public void IdsAreUnique()
    {
        Assert.NotEqual(new TCP.Client().Id, new TCP.Client().Id);
        Assert.NotEqual(new TCP.Server().Id, new TCP.Server().Id);
    }

    [Fact]
    public async Task ServerOpenAndClose()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();

        int open = 0, close = 0, modify = 0, error = 0;
        server.On.Open(() => Interlocked.Increment(ref open));
        server.On.Close(() => Interlocked.Increment(ref close));
        server.On.Modify(_ => Interlocked.Increment(ref modify));
        server.On.Error(_ => Interlocked.Increment(ref error));

        await server.To.Open(host);

        Assert.True(server.IsOpened);
        Assert.Equal(1, open);
        Assert.Equal(1, modify);
        Assert.Equal(0, close);
        Assert.Equal(0, error);
        Assert.Equal(host.Port, server.Host.Port);

        await server.To.Close();

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    }

    [Fact]
    public async Task ServerCanReopenAfterClose()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        var open = 0;
        server.On.Open(() => Interlocked.Increment(ref open));

        for (var i = 0; i < 3; i++)
        {
            await server.To.Open(host);
            Assert.True(server.IsOpened);
            await server.To.Close();
            Assert.False(server.IsOpened);
        }

        Assert.Equal(3, open);
    }

    [Fact]
    public async Task ServerOpenOnUnassignableAddressFails()
    {
        var server = new TCP.Server();
        Exception? failure = null;
        server.On.Error(e => failure = e);

        await server.To.Open(new Host("1.1.1.1", 0));

        Assert.False(server.IsOpened);
        Assert.NotNull(failure);
    }

    [Fact]
    public async Task ServerOpenOnBusyPortFails()
    {
        var host = Net.TcpHost();
        var first = new TCP.Server();
        var second = new TCP.Server();
        Exception? failure = null;
        second.On.Error(e => failure = e);

        await first.To.Open(host);
        await second.To.Open(host);

        Assert.True(first.IsOpened);
        Assert.False(second.IsOpened);
        Assert.NotNull(failure);

        await first.To.Close();
    }

    [Fact]
    public async Task ClientOpenAndClose()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var client = new TCP.Client();
        int open = 0, close = 0, modify = 0, error = 0;
        client.On.Open(() => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Modify(_ => Interlocked.Increment(ref modify));
        client.On.Error(_ => Interlocked.Increment(ref error));

        Assert.False(client.IsOpened);

        await client.To.Open(host);

        Assert.True(client.IsOpened);
        Assert.Equal(1, open);
        Assert.Equal(1, modify);
        Assert.Equal(0, close);
        Assert.Equal(0, error);
        Assert.Equal(host.Port, client.Host.Port);

        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);

        await server.To.Close();
    }

    [Fact]
    public async Task ClientCanReconnect()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var client = new TCP.Client();
        var open = 0;
        client.On.Open(() => Interlocked.Increment(ref open));

        for (var i = 0; i < 3; i++)
        {
            await client.To.Open(host);
            Assert.True(client.IsOpened);
            await client.To.Close();
            Assert.False(client.IsOpened);
        }

        Assert.Equal(3, open);
        await server.To.Close();
    }

    [Fact]
    public async Task ClientReconnectIgnoresOldReceive()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var client = new TCP.Client();
        Exception? error = null;
        client.On.Error(e => error = e);

        for (var i = 0; i < 100; i++)
        {
            await client.To.Open(host);
            Assert.True(client.IsOpened, $"closed by old receive callback (round {i}) error: {error}");
            await client.To.Close();
        }

        await server.To.Close();
    }

    [Fact]
    public async Task ClientConnectToClosedPortRaisesError()
    {
        var client = new TCP.Client();
        Exception? failure = null;
        var opened = false;
        client.On.Open(() => opened = true);
        client.On.Error(e => failure = e);

        await client.To.Open(Net.TcpHost());

        Assert.False(client.IsOpened);
        Assert.False(opened);
        Assert.NotNull(failure);
    }

    [Fact]
    public async Task ClientDataBeforeOpenIsIgnored()
    {
        var client = new TCP.Client();
        var error = false;
        client.On.Error(_ => error = true);

        client.To.Data("ignored");
        client.To.Event("name", "ignored");
        await client.To.Close();

        Assert.False(client.IsOpened);
        Assert.False(error);
    }

    [Fact]
    public async Task ClientObservesServerClose()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        var client = new TCP.Client();
        var closed = Wait.Signal<bool>();
        client.On.Close(() => closed.TrySetResult(true));

        await server.To.Open(host);
        await client.To.Open(host);
        await server.To.Close();

        await Wait.For(closed, what: "client OnClose after server close");
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async Task ServerCloseDoesNotLeakClientsStillBeingAccepted()
    {
        const int rounds = 30, total = 5;

        for (var round = 0; round < rounds; round++)
        {
            var host = Net.TcpHost();
            var server = new TCP.Server();
            var closed = 0;
            await server.To.Open(host);

            var clients = new List<TCP.Client>();

            for (var i = 0; i < total; i++)
            {
                var client = new TCP.Client();
                client.On.Close(() => Interlocked.Increment(ref closed));
                clients.Add(client);
            }

            await Task.WhenAll(clients.Select(c => c.To.Open(host)));

            // close without waiting accept, connections can be in flight
            await server.To.Close();

            await Wait.Until(() => closed == total, what: $"all clients closed (round {round})");
        }
    }

    [Fact]
    public async Task IsOpenedDoesNotBlock()
    {
        const int total = 200;
        var host = Net.TcpHost();
        var server = new TCP.Server();
        await server.To.Open(host);

        var clients = Enumerable.Range(0, total).Select(_ => new TCP.Client()).ToList();
        await Task.WhenAll(clients.Select(c => c.To.Open(host)));
        await Wait.Until(() => server.Clients.Count == total, what: "all accepts");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var opened = clients.Count(c => c.IsOpened) + server.Clients.Count(c => c.IsOpened);
        watch.Stop();

        Assert.Equal(total * 2, opened);
        Assert.True(watch.ElapsedMilliseconds < 200, $"IsOpened took {watch.ElapsedMilliseconds} ms");

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }

    [Fact]
    public async Task ServerObservesClientClose()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        var accepted = Wait.Signal<global::Netly.Interfaces.ITCP.Client>();
        var closed = Wait.Signal<bool>();
        server.On.Accept(c =>
        {
            c.On.Close(() => closed.TrySetResult(true));
            accepted.TrySetResult(c);
        });

        await server.To.Open(host);

        var client = new TCP.Client();
        await client.To.Open(host);
        await Wait.For(accepted, what: "server accept");
        Assert.Single(server.Clients);

        await client.To.Close();
        await Wait.For(closed, what: "server-side client OnClose");
        await Wait.Until(() => server.Clients.Count == 0, what: "server clients cleanup");

        await server.To.Close();
    }

    [Fact]
    public async Task ServerAcceptsManyClients()
    {
        const int total = 20;
        var host = Net.TcpHost();
        var server = new TCP.Server();
        var accepted = 0;
        server.On.Accept(_ => Interlocked.Increment(ref accepted));

        await server.To.Open(host);

        var clients = Enumerable.Range(0, total).Select(_ => new TCP.Client()).ToList();
        await Task.WhenAll(clients.Select(c => c.To.Open(host)));

        await Wait.Until(() => accepted == total, what: "all accepts");
        Assert.Equal(total, server.Clients.Count);
        Assert.All(clients, c => Assert.True(c.IsOpened));
        Assert.Equal(total, server.Clients.Select(c => c.Id).Distinct().Count());

        await Task.WhenAll(clients.Select(c => c.To.Close()));
        await server.To.Close();
    }
}
