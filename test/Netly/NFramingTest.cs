namespace NetlyTest.Netly;

public class NFramingTest
{
    public NFramingTest(ITestOutputHelper output)
    {
        NetlyEnvironment.Logger.On(output.WriteLine);
        NetlyEnvironment.Logger.On((Exception exception) => output.WriteLine(exception.ToString()));
    }

    [Fact]
    public void Start()
    {
        // create demo message
        var message = "Hello World!"u8.ToArray();

        // generate framing header
        var header = NFraming.Create(message.LongLength);

        // create framing instance
        var framing = new NFraming();

        // repeat allocation
        for (var i = 0; i < 10; i++)
        {
            // first deploy
            var deploy1 = framing.Write(header, header.Length, NFraming.DefaultOnStream, out var stream1,
                out var close1, out var recall1);
            Assert.False(deploy1);
            Assert.False(close1);
            Assert.False(recall1);
            Assert.Null(stream1);

            // last deploy
            var deploy2 = framing.Write(message, message.Length, NFraming.DefaultOnStream, out var stream2,
                out var close2, out var recall2);
            Assert.True(deploy2);
            Assert.False(close2);
            Assert.False(recall2);
            Assert.NotNull(stream2);
            Assert.Equal(message.Length, stream2.Length);

            // validate data
            var data = new byte[message.Length];
            Assert.Equal(message.Length, stream2.Read(data, 0, data.Length));
            Assert.Equal(message, data);
        }
    }
}