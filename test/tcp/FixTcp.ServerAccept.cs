using System.Net;

public partial class FixTcp
{
    //[Fact]
    public async Task ServerAccept()
    {
        await Server();

        async Task Client(Host host)
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
            }

            await client.To.Open(host);

            client.To.Data(Guid.NewGuid().ToString());

            Thread.Sleep(millisecondsTimeout: 250);
            {
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }
        }

        async Task Server()
        {
            var host = HostManager.GenerateLocalHost();

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
                Assert.False(server.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
            }

            await server.To.Open(host);

            Thread.Sleep(millisecondsTimeout: 1000);
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
                await Client(server.Host);
            }

            Thread.Sleep(5000);

            Assert.Equal(maxConnection, server.Clients.Length);
        }
    }
}
