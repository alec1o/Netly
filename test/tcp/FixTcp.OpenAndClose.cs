using System.Net;

public partial class FixTcp(ITestOutputHelper output)
{
    [Fact]
    public async Task OpenAndClose()
    {
        var host = HostManager.GenerateLocalHost();

        await Server();

        async Task Client()
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

            Thread.Sleep(millisecondsTimeout: 1000);
            {
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }

            await client.To.Close();

            Thread.Sleep(millisecondsTimeout: 1000);
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

        async Task Server()
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

            await Client();

            await server.To.Close();

            Thread.Sleep(millisecondsTimeout: 1000);
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
