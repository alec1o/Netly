using HttpClient = Netly.HttpClient;
using NetlyTest.Support;

namespace NetlyTest.Http;

public class HttpClientRetryTests
{
    [Fact]
    public void Retry_DefaultsAndValidation()
    {
        var client = new HttpClient();

        Assert.Equal(0, client.RetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), client.RetryDelay);

        client.UseRetry(3, TimeSpan.FromMilliseconds(50));
        Assert.Equal(3, client.RetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(50), client.RetryDelay);

        Assert.Throws<ArgumentOutOfRangeException>(() => client.UseRetry(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.UseRetry(1, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task Retry_SucceedsWhenTheServerComesBack()
    {
        var port = Net.FreeTcpPort();
        var url = $"http://127.0.0.1:{port}";
        var client = new HttpClient { Timeout = 2000 };
        var errors = 0;
        var closes = 0;
        var done = Wait.Signal<int>();

        client.UseRetry(40, TimeSpan.FromMilliseconds(100));
        client.OnError(_ => Interlocked.Increment(ref errors));
        client.OnClose(() => Interlocked.Increment(ref closes));
        client.OnOpen(response => done.TrySetResult(response.Status));
        client.Open("GET", $"{url}/ok");

        // the first attempts fail, the server starts later
        await Task.Delay(400);
        var server = new HttpServer();
        server.Map.Get("/ok", (_, res) => res.Send(200, "fine"));
        await server.OpenAsync(new Uri(url));

        Assert.Equal(200, await Wait.For(done, TimeSpan.FromSeconds(30), "response after retries"));
        await Wait.Until(() => closes == 1, what: "close");
        Assert.Equal(0, errors);

        await server.CloseAsync();
    }

    [Fact]
    public async Task Retry_GivesUpAndReportsOneError()
    {
        var client = new HttpClient { Timeout = 2000 };
        var errors = 0;
        var closed = Wait.Signal<bool>();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        client.UseRetry(2, TimeSpan.FromMilliseconds(200));
        client.OnError(_ => Interlocked.Increment(ref errors));
        client.OnClose(() => closed.TrySetResult(true));
        client.Open("GET", $"http://127.0.0.1:{Net.FreeTcpPort()}/none");

        await Wait.For(closed, TimeSpan.FromSeconds(30), "close after the last attempt");

        Assert.Equal(1, errors);
        // two waits between the three attempts
        Assert.True(watch.ElapsedMilliseconds >= 350, $"{watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Retry_NotForInvalidUrl()
    {
        var client = new HttpClient();
        var errors = 0;
        var closed = Wait.Signal<bool>();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        client.UseRetry(5, TimeSpan.FromSeconds(5));
        client.OnError(_ => Interlocked.Increment(ref errors));
        client.OnClose(() => closed.TrySetResult(true));
        client.Open("GET", "not a url");

        await Wait.For(closed, what: "close");

        Assert.Equal(1, errors);
        Assert.True(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Retry_CloseCancelsTheWait()
    {
        var client = new HttpClient { Timeout = 2000 };
        var errors = 0;
        var closed = Wait.Signal<bool>();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        client.UseRetry(5, TimeSpan.FromSeconds(10));
        client.OnError(_ => Interlocked.Increment(ref errors));
        client.OnClose(() => closed.TrySetResult(true));
        client.Open("GET", $"http://127.0.0.1:{Net.FreeTcpPort()}/none");

        await Task.Delay(300);
        client.Close();

        await Wait.For(closed, TimeSpan.FromSeconds(5), "close after Close()");

        Assert.Equal(1, errors);
        Assert.True(watch.ElapsedMilliseconds < 5000, $"{watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Close_DoesNotBreakTheNextRequests()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/ok", (_, res) => res.Send(200, "fine")));
        var client = new HttpClient();

        // a Close with nothing running used to cancel every later request of the client
        client.Close();

        var (status, body) = await HttpHelper.Request("GET", $"{url}/ok", c =>
        {
            c.Close();
        });
        Assert.Equal((200, "fine"), (status, body));

        var again = Wait.Signal<int>();
        client.OnOpen(r => again.TrySetResult(r.Status));
        client.Open("GET", $"{url}/ok");
        Assert.Equal(200, await Wait.For(again, what: "request after Close"));

        await server.CloseAsync();
    }

    [Fact]
    public async Task Headers_ReachTheServer()
    {
        string? authorization = null, custom = null, contentType = null;
        var (server, url) = await HttpHelper.Start(s => s.Map.Post("/h", (req, res) =>
        {
            req.Headers.TryGetValue("Authorization", out authorization);
            req.Headers.TryGetValue("X-Custom", out custom);
            req.Headers.TryGetValue("Content-Type", out contentType);
            res.Send(200, "ok");
        }));

        var (status, _) = await HttpHelper.Request("POST", $"{url}/h", c =>
        {
            c.Headers["Authorization"] = "Bearer token";
            c.Headers["X-Custom"] = "value";
            c.Headers["Content-Type"] = "text/plain";
        }, "body");

        Assert.Equal(200, status);
        Assert.Equal("Bearer token", authorization);
        Assert.Equal("value", custom);
        Assert.StartsWith("text/plain", contentType);

        await server.CloseAsync();
    }

    [Fact]
    public async Task OpenWhileRunning_ThrowsInvalidOperation()
    {
        var release = new ManualResetEventSlim();
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/slow", (_, res) =>
        {
            release.Wait(TimeSpan.FromSeconds(10));
            res.Send(200);
        }));
        var client = new HttpClient();
        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));

        client.Open("GET", $"{url}/slow");

        Assert.Throws<InvalidOperationException>(() => client.Open("GET", $"{url}/slow"));
        Assert.Throws<InvalidOperationException>(() => client.Timeout = 1000);

        release.Set();
        await Wait.For(closed, what: "close");
        await server.CloseAsync();
    }

    [Fact]
    public void Timeout_RejectsInvalidValue()
    {
        var client = new HttpClient();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.Timeout = -5);

        client.Timeout = -1;
        client.Timeout = 0;
        client.Timeout = 1234;
        Assert.Equal(1234, client.Timeout);
    }
}
