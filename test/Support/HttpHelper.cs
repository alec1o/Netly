namespace NetlyTest.Support;

/// <summary>
///     Starts a http server on a free port and gives the base url.
/// </summary>
internal static class HttpHelper
{
    public static async Task<(HTTP.Server server, string url)> Start(Action<HTTP.Server> setup)
    {
        var server = new HTTP.Server();
        setup(server);

        var url = $"http://127.0.0.1:{Net.FreeTcpPort()}";
        await server.To.Open(new Uri(url));

        if (!server.IsOpened) throw new InvalidOperationException($"http server not opened: {url}");

        return (server, url);
    }

    public static async Task<(int status, string body)> Request(string method, string url, Action<HTTP.Client>? setup = null,
        string? body = null)
    {
        var client = new HTTP.Client();
        var done = Wait.Signal<(int, string)>();

        client.On.Open(response => done.TrySetResult((response.Status, response.Body.Text)));
        client.On.Error(e => done.TrySetException(e));
        setup?.Invoke(client);

        if (body == null)
            await client.To.Open(method, url);
        else
            await client.To.Open(method, url, body);

        return await Wait.For(done, what: $"{method} {url}");
    }
}
