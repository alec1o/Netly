public partial class FixTcp
{
    //[Fact]
    public void DefaultConfig()
    {
        Client();
        Server();

        void Client()
        {
            var client = new TCP.Client();
            {
                Assert.False(client.IsOpened);
                Assert.True(client.IsFraming);
                Assert.False(client.IsEncrypted);
                Assert.False(string.IsNullOrWhiteSpace(client.Id));
                Assert.NotNull(client.Host);
                Assert.NotNull(client.On);
                Assert.NotNull(client.To);
            }
        }

        void Server()
        {
            var server = new TCP.Server();
            {
                Assert.False(server.IsOpened);
                Assert.True(server.IsFraming);
                Assert.False(server.IsEncrypted);
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