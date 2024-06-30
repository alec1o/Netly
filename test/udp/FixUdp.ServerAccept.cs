using System.Net;

public partial class FixUdp
{
    [Fact]
    public void ServerAccept()
    {
        Server();

        void Client(Host host)
        {
            UDP.Client client = new();

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
            }

            client.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 20);
            {
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            client.To.Data(Guid.NewGuid().ToString());
            client.To.Event("ACTION", "CONNECTION");
            Thread.Sleep(millisecondsTimeout: 20);
        }

        void Server()
        {
            var host = HostManager.GenerateLocalHost();

            UDP.Server server = new();

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
                Assert.False(server.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
            }

            server.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 100);
            {
                Assert.True(server.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            const int maxConnection = 100;

            for (int i = 0; i < maxConnection; i++)
            {
                Client(host);
            }

            Thread.Sleep(1000);

            Assert.Equal(maxConnection, server.Clients.Length);
        }
    }
}