using TcpClient = Netly.TcpClient;
using System.Security.Authentication;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Package;

public class TcpSecureTests
{
    private const string Password = "alecio";

    private static byte[] Read(Stream stream)
    {
        var buffer = new byte[stream.Length - stream.Position];
        var read = 0;
        while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
        return buffer;
    }

    private static async Task<(TcpServer server, Host host)> SecureServer(bool clientCertificateRequired = false,
        Func<System.Security.Cryptography.X509Certificates.X509Certificate, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, bool>? validate = null)
    {
        var host = Net.TcpHost();
        var server = new TcpServer(true);
        var opened = Wait.Signal<bool>();

        server.OnAccept(client => client.OnData(s => client.SendData(Read(s))));
        server.ToSecure(File.ReadAllBytes("certificate.p12"), Password, SslProtocols.Tls12, false,
            clientCertificateRequired);
        if (validate != null) server.OnSecure(validate);
        server.OnOpen(() => opened.TrySetResult(true));
        server.OnError(_ => opened.TrySetResult(false));
        server.Open(host);

        Assert.True(await Wait.For(opened, what: "server open"));
        return (server, host);
    }

    private static TcpClient NewClient(bool accept = true, byte[]? certificate = null)
    {
        var client = new TcpClient(true);
        client.ToSecure(true, certificate, certificate == null ? null : Password);
        if (accept) client.OnSecure((_, _, _) => true);
        return client;
    }

    private static async Task<bool> Open(TcpClient client, Host host)
    {
        var result = Wait.Signal<bool>();
        client.OnOpen(() => result.TrySetResult(true));
        client.OnError(_ => result.TrySetResult(false));
        client.Open(host);
        return await Wait.For(result, what: "client open result");
    }

    private static async Task Close(TcpClient client)
    {
        if (!client.IsOpened) return;

        var closed = Wait.Signal<bool>();
        client.OnClose(() => closed.TrySetResult(true));
        client.Close();
        await Wait.For(closed, what: "client close");
    }

    private static async Task Close(TcpServer server)
    {
        if (!server.IsOpened) return;

        var closed = Wait.Signal<bool>();
        server.OnClose(() => closed.TrySetResult(true));
        server.Close();
        await Wait.For(closed, what: "server close");
    }

    [Fact]
    public async Task ServerReportsSecure()
    {
        var (server, _) = await SecureServer();

        Assert.True(server.IsSecure);
        Assert.True(server.IsOpened);
        Assert.False(server.ClientCertificateRequired);

        await Close(server);
    }

    [Fact]
    public async Task SecureLargePayload()
    {
        var (server, host) = await SecureServer();
        var client = NewClient();
        var received = Wait.Signal<byte[]>();
        client.OnData(s => received.TrySetResult(Read(s)));

        Assert.True(await Open(client, host));
        Assert.True(client.IsSecure);

        var payload = Net.Payload(256 * 1024);
        client.SendData(payload);

        Assert.Equal(payload, await Wait.For(received, what: "large secure echo"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ClientCannotChangeSecureWhileOpened()
    {
        var (server, host) = await SecureServer();
        var client = NewClient();

        Assert.True(await Open(client, host));
        Assert.Throws<InvalidOperationException>(() => client.ToSecure(false));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task CertificateCallbackReceivesCertificate()
    {
        var (server, host) = await SecureServer();
        var client = NewClient(false);
        var seen = Wait.Signal<bool>();

        client.OnSecure((certificate, _, _) =>
        {
            seen.TrySetResult(certificate != null);
            return true;
        });

        Assert.True(await Open(client, host));
        Assert.True(await Wait.For(seen, what: "certificate callback"));
        Assert.True(client.IsOpened);

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task RejectedCertificate_DoesNotOpenConnection()
    {
        var (server, host) = await SecureServer();
        var client = NewClient(false);
        client.OnSecure((_, _, _) => false);

        Assert.False(await Open(client, host));
        Assert.False(client.IsOpened);

        await Close(server);
    }

    [Fact]
    public async Task MultipleCallbacks_AllMustAccept()
    {
        var (server, host) = await SecureServer();
        var client = NewClient(false);
        var first = 0;

        client.OnSecure((_, _, _) =>
        {
            first++;
            return true;
        });
        client.OnSecure((_, _, _) => false);

        Assert.False(await Open(client, host));
        Assert.Equal(1, first);
        Assert.False(client.IsOpened);

        await Close(server);
    }

    [Fact]
    public async Task ClientCertificateIsNotRequiredByDefault()
    {
        var (server, host) = await SecureServer();
        var client = NewClient();
        var echoed = Wait.Signal<string>();
        client.OnData(s => echoed.TrySetResult(Encoding.UTF8.GetString(Read(s))));

        Assert.True(await Open(client, host));
        client.SendData(Encoding.UTF8.GetBytes("hello"));

        Assert.Equal("hello", await Wait.For(echoed, what: "echo without client certificate"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ClientCertificateRequired_RejectsClientWithoutCertificate()
    {
        var (server, host) = await SecureServer(true);
        var client = NewClient();
        var echoed = 0;
        client.OnData(_ => Interlocked.Increment(ref echoed));

        // the handshake may finish on the client before the server refuses it
        if (await Open(client, host))
            client.SendData(Encoding.UTF8.GetBytes("hello"));

        await Wait.Never(() => echoed > 0, TimeSpan.FromSeconds(1), "client without certificate served");

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ClientCertificateRequired_AcceptsClientWithCertificate()
    {
        var (server, host) = await SecureServer(true, (certificate, _, _) => certificate != null);
        var client = NewClient(true, File.ReadAllBytes("certificate.p12"));
        var echoed = Wait.Signal<string>();
        client.OnData(s => echoed.TrySetResult(Encoding.UTF8.GetString(Read(s))));

        Assert.True(await Open(client, host));
        client.SendData(Encoding.UTF8.GetBytes("mutual"));

        Assert.Equal("mutual", await Wait.For(echoed, what: "echo with client certificate"));

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ClientCertificateRequired_ServerCallbackCanRefuse()
    {
        var (server, host) = await SecureServer(true, (_, _, _) => false);
        var client = NewClient(true, File.ReadAllBytes("certificate.p12"));
        var echoed = 0;
        client.OnData(_ => Interlocked.Increment(ref echoed));

        if (await Open(client, host))
            client.SendData(Encoding.UTF8.GetBytes("hello"));

        await Wait.Never(() => echoed > 0, TimeSpan.FromSeconds(1), "refused client served");

        await Close(client);
        await Close(server);
    }

    [Fact]
    public async Task ClientCertificateRequired_CannotChangeWhileOpened()
    {
        var (server, _) = await SecureServer(true);
        var pfx = File.ReadAllBytes("certificate.p12");

        Assert.Throws<InvalidOperationException>(() => server.ToSecure(pfx, Password, SslProtocols.Tls12, false));

        await Close(server);
    }

    [Fact]
    public async Task ManySecureClients_EchoIndependently()
    {
        const int total = 10;
        var (server, host) = await SecureServer();
        var clients = new List<TcpClient>();
        var echoed = 0;

        for (var i = 0; i < total; i++)
        {
            var client = NewClient();
            var expected = $"client-{i}";
            client.OnData(s =>
            {
                if (Encoding.UTF8.GetString(Read(s)) == expected) Interlocked.Increment(ref echoed);
            });

            Assert.True(await Open(client, host));
            client.SendData(Encoding.UTF8.GetBytes(expected));
            clients.Add(client);
        }

        await Wait.Until(() => echoed == total, what: "all secure echoes");

        foreach (var client in clients) await Close(client);
        await Close(server);
    }
}
