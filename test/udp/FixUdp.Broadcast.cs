public partial class FixUdp
{
    [Fact]
    public async Task Broadcast()
    {
        if(IsMac()) return;
        
        await Server();

        async Task Server()
        {
            var host = HostManager.GenerateLocalHost();

            UDP.Server server = new();

            object dataLock = new();
            object eventLock = new();
            int allDataReceived = 0, allEventReceived = 0;
            bool isOpen = false, isClose = false, isError = false, isModify = false;

            server.On.Open(() => isOpen = true);
            server.On.Close(() => isClose = true);
            server.On.Modify(_ => isModify = true);
            server.On.Error(exception =>
            {
                isError = true;
                output.WriteLine(exception.ToString());
            });
            server.On.Accept(client =>
            {
                client.On.Data(data =>
                {
                    // used to open connection
                    if (data.Length == 1 && data[0] == 0) return;
                    
                    lock (dataLock)
                    {
                        allDataReceived++;
                    }
                });

                client.On.Event((name, data) =>
                {
                    lock (eventLock)
                    {
                        allEventReceived++;
                    }
                });
            });

            {
                Assert.False(server.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
            }

            await server.To.Open(host);

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
                await Client(server.Host);
            }

            // broadcast
            server.To.DataBroadcast(Guid.NewGuid().ToString());
            server.To.EventBroadcast(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // wait for client respond broadcast
            Thread.Sleep(300);

            Assert.Equal(maxConnection, server.Clients.Length);
            Assert.Equal(maxConnection, allDataReceived);
            Assert.Equal(maxConnection, allEventReceived);
        }

        async Task Client(Host host)
        {
            UDP.Client client = new();
            bool isOpen = false, isClose = false, isError = false, isModify = false;

            client.On.Open(() => isOpen = true);
            client.On.Close(() => isClose = true);
            client.On.Error(_ => isError = true);
            client.On.Modify(_ => isModify = true);
            client.On.Data(bytes => client.To.Data(bytes));
            client.On.Event((name, bytes) => client.To.Event(name, bytes));
            {
                Assert.False(client.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
            }

            await client.To.Open(host);

            // for open connection
            client.To.Data([0]);

            Thread.Sleep(millisecondsTimeout: 10);
            {
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
            }
        }
    }
}