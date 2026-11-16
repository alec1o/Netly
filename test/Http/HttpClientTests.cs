using HttpClient = Netly.HttpClient;
using NetlyTest.Support;

namespace NetlyTest.Http;

public class HttpClientTests
{
    [Fact]
    public void DefaultConfig()
    {
        var client = new HttpClient();

        Assert.False(client.IsOpened);
        Assert.Empty(client.Headers);
        Assert.Empty(client.Queries);
        Assert.Equal(15000, client.Timeout);
    }

    [Fact]
    public async Task EventsOnSuccess()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/ok", (_, res) => res.Send(200, "fine")));
        var client = new HttpClient();
        int open = 0, close = 0, modify = 0, error = 0;
        client.OnOpen(_ => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnModify(_ => Interlocked.Increment(ref modify));
        client.OnError(_ => Interlocked.Increment(ref error));

        await client.OpenAsync("GET", $"{url}/ok");

        Assert.Equal((1, 1, 1, 0), (open, modify, close, error));
        Assert.False(client.IsOpened);

        await server.CloseAsync();
    }

    [Fact]
    public async Task ConnectionRefusedRaisesError()
    {
        var client = new HttpClient { Timeout = 3000 };
        int open = 0, close = 0, error = 0;
        client.OnOpen(_ => Interlocked.Increment(ref open));
        client.OnClose(() => Interlocked.Increment(ref close));
        client.OnError(_ => Interlocked.Increment(ref error));

        await client.OpenAsync("GET", $"http://127.0.0.1:{Net.FreeTcpPort()}/none");

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
        var client = new HttpClient { Timeout = 300 };
        var error = Wait.Signal<Exception>();
        client.OnError(e => error.TrySetResult(e));

        await client.OpenAsync("GET", $"{url}/slow");

        Assert.NotNull(await Wait.For(error, what: "timeout error"));

        await server.CloseAsync();
    }

    [Fact]
    public async Task ClientIsReusable()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/n", (req, res) => res.Send(200, req.Queries["i"])));
        var client = new HttpClient();
        var body = string.Empty;
        client.OnOpen(r => body = r.Body.Text);

        for (var i = 0; i < 5; i++)
        {
            client.Queries["i"] = i.ToString();
            await client.OpenAsync("GET", $"{url}/n");
            Assert.Equal(i.ToString(), body);
        }

        await server.CloseAsync();
    }

    [Fact]
    public async Task InvalidUrlRaisesError()
    {
        var client = new HttpClient();
        var error = false;
        client.OnError(_ => error = true);

        await client.OpenAsync("GET", "not a url");

        Assert.True(error);
    }
}
