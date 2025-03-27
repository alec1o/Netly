using Netly.Interfaces;

public class HTTP_xunit
{
    [Fact]
    public void InitServer()
    {
        var server = new HTTP.Server();

        Assert.False(server.IsOpened);
        Assert.NotNull(server.Host);
        Assert.NotNull(server.Map);
        Assert.NotNull(server.Middleware);
        Assert.NotNull(server.On);
        Assert.NotNull(server.To);
        Assert.Equal("http://0.0.0.0/", server.Host.ToString());
    }

    [Fact]
    public void InitClient()
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
    public async Task ServerConnection()
    {
        var server = new HTTP.Server();
        var index = -1;

        server.On.Modify(_ => index += 1);
        server.On.Open(() => index += 2);
        server.On.Error(_ => index += 4);
        server.On.Close(() => index += 8);

        Assert.Equal(-1, index);

        await server.To.Open(new("https://127.0.0.1:4001/"));
        Assert.Equal(2, index);
        Assert.True(server.IsOpened);

        await server.To.Close();
        Assert.Equal(10, index);
        Assert.False(server.IsOpened);

        await server.To.Open(new("https://1.1.1.1:4002/"));
        Assert.Equal(15, index);
        Assert.False(server.IsOpened);
    }

    [Fact]
    public async Task ClientConnection()
    {
        var server = new HTTP.Server();
        server.Map.All("/ping", (req, res) => res.Send(200, req.Queries["a"] + req.Headers["b"]));
        await server.To.Open(new("http://127.0.0.1:4002"));
        Assert.True(server.IsOpened);


        var client = new HTTP.Client();
        var index = -1;
        var status = 0;
        var message = string.Empty;

        client.Queries.Add("a", "hello ");
        client.Headers.Add("b", "world!");

        client.On.Modify(_ => index += 1);
        client.On.Open(e =>
        {
            status = e.Status;
            message = e.Body.Text;
            index += 2;
            Assert.True(client.IsOpened);
        });
        client.On.Error(_ => index += 4);
        client.On.Close(() => index += 8);

        Assert.Equal(-1, index);

        await client.To.Open("GET", "http://127.0.0.1:4002/ping");
        Assert.Equal(200, status);
        Assert.Equal("hello world!", message);
        Assert.False(client.IsOpened);
        Assert.Equal(10, index);
        index = 0;

        client.Timeout = 5000;
        await client.To.Open("GET", "http://127.0.0.1:2004/pong");
        Assert.Equal(1 + 4 + 8, index);
    }

    [Fact]
    public async Task HttpMethods()
    {
        var server = new HTTP.Server();
        var serverIndex = string.Empty;

        void ServerCallback(IHTTP.ServerRequest req, IHTTP.ServerResponse res)
        {
            serverIndex = req.Method.ToString().ToLower();
            res.Send(200);
        }

        async Task Go(HTTP.Client client, string path, string method)
        {
            await client.To.Open(method, $"http://127.0.0.1:4003{path}");
        }

        server.Map.Get("/get", ServerCallback);
        server.Map.Post("/post", ServerCallback);
        server.Map.Delete("/delete", ServerCallback);
        server.Map.Head("/head", ServerCallback);
        server.Map.Patch("/patch", ServerCallback);
        server.Map.Put("/put", ServerCallback);
        server.Map.All("/all", ServerCallback);

        await server.To.Open(new("https://127.0.0.1:4003"));

        Assert.True(server.IsOpened);

        var client = new HTTP.Client();

        await Go(client, "/get", "get");
        Assert.Equal("get", serverIndex);

        await Go(client, "/post", "post");
        Assert.Equal("post", serverIndex);

        await Go(client, "/delete", "delete");
        Assert.Equal("delete", serverIndex);

        await Go(client, "/head", "head");
        Assert.Equal("head", serverIndex);

        await Go(client, "/patch", "patch");
        Assert.Equal("patch", serverIndex);

        await Go(client, "/put", "put");
        Assert.Equal("put", serverIndex);

        // all
        await Go(client, "/all", "get");
        Assert.Equal("get", serverIndex);

        await Go(client, "/all", "post");
        Assert.Equal("post", serverIndex);

        await Go(client, "/all", "delete");
        Assert.Equal("delete", serverIndex);

        await Go(client, "/all", "delete");
        Assert.Equal("delete", serverIndex);

        await Go(client, "/all", "head");
        Assert.Equal("head", serverIndex);

        await Go(client, "/all", "patch");
        Assert.Equal("patch", serverIndex);

        await Go(client, "/all", "put");
        Assert.Equal("put", serverIndex);
    }

    [Fact]
    public async Task ServerMiddleware()
    {
        var server = new HTTP.Server();

        var customTimer = DateTime.UtcNow;
        var globalTimer = DateTime.UtcNow;

        var customCounter = 0;
        var globalCounter = 0;

        server.Middleware.Add("/custom", (req, res, nxt) =>
        {
            customTimer = DateTime.UtcNow;
            customCounter++;
            nxt();
        });

        server.Middleware.Add((req, res, nxt) =>
        {
            globalTimer = DateTime.UtcNow;
            globalCounter++;
            nxt();
        });

        server.Map.All("/", (req, res) =>
        {
            res.Send(403);
        });

        server.Map.All("/custom", (req, res) =>
        {
            res.Send(404);
        });

        await server.To.Open(new("https://127.0.0.1:4005"));

        Assert.True(server.IsOpened);

        var client = new HTTP.Client();
        Assert.Equal(0, customCounter);
        Assert.Equal(0, globalCounter);

        await client.To.Open("get", "http://127.0.0.1:4005/");
        Assert.Equal(0, customCounter);
        Assert.Equal(1, globalCounter);

        await client.To.Open("get", "http://127.0.0.1:4005/custom");
        Assert.Equal(1, customCounter);
        Assert.Equal(2, globalCounter);
        Assert.True(globalTimer >= customTimer);
    }

    [Fact]
    public async Task BodyParser()
    {
        var server = new HTTP.Server();

        var data = "*";

        server.Middleware.Add((req, res, next) =>
        {
            if (req.Enctype == HTTP.Enctype.Json)
            {
                req.Body.RegisterParser(true, (Type type) => "is json");
            }
            else if (req.Enctype == HTTP.Enctype.Xml)
            {
                req.Body.RegisterParser(true, (Type type) => "is xml");
            }
            else
            {
                req.Body.RegisterParser(true, (Type type) => "is empty");
            }

            next();
        });

        server.Map.Get("/demo", (req, res) =>
        {
            data = req.Body.Parse<string>();
            res.Send(200);
        });

        var url = "http://127.0.0.1:4007/demo";
        await server.To.Open(new(url));

        Assert.True(server.IsOpened);

        var client = new HTTP.Client();
        client.Timeout = 2000;
        client.On.Error(e => throw e);

        client.Headers["content-type"] = "application/json";
        await client.To.Open("GET", url);
        await Task.Delay(1000);
        Assert.Equal("is json", data);

        client.Headers["content-type"] = "application/xml";
        await client.To.Open("GET", url);
        await Task.Delay(1000);
        Assert.Equal("is xml", data);

        client.Headers["content-type"] = "text/html";
        await client.To.Open("GET", url);
        await Task.Delay(1000);
        Assert.Equal("is empty", data);
    }
}