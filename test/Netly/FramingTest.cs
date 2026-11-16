namespace NetlyTest.Netly;

public class FramingTest
{
    [Fact]
    public void Start()
    {
        byte[]
            // create demo message
            message = "Hello World!"u8.ToArray(),
            // generate message header
            header = Framing.Create(message.LongLength);

        // create framing instance
        var framing = new Framing();

        // repeat allocation
        for (var i = 0; i < 10; i++)
        {
            // first deploy
            framing.Write(new ArraySegment<byte>(header), Helper.NewStream);
            var deploy1 = framing.Read(out var stream1);
            Assert.False(deploy1);
            Assert.Null(stream1);

            // last deploy
            framing.Write(new ArraySegment<byte>(message), Helper.NewStream);
            var deploy2 = framing.Read(out var stream2);
            Assert.True(deploy2);
            Assert.NotNull(stream2);
            Assert.Equal(message.Length, stream2.Length);

            // validate data
            var data = new byte[message.Length];
            Assert.Equal(0, stream2.Position);
            Assert.Equal(message.Length, stream2.Read(data, 0, data.Length));
            Assert.Equal(message, data);
            Assert.Equal(data.Length, stream2.Position);
        }
    }

    [Fact]
    public void Multiples()
    {
        const int times = 10;

        var random = new Random(1024);
        var messages = new List<byte[]>();

        // create buffer instance
        var buffer = new List<byte>();

        // create framing instance
        var framing = new Framing();

        // create multi call message
        for (var i = 0; i < times; i++)
        {
            // generate random message
            var message = new byte[random.Next(32, 128)];
            Random.Shared.NextBytes(message);
            // generate and send the message header
            buffer.AddRange(Framing.Create(message.LongLength));
            // send message payload
            buffer.AddRange(message);
            // save message payload to check
            messages.Add(message);
        }

        framing.Write(new ArraySegment<byte>(buffer.ToArray()), Helper.NewStream);
        buffer.Clear();

        var count = 0;

        while (framing.Read(out var stream))
        {
            var message = messages[count];
            Assert.NotNull(stream);
            Assert.Equal(message.Length, stream.Length);

            // validate data
            var data = new byte[message.Length];
            Assert.Equal(0, stream.Position);
            Assert.Equal(message.Length, stream.Read(data, 0, data.Length));
            Assert.Equal(message, data);
            Assert.Equal(data.Length, stream.Position);

            // increment total count
            count++;
        }

        Assert.Equal(times, count);
    }
}