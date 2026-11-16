namespace NetlyTest.Support;

/// <summary>
///     Awaitable open and close for the non blocking Rudp API, they finish by the same callbacks the user receives.
/// </summary>
internal static class RudpLifecycle
{
    public static async Task OpenAsync(this RudpServer server, Host host)
    {
        var done = Wait.Signal<bool>();
        server.OnOpen(() => done.TrySetResult(true));
        server.OnError(_ => done.TrySetResult(false));
        server.Open(host);
        await Wait.For(done, what: "server open result");
    }

    public static async Task OpenAsync(this RudpClient client, Host host)
    {
        var done = Wait.Signal<bool>();
        client.OnOpen(() => done.TrySetResult(true));
        client.OnError(_ => done.TrySetResult(false));
        client.Open(host);
        await Wait.For(done, TimeSpan.FromSeconds(30), "client open result");
    }

    public static async Task CloseAsync(this RudpServer server)
    {
        if (!server.IsOpened) return;

        var done = Wait.Signal<bool>();
        server.OnClose(() => done.TrySetResult(true));
        server.Close();
        await Wait.For(done, what: "server close");
    }

    public static async Task CloseAsync(this RudpClient client)
    {
        if (!client.IsOpened) return;

        var done = Wait.Signal<bool>();
        client.OnClose(() => done.TrySetResult(true));
        client.Close();
        await Wait.For(done, what: "client close");
    }
}

/// <summary>
///     The Rudp API has no string overloads, tests send text as utf8.
/// </summary>
internal static class RudpText
{
    public static void SendData(this RudpClient client, string text, RudpDelivery delivery = RudpDelivery.Reliable)
    {
        client.SendData(System.Text.Encoding.UTF8.GetBytes(text), delivery);
    }

    public static void SendEvent(this RudpClient client, string name, string text,
        RudpDelivery delivery = RudpDelivery.Reliable)
    {
        client.SendEvent(name, System.Text.Encoding.UTF8.GetBytes(text), delivery);
    }

    public static void BroadcastData(this RudpServer server, string text, RudpDelivery delivery = RudpDelivery.Reliable)
    {
        server.BroadcastData(System.Text.Encoding.UTF8.GetBytes(text), delivery);
    }
}
