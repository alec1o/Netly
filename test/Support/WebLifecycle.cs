using HttpClient = Netly.HttpClient;
using System.Net.WebSockets;

namespace NetlyTest.Support;

/// <summary>
///     Awaitable open and close for the non blocking Http and WebSocket API, they finish by the same callbacks the user receives.
/// </summary>
internal static class WebLifecycle
{
    public static async Task OpenAsync(this HttpServer server, Uri host)
    {
        var done = Wait.Signal<bool>();
        server.OnOpen(() => done.TrySetResult(true));
        server.OnError(_ => done.TrySetResult(false));
        server.Open(host);
        await Wait.For(done, what: "http server open result");
    }

    public static async Task CloseAsync(this HttpServer server)
    {
        if (!server.IsOpened) return;

        var done = Wait.Signal<bool>();
        server.OnClose(() => done.TrySetResult(true));
        server.Close();
        await Wait.For(done, what: "http server close");
    }

    public static async Task OpenAsync(this WebSocketServer server, Uri host)
    {
        var done = Wait.Signal<bool>();
        server.OnOpen(() => done.TrySetResult(true));
        server.OnError(_ => done.TrySetResult(false));
        server.Open(host);
        await Wait.For(done, what: "websocket server open result");
    }

    public static async Task CloseAsync(this WebSocketServer server)
    {
        if (!server.IsOpened) return;

        var done = Wait.Signal<bool>();
        server.OnClose(() => done.TrySetResult(true));
        server.Close();
        await Wait.For(done, what: "websocket server close");
    }

    // a request is over when OnClose is called, with or without error
    public static async Task OpenAsync(this HttpClient client, string method, string url, string? body = null)
    {
        var done = Wait.Signal<bool>();
        client.OnClose(() => done.TrySetResult(true));

        if (body == null)
            client.Open(method, url);
        else
            client.Open(method, url, body);

        await Wait.For(done, TimeSpan.FromSeconds(30), $"request {method} {url}");
    }

    public static async Task OpenAsync(this WebSocketClient client, Uri host)
    {
        var done = Wait.Signal<bool>();
        client.OnOpen(() => done.TrySetResult(true));
        client.OnError(_ => done.TrySetResult(false));
        client.Open(host);
        await Wait.For(done, what: "websocket open result");
    }

    public static async Task CloseAsync(this WebSocketClient client)
    {
        if (!client.IsOpened) return;

        var done = Wait.Signal<bool>();
        client.OnClose(() => done.TrySetResult(true));
        client.Close();
        await Wait.For(done, what: "websocket close");
    }

    public static async Task CloseAsync(this WebSocketClient client, WebSocketCloseStatus status)
    {
        if (!client.IsOpened) return;

        var done = Wait.Signal<bool>();
        client.OnClose(() => done.TrySetResult(true));
        client.Close(status);
        await Wait.For(done, what: "websocket close");
    }

    public static void SendData(this WebSocketClient client, string text, WebSocketDelivery delivery = WebSocketDelivery.Binary)
    {
        client.SendData(System.Text.Encoding.UTF8.GetBytes(text), delivery);
    }

    public static void SendEvent(this WebSocketClient client, string name, string text,
        WebSocketDelivery delivery = WebSocketDelivery.Binary)
    {
        client.SendEvent(name, System.Text.Encoding.UTF8.GetBytes(text), delivery);
    }
}
