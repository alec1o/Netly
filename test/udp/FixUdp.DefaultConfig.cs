public partial class FixUdp
{
    [Fact]
    public void DefaultConfig()
    {
        Client();
        Server();

        void Client()
        {
            var client = new UDP.Client();
            {
                Assert.False(client.IsOpened);
                Assert.False(string.IsNullOrWhiteSpace(client.Id));
                Assert.NotNull(client.Host);
                Assert.NotNull(client.On);
                Assert.NotNull(client.To);
            }
        }

        void Server()
        {
            var server = new UDP.Server();
            {
                Assert.False(server.IsOpened);
                Assert.False(string.IsNullOrWhiteSpace(server.Id));
                Assert.NotNull(server.Host);
                Assert.NotNull(server.On);
                Assert.NotNull(server.To);
                Assert.NotNull(server.Clients);
                Assert.Empty(server.Clients);
            }
        }
    }
}