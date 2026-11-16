using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Http;

public class HttpServerTests
{
    [Fact]
    public void DefaultConfig()
    {
        var server = new HTTP.Server();

        Assert.False(server.IsOpened);
        Assert.NotNull(server.Host);
        Assert.NotNull(server.Map);
        Assert.NotNull(server.Middleware);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
    }

    [Fact]
    public async Task OpenAndClose()
    {
        var server = new HTTP.Server();
        int open = 0, close = 0, modify = 0, error = 0;
        server.On.Open(() => Interlocked.Increment(ref open));
        server.On.Close(() => Interlocked.Increment(ref close));
        server.On.Modify(_ => Interlocked.Increment(ref modify));
        server.On.Error(_ => Interlocked.Increment(ref error));

        await server.To.Open(new Uri($"http://127.0.0.1:{Net.FreeTcpPort()}/"));

        Assert.True(server.IsOpened);
        Assert.Equal((1, 1, 0, 0), (open, modify, close, error));

        await server.To.Close();

        Assert.False(server.IsOpened);
        Assert.Equal(1, close);
        Assert.Equal(0, error);
    }

    [Fact]
    public async Task CanReopenAfterClose()
    {
        var port = Net.FreeTcpPort();
        var server = new HTTP.Server();
        server.Map.Get("/ping", (_, res) => res.Send(200, "pong"));

        for (var i = 0; i < 3; i++)
        {
            await server.To.Open(new Uri($"http://127.0.0.1:{port}/"));
            Assert.True(server.IsOpened);

            var (status, body) = await HttpHelper.Request("GET", $"http://127.0.0.1:{port}/ping");
            Assert.Equal((200, "pong"), (status, body));

            await server.To.Close();
            Assert.False(server.IsOpened);
        }
    }

    [Fact]
    public async Task OpenOnUnassignableAddressFails()
    {
        var server = new HTTP.Server();
        Exception? failure = null;
        server.On.Error(e => failure = e);

        await server.To.Open(new Uri("http://1.1.1.1:4002/"));

        Assert.False(server.IsOpened);
        Assert.NotNull(failure);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task MethodsReachTheirRoute(string method)
    {
        var received = string.Empty;

        void Callback(global::Netly.Interfaces.IHTTP.ServerRequest req, global::Netly.Interfaces.IHTTP.ServerResponse res)
        {
            received = req.Method.Method;
            res.Send(200, "ok");
        }

        var (server, url) = await HttpHelper.Start(s =>
        {
            s.Map.Get("/get", Callback);
            s.Map.Post("/post", Callback);
            s.Map.Put("/put", Callback);
            s.Map.Patch("/patch", Callback);
            s.Map.Delete("/delete", Callback);
        });

        var (status, body) = await HttpHelper.Request(method, $"{url}/{method.ToLower()}");

        Assert.Equal((200, "ok"), (status, body));
        Assert.Equal(method, received);

        await server.To.Close();
    }

    [Fact]
    public async Task HeadHasStatusAndNoBody()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Head("/head", (_, res) => res.Send(200)));

        var (status, body) = await HttpHelper.Request("HEAD", $"{url}/head");

        Assert.Equal(200, status);
        Assert.Equal(string.Empty, body);

        await server.To.Close();
    }

    [Fact]
    public async Task MapAllAcceptsEveryMethod()
    {
        var methods = new List<string>();
        var (server, url) = await HttpHelper.Start(s => s.Map.All("/all", (req, res) =>
        {
            lock (methods) methods.Add(req.Method.Method);
            res.Send(200);
        }));

        foreach (var method in new[] { "GET", "POST", "PUT", "PATCH", "DELETE" })
            Assert.Equal(200, (await HttpHelper.Request(method, $"{url}/all")).status);

        Assert.Equal(new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }, methods);

        await server.To.Close();
    }

    [Fact]
    public async Task WrongMethodOnRouteIsNotFound()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/only-get", (_, res) => res.Send(200)));

        Assert.Equal(404, (await HttpHelper.Request("POST", $"{url}/only-get")).status);

        await server.To.Close();
    }

    [Fact]
    public async Task UnknownPathIsNotFound()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/exists", (_, res) => res.Send(200)));

        Assert.Equal(404, (await HttpHelper.Request("GET", $"{url}/missing")).status);

        await server.To.Close();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task StatusCodeIsSent(int code)
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/status", (_, res) => res.Send(code)));

        Assert.Equal(code, (await HttpHelper.Request("GET", $"{url}/status")).status);

        await server.To.Close();
    }

    [Fact]
    public async Task RouteParamsAreParsed()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/user/{id}/post/{post}", (req, res) =>
            res.Send(200, $"{req.Params["id"]}:{req.Params["post"]}")));

        var (status, body) = await HttpHelper.Request("GET", $"{url}/user/42/post/abc");

        Assert.Equal((200, "42:abc"), (status, body));

        await server.To.Close();
    }

    [Fact]
    public async Task QueriesAndHeadersReachServer()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/echo", (req, res) =>
            res.Send(200, $"{req.Queries["a"]}|{req.Queries["b"]}|{req.Headers["x-test"]}")));

        var (status, body) = await HttpHelper.Request("GET", $"{url}/echo", c =>
        {
            c.Queries.Add("a", "1");
            c.Queries.Add("b", "two words");
            c.Headers.Add("x-test", "header-value");
        });

        Assert.Equal((200, "1|two words|header-value"), (status, body));

        await server.To.Close();
    }

    [Fact]
    public async Task ResponseHeadersReachClient()
    {
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/h", (_, res) =>
        {
            res.Headers["x-answer"] = "42";
            res.Send(200);
        }));

        var client = new HTTP.Client();
        var header = Wait.Signal<string>();
        client.On.Open(r => header.TrySetResult(r.Headers.TryGetValue("x-answer", out var value) ? value : "missing"));
        await client.To.Open("GET", $"{url}/h");

        Assert.Equal("42", await Wait.For(header));

        await server.To.Close();
    }

    [Fact]
    public async Task BodyIsReceivedAsTextAndBinary()
    {
        var text = string.Empty;
        byte[] binary = Array.Empty<byte>();

        var (server, url) = await HttpHelper.Start(s => s.Map.Post("/body", (req, res) =>
        {
            text = req.Body.Text;
            binary = req.Body.Binary;
            res.Send(200, req.Body.Text);
        }));

        var (status, echoed) = await HttpHelper.Request("POST", $"{url}/body", body: "olá ✓ body");

        Assert.Equal(200, status);
        Assert.Equal("olá ✓ body", text);
        Assert.Equal("olá ✓ body", echoed);
        Assert.Equal(Encoding.UTF8.GetBytes("olá ✓ body"), binary);

        await server.To.Close();
    }

    [Fact]
    public async Task LargeBodyRoundTrip()
    {
        var payload = new string('x', 2 * 1024 * 1024);
        var (server, url) = await HttpHelper.Start(s => s.Map.Post("/big", (req, res) => res.Send(200, req.Body.Text.Length.ToString())));

        var (status, body) = await HttpHelper.Request("POST", $"{url}/big", body: payload);

        Assert.Equal((200, payload.Length.ToString()), (status, body));

        await server.To.Close();
    }

    [Fact]
    public async Task BinaryResponse()
    {
        var data = Net.Payload(100 * 1024);
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/bin", (_, res) => res.Send(200, data)));

        var client = new HTTP.Client();
        var received = Wait.Signal<byte[]>();
        client.On.Open(r => received.TrySetResult(r.Body.Binary));
        await client.To.Open("GET", $"{url}/bin");

        Assert.Equal(data, await Wait.For(received));

        await server.To.Close();
    }

    [Fact]
    public async Task RedirectIsFollowedOrReported()
    {
        var (server, url) = await HttpHelper.Start(s =>
        {
            s.Map.Get("/from", (_, res) => res.Redirect(302, "/to"));
            s.Map.Get("/to", (_, res) => res.Send(200, "arrived"));
        });

        var (status, body) = await HttpHelper.Request("GET", $"{url}/from");

        Assert.True(status == 200 && body == "arrived" || status == 302, $"status {status}");

        await server.To.Close();
    }

    [Fact]
    public async Task MiddlewareRunsInOrderAndCanBeScoped()
    {
        var order = new List<string>();

        var (server, url) = await HttpHelper.Start(s =>
        {
            s.Middleware.Add((_, _, next) =>
            {
                lock (order) order.Add("global");
                next();
            });
            s.Middleware.Add("/scoped", (_, _, next) =>
            {
                lock (order) order.Add("scoped");
                next();
            });
            s.Map.Get("/scoped", (_, res) =>
            {
                lock (order) order.Add("route");
                res.Send(200);
            });
            s.Map.Get("/other", (_, res) =>
            {
                lock (order) order.Add("other");
                res.Send(200);
            });
        });

        await HttpHelper.Request("GET", $"{url}/scoped");
        await HttpHelper.Request("GET", $"{url}/other");

        Assert.Equal(new[] { "global", "scoped", "route", "global", "other" }, order);

        await server.To.Close();
    }

    [Fact]
    public async Task MiddlewareCanStopTheChain()
    {
        var routeCalled = false;

        var (server, url) = await HttpHelper.Start(s =>
        {
            s.Middleware.Add((_, res, _) => res.Send(401, "denied"));
            s.Map.Get("/secret", (_, res) =>
            {
                routeCalled = true;
                res.Send(200);
            });
        });

        var (status, body) = await HttpHelper.Request("GET", $"{url}/secret");

        Assert.Equal((401, "denied"), (status, body));
        Assert.False(routeCalled);

        await server.To.Close();
    }

    [Fact]
    public async Task ManyConcurrentRequests()
    {
        const int total = 100;
        var (server, url) = await HttpHelper.Start(s => s.Map.Get("/n/{value}", (req, res) => res.Send(200, req.Params["value"])));

        var results = await Task.WhenAll(Enumerable.Range(0, total).Select(async i =>
            (i, await HttpHelper.Request("GET", $"{url}/n/{i}"))));

        Assert.All(results, r => Assert.Equal((200, r.i.ToString()), r.Item2));

        await server.To.Close();
    }

    [Theory]
    [InlineData("noslash")]
    [InlineData("/a//b")]
    public async Task InvalidPathsAreIgnored(string path)
    {
        var called = false;
        var (server, url) = await HttpHelper.Start(s => s.Map.Get(path, (_, res) =>
        {
            called = true;
            res.Send(200);
        }));

        // invalid route is not mapped (silently), so request is not found
        Assert.Equal(404, (await HttpHelper.Request("GET", $"{url}/{path.TrimStart('/')}")).status);
        Assert.False(called);

        await server.To.Close();
    }
}
