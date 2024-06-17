public partial class FixUdp
{
    [Fact]
    public async Task SendAndReceive()
    {
        if (IsMac()) return;

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
                    client.To.Data(data);

                    lock (dataLock)
                    {
                        allDataReceived++;
                    }
                });

                client.On.Event((name, data) =>
                {
                    client.To.Event(name, data);

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

            Thread.Sleep(300);

            Assert.Equal(maxConnection, server.Clients.Length);
            Assert.Equal(maxConnection, allDataReceived);
            Assert.Equal(maxConnection, allEventReceived);
        }

        async Task Client(Host host)
        {
            UDP.Client client = new();

            bool isOpen = false, isClose = false, isError = false, isModify = false;
            byte[] dataSent = Guid.NewGuid().ToString().GetBytes();
            List<byte> dataReceived = [];
            (string name, byte[] data) eventSent = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString().GetBytes());
            (string name, byte[] data) eventReceived = (string.Empty, []);

            client.On.Open(() => isOpen = true);
            client.On.Close(() => isClose = true);
            client.On.Error(_ => isError = true);
            client.On.Modify(_ => isModify = true);
            client.On.Data(bytes => dataReceived.AddRange(bytes));
            client.On.Event((name, bytes) => eventReceived = (name, bytes));
            {
                Assert.False(client.IsOpened);
                Assert.False(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.False(isModify);
                Assert.NotEmpty(dataSent); // data
                Assert.NotEmpty(eventSent.name); // event
                Assert.NotEmpty(eventSent.data); // event
                Assert.Empty(dataReceived); // data
                Assert.Empty(eventReceived.name); // event
                Assert.Empty(eventReceived.data); // event
            }

            await client.To.Open(host);

            client.To.Data(dataSent);
            client.To.Event(eventSent.name, eventSent.data);

            Thread.Sleep(millisecondsTimeout: 100);
            {
                Assert.True(client.IsOpened);
                Assert.True(isModify);
                Assert.True(isOpen);
                Assert.False(isClose);
                Assert.False(isError);
                Assert.NotEmpty(dataSent); // data
                Assert.NotEmpty(eventSent.name); // event
                Assert.NotEmpty(eventSent.data); // event
                Assert.NotEmpty(dataReceived); // data
                Assert.NotEmpty(eventReceived.name); // event
                Assert.NotEmpty(eventReceived.data); // event
                Assert.Equal(dataSent, dataReceived); // data
                Assert.Equal(eventSent.name, eventReceived.name); // event
                Assert.Equal(eventSent.data, eventReceived.data); // event
            }
        }
    }
}