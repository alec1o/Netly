using System.Security.Authentication;
using System.Text;
using NetlyTest.Support;

namespace NetlyTest.Tcp;

public class TcpEncryptionTests
{
    private const string Password = "alecio";

    private static async Task<(TCP.Server server, NHost host)> SecureEchoServer()
    {
        var host = Net.TcpHost();
        var server = new TCP.Server();
        server.On.Accept(client => client.On.Data(s => client.To.Data(s.GetBytes())));
        server.To.Encryption(true, File.ReadAllBytes("certificate.p12"), Password, SslProtocols.Tls12);
        await server.To.Open(host);
        return (server, host);
    }

    [Fact]
    public async Task ServerReportsEncryption()
    {
        var (server, _) = await SecureEchoServer();
        Assert.True(server.IsEncrypted);
        Assert.True(server.IsOpened);
        await server.To.Close();
    }

    [Fact]
    public async Task EncryptedEcho()
    {
        var (server, host) = await SecureEchoServer();
        var client = new TCP.Client();
        var received = Wait.Signal<string>();
        client.To.Encryption(true);
        client.On.Encryption((_, _, _) => true);
        client.On.Data(s => received.TrySetResult(Encoding.UTF8.GetString(s.GetBytes())));

        await client.To.Open(host);

        Assert.True(client.IsEncrypted);
        Assert.True(client.IsOpened);

        client.To.Data("segredo");
        Assert.Equal("segredo", await Wait.For(received, what: "encrypted echo"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task EncryptedLargePayload()
    {
        var (server, host) = await SecureEchoServer();
        var client = new TCP.Client();
        var received = Wait.Signal<byte[]>();
        client.To.Encryption(true);
        client.On.Encryption((_, _, _) => true);
        client.On.Data(s => received.TrySetResult(s.GetBytes()));
        await client.To.Open(host);

        var payload = Net.Payload(256 * 1024);
        client.To.Data(payload);

        Assert.Equal(payload, await Wait.For(received, what: "large encrypted echo"));

        await client.To.Close();
        await server.To.Close();
    }

    [Fact]
    public async Task ClientCannotChangeEncryptionWhileOpened()
    {
        var (server, host) = await SecureEchoServer();
        var client = new TCP.Client();
        client.To.Encryption(true);
        client.On.Encryption((_, _, _) => true);
        await client.To.Open(host);

        Assert.Throws<InvalidOperationException>(() => client.To.Encryption(false));

        await client.To.Close();
        await server.To.Close();
    }
}
