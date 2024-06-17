using System.Net;
using System.Reflection;
using System.Security.Authentication;

public partial class FixTcp
{
    [Fact]
    public void Encryption()
    {
        var host = HostManager.GenerateLocalHost();

        Server();

        async Task Client(bool useEncryption)
        {
            TCP.Client client = new();

            bool isOpen = false, isClose = false, isError = false, isModify = false;

            client.On.Open(() => isOpen = true);
            client.On.Close(() => isClose = true);
            client.On.Error(_ => isError = true);
            client.On.Modify(_ => isModify = true);

            {
                Assert.False(client.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
                Assert.False(client.IsEncrypted);
            }

            if (useEncryption)
            {
                client.To.Encryption(true);
                client.On.Encryption((certificate, chain, errors) =>
                {
                    // on production must check this.
                    const bool isValid = true;
                    return isValid;
                });
            }

            await client.To.Open(host);

            Thread.Sleep(millisecondsTimeout: 100);
            {
                if (useEncryption)
                {
                    Assert.True(client.IsEncrypted);
                }
                else
                {
                    Assert.False(client.IsEncrypted);
                }

                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            await client.To.Close();

            Thread.Sleep(millisecondsTimeout: 100);
            {
                Assert.False(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.False(isError);
            }

            await client.To.Open(new Host(IPAddress.Any, 0));
            {
                Assert.False(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.True(isError);
            }

            await client.To.Close();
        }

        async void Server()
        {
            TCP.Server server = new();

            bool isOpen = false, isClose = false, isError = false, isModify = false;

            server.On.Open(() => isOpen = true);
            server.On.Close(() => isClose = true);
            server.On.Modify(_ => isModify = true);
            server.On.Error(exception =>
            {
                isError = true;
                output.WriteLine(exception.ToString());
            });

            {
                Assert.False(server.IsEncrypted);
                Assert.False(server.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
            }


            {
                byte[] pfxCertificate = await File.ReadAllBytesAsync("certificate.p12");
                const string pfxPassword = "alecio";
                server.To.Encryption(true, pfxCertificate, pfxPassword, SslProtocols.Tls13);
            }

            await server.To.Open(host);

            Thread.Sleep(millisecondsTimeout: 10);
            {
                Assert.True(server.IsEncrypted);
                Assert.True(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            await Client(true);
            await Client(false);

            await server.To.Close();

            Thread.Sleep(millisecondsTimeout: 10);
            {
                Assert.False(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.False(isError);
            }

            // Cannot assign requested address
            await server.To.Open(new Host("1.1.1.1", 0));
            {
                Assert.False(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.True(isError);
            }

            await server.To.Close();
        }
    }
}