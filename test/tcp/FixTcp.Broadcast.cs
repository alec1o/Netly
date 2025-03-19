public partial class FixTcp
{
    //[Fact]
    public void Broadcast()
    {
        Server();

        void Server()
        {
            var host = HostManager.GenerateLocalHost();

            TCP.Server server = new();

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

            server.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 10);
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
                Client(server.Host);
            }

            // broadcast
            server.To.DataBroadcast(Guid.NewGuid().ToString());
            server.To.EventBroadcast(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // wait for client respond broadcast
            Thread.Sleep(1000);

            Assert.Equal(maxConnection, server.Clients.Length);
            Assert.Equal(maxConnection, allDataReceived);
            Assert.Equal(maxConnection, allEventReceived);
        }

        void Client(Host host)
        {
            TCP.Client client = new();
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

            client.To.Open(host).Wait();

            Thread.Sleep(millisecondsTimeout: 20);
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