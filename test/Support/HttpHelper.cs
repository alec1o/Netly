using HttpClient = Netly.HttpClient;
namespace NetlyTest.Support;

/// <summary>
///     Starts a http server on a free port and gives the base url.
/// </summary>
internal static class HttpHelper
{
    public static async Task<(HttpServer server, string url)> Start(Action<HttpServer> setup)
    {
        var server = new HttpServer();
        setup(server);

        var url = $"http://127.0.0.1:{Net.FreeTcpPort()}";
        await server.OpenAsync(new Uri(url));

        if (!server.IsOpened) throw new InvalidOperationException($"http server not opened: {url}");

        return (server, url);
    }

    public static async Task<(int status, string body)> Request(string method, string url, Action<HttpClient>? setup = null,
        string? body = null)
    {
        var client = new HttpClient();
        var done = Wait.Signal<(int, string)>();

        client.OnOpen(response => done.TrySetResult((response.Status, response.Body.Text)));
        client.OnError(e => done.TrySetException(e));
        setup?.Invoke(client);

        if (body == null)
            await client.OpenAsync(method, url);
        else
            await client.OpenAsync(method, url, body);

        return await Wait.For(done, what: $"{method} {url}");
    }
}
