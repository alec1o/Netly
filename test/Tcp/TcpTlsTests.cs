using System.Security.Authentication;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpTlsTests
{
    private static async Task<(TCP.Server server, NHost host)> SecureServer()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        server.On.Accept(client => client.On.Data(s => client.To.Data(s.GetBytes())));
        server.To.Encryption(true, File.ReadAllBytes("certificate.p12"), "alecio", SslProtocols.Tls12);
        await server.To.Open(host);
        return (server, host);
    }

    [Fact]
    public async Task CertificateCallbackReceivesCertificate()
    {
        var (server, host) = await SecureServer();
        var client = new TCP.Client();
        var seen = Wait.Signal<bool>();
        client.To.Encryption(true);
        client.On.Encryption((certificate, _, _) =>
        {
            seen.TrySetResult(certificate != null);
            return true;
        });

        await client.To.Open(host);

        Assert.True(await Wait.For(seen, what: "certificate callback"));
        Assert.True(client.IsOpened);

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task RejectedCertificate_DoesNotOpenConnection()
    {
        var (server, host) = await SecureServer();
        var client = new TCP.Client();
        var opened = false;
        var failed = Wait.Signal<bool>();
        client.To.Encryption(true);
        client.On.Encryption((_, _, _) => false);
        client.On.Open(() => opened = true);
        client.On.Error(_ => failed.TrySetResult(true));

        await client.To.Open(host);

        Assert.False(opened);
        Assert.False(client.IsOpened);

        await server.To.Close();
    }

    [Fact]
    public async Task MultipleCallbacks_AllMustAccept()
    {
        var (server, host) = await SecureServer();
        var client = new TCP.Client();
        var first = 0;
        client.To.Encryption(true);
        client.On.Encryption((_, _, _) =>
        {
            first++;
            return true;
        });
        client.On.Encryption((_, _, _) => false);

        await client.To.Open(host);

        Assert.Equal(1, first);
        Assert.False(client.IsOpened);

        await server.To.Close();
    }

    [Fact]
    public async Task ManyEncryptedClients_EchoIndependently()
    {
        const int total = 10;
        var (server, host) = await SecureServer();
        var clients = new List<TCP.Client>();
        var echoed = 0;

        for (var i = 0; i < total; i++)
        {
            var client = new TCP.Client();
            var expected = $"client-{i}";
            client.To.Encryption(true);
            client.On.Encryption((_, _, _) => true);
            client.On.Data(s =>
            {
                if (System.Text.Encoding.UTF8.GetString(s.GetBytes()) == expected) Interlocked.Increment(ref echoed);
            });
            await client.To.Open(host);
            client.To.Data(expected);
            clients.Add(client);
        }

        await Wait.Until(() => echoed == total, what: "all encrypted echoes");

        foreach (var client in clients) await client.To.Close();
        await server.To.Close();
    }
}
