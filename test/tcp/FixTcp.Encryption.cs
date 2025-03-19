using System.Net;
using System.Security.Authentication;

public partial class FixTcp
{
    //[Fact]
    public void Encryption()
    {
        var host = HostManager.GenerateLocalHost();

        Server();

        void Client(bool useEncryption)
        {
            TCP.Client client = new();

            bool isOpen = false, isClose = false, isError = false, isModify = false;

            client.On.Open(() => isOpen = true);
            client.On.Close(() => isClose = true);
            client.On.Error(e =>
            {
                output.WriteLine(e.ToString());
                isError = true;
            });
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

                Thread.Sleep(millisecondsTimeout: 1000);
            }

            client.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 100);
            {
                Assert.Equal(useEncryption, client.IsEncrypted);
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            client.To.Close().Wait();

            Thread.Sleep(millisecondsTimeout: 100);
            {
                Assert.False(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.False(isError);
            }

            client.To.Open(new Host(IPAddress.Any, 0)).Wait();
            {
                Assert.False(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.True(isError);
            }

            client.To.Close().Wait();
        }

        void Server()
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
                byte[] pfxCertificate = File.ReadAllBytes("certificate.p12");
                const string pfxPassword = "alecio";
                output.WriteLine($"pfxCertificate: {pfxCertificate.Length}");
                server.To.Encryption(true, pfxCertificate, pfxPassword, SslProtocols.Tls12);
            }

            server.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 10);
            {
                Assert.True(server.IsEncrypted);
                Assert.True(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            Client(true);
            Client(false);

            server.To.Close().Wait();

            Thread.Sleep(millisecondsTimeout: 10);
            {
                Assert.False(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.False(isError);
            }

            // Cannot assign requested address
            server.To.Open(new Host("1.1.1.1", 0)).Wait();
            {
                Assert.False(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.True(isClose);
                Assert.True(isError);
            }

            server.To.Close().Wait();
        }
    }
}