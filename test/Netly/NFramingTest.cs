using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NetlyTest.Netly;

public class NFramingTest
{
    public NFramingTest(ITestOutputHelper output)
    {
        // initialize logger
        NetlyEnvironment.Logger.On(output.WriteLine);
        NetlyEnvironment.Logger.On((Exception exception) => output.WriteLine(exception.ToString()));

        // create demo message
        MyMessage = "Hello World!"u8.ToArray();

        // generate message header
        MyHeader = NFraming.Create(MyMessage.LongLength);
    }

    private readonly byte[] MyMessage, MyHeader;

    [Fact]
    public void Start()
    {
        // create framing instance
        var framing = new NFraming();

        // repeat allocation
        for (var i = 0; i < 10; i++)
        {
            // first deploy
            framing.Write(MyHeader, MyHeader.Length, NFraming.NewStream);
            var deploy1 = framing.Read(out var stream1);
            Assert.False(deploy1);
            Assert.Null(stream1);

            // last deploy
            framing.Write(MyMessage, MyMessage.Length, NFraming.NewStream);
            var deploy2 = framing.Read(out var stream2);
            Assert.True(deploy2);
            Assert.NotNull(stream2);
            Assert.Equal(MyMessage.Length, stream2.Length);

            // validate data
            var data = new byte[MyMessage.Length];
            Assert.Equal(0, stream2.Position);
            Assert.Equal(MyMessage.Length, stream2.Read(data, 0, data.Length));
            Assert.Equal(MyMessage, data);
            Assert.Equal(data.Length, stream2.Position);
        }
    }

    [Fact]
    public void Multiples()
    {
        const int times = 10;

        // create buffer instance
        var buffer = new List<byte>();

        // create framing instance
        var framing = new NFraming();

        // create multi call message
        for (var i = 0; i < times; i++)
        {
            buffer.AddRange(MyHeader);
            buffer.AddRange(MyMessage);
        }

        framing.Write(buffer.ToArray(), buffer.Count, NFraming.NewStream);
        buffer.Clear();

        var count = 0;
        
        while (framing.Read(out var stream))
        {
            count++;
            Assert.NotNull(stream);
            Assert.Equal(MyMessage.Length, stream.Length);

            // validate data
            var data = new byte[MyMessage.Length];
            Assert.Equal(0, stream.Position);
            Assert.Equal(MyMessage.Length, stream.Read(data, 0, data.Length));
            Assert.Equal(MyMessage, data);
            Assert.Equal(data.Length, stream.Position);
        }

        Assert.Equal(times, count);
    }
}