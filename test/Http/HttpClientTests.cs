using NetlyTest.Support;

namespace NetlyTest.Http;

public class HttpClientTests
{
    [Fact]
    public void DefaultConfig()
    {
        var client = new HTTP.Client();

        Assert.False(client.IsOpened);
        Assert.Empty(client.Headers);
        Assert.Empty(client.Queries);
        Assert.NotNull(client.To);
        Assert.NotNull(client.On);
        Assert.Equal(15000, client.Timeout);
    }

    [Fact]
    public async Task EventsOnSuccess()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/ok", (_, res) => res.Send(200, "fine")));
        var client = new HTTP.Client();
        int open = 0, close = 0, modify = 0, error = 0;
        client.On.Open(_ => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Modify(_ => Interlocked.Increment(ref modify));
        client.On.Error(_ => Interlocked.Increment(ref error));

        await client.To.Open("GET", $"{url}/ok");

        Assert.Equal((1, 1, 1, 0), (open, modify, close, error));
        Assert.False(client.IsOpened);

        await server.To.Close();
    }

    [Fact]
    public async Task ConnectionRefusedRaisesError()
    {
        var client = new HTTP.Client { Timeout = 3000 };
        int open = 0, close = 0, error = 0;
        client.On.Open(_ => Interlocked.Increment(ref open));
        client.On.Close(() => Interlocked.Increment(ref close));
        client.On.Error(_ => Interlocked.Increment(ref error));

        await client.To.Open("GET", $"http://127.0.0.1:{Net.FreeTcpPort()}/none");

        Assert.Equal((0, 1, 1), (open, error, close));
    }

    [Fact]
    public async Task TimeoutRaisesError()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/slow", (_, res) =>
        {
            Thread.Sleep(2000);
            res.Send(200);
        }));
        var client = new HTTP.Client { Timeout = 300 };
        var error = Wait.Signal<Exception>();
        client.On.Error(e => error.TrySetResult(e));

        await client.To.Open("GET", $"{url}/slow");

        Assert.NotNull(await Wait.For(error, what: "timeout error"));

        await server.To.Close();
    }

    [Fact]
    public async Task ClientIsReusable()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/n", (req, res) => res.Send(200, req.Queries["i"])));
        var client = new HTTP.Client();
        var body = string.Empty;
        client.On.Open(r => body = r.Body.Text);

        for (var i = 0; i < 5; i++)
        {
            client.Queries["i"] = i.ToString();
            await client.To.Open("GET", $"{url}/n");
            Assert.Equal(i.ToString(), body);
        }

        await server.To.Close();
    }

    [Fact]
    public async Task InvalidUrlRaisesError()
    {
        var client = new HTTP.Client();
        var error = false;
        client.On.Error(_ => error = true);

        await client.To.Open("GET", "not a url");

        Assert.True(error);
    }
}
