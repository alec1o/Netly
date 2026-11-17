using Message = Netly.Message;
using System.Text;

namespace NetlyTest.Netly;

public class MessageEdgeTest
{
    private static Stream Memory(long size) => new MemoryStream((int)size);

    private static MemoryStream StreamOf(byte[] bytes) => new(bytes);

    // a hostile peer sends a valid checksum for the sizes it chose
    private static byte[] Sign(byte[] header)
    {
        header[Message.HeaderSize - 1] = Framing.Checksum(header, Message.HeaderSize - 1);
        return header;
    }

    private static void PutBodySize(byte[] header, ulong size)
    {
        for (var i = 0; i < sizeof(ulong); i++) header[Message.Prefix.Length + 2 + i] = (byte)(size >> (8 * (7 - i)));
    }

    [Theory]
    [InlineData("")]
    public void Create_InvalidName_Throws(string name)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Message.Create(name, 10));
    }

    [Fact]
    public void Create_NegativeMessageSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Message.Create("name", -1));
    }

    [Fact]
    public void RoundTrip_EmptyBody()
    {
        var payload = Message.Create("ping", 0);

        Assert.True(Message.TryParse(StreamOf(payload), out var name, out var message, Memory));
        Assert.Equal("ping", name);
        Assert.Equal(0, message.Length);
    }

    [Fact]
    public void RoundTrip_Utf8Name()
    {
        const string name = "evento.ação.✓";
        var body = Encoding.UTF8.GetBytes("corpo");
        var payload = Helper.ArrayConcat(Message.Create(name, body.Length), body);

        Assert.True(Message.TryParse(StreamOf(payload), out var parsedName, out var message, Memory));
        Assert.Equal(name, parsedName);
        Assert.Equal(body, ((MemoryStream)message).ToArray());
    }

    [Fact]
    public void RoundTrip_LargeBody()
    {
        var body = new byte[1024 * 1024];
        new Random(5).NextBytes(body);
        var payload = Helper.ArrayConcat(Message.Create("big", body.Length), body);

        Assert.True(Message.TryParse(StreamOf(payload), out var name, out var message, Memory));
        Assert.Equal("big", name);
        Assert.Equal(body, ((MemoryStream)message).ToArray());
    }

    [Fact]
    public void TryParse_RandomGarbage_ReturnsFalse()
    {
        var random = new Random(11);

        for (var i = 1; i < 200; i++)
        {
            var garbage = new byte[i];
            random.NextBytes(garbage);
            garbage[0] = 0; // never starts with the prefix

            Assert.False(Message.TryParse(StreamOf(garbage), out var name, out var message, Memory));
            Assert.Null(name);
            Assert.Null(message);
        }
    }

    [Fact]
    public void TryParse_TruncatedMessage_ReturnsFalse()
    {
        var body = new byte[50];
        var payload = Helper.ArrayConcat(Message.Create("name", body.Length), body);

        for (var cut = 1; cut < payload.Length; cut++)
        {
            var truncated = payload.AsSpan(0, cut).ToArray();
            Assert.False(Message.TryParse(StreamOf(truncated), out _, out _, Memory), $"cut {cut}");
        }
    }

    [Fact]
    public void TryParse_WrongBodySize_ReturnsFalse()
    {
        var payload = Helper.ArrayConcat(Message.Create("name", 10), new byte[20]);

        Assert.False(Message.TryParse(StreamOf(payload), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_HostileSizes_NeverAllocateStream()
    {
        var header = Message.Create("name", 10);
        var hostile = (byte[])header.Clone();

        // overwrite declared message size with long.MaxValue
        PutBodySize(hostile, long.MaxValue);
        Sign(hostile);

        var called = false;
        var result = Message.TryParse(StreamOf(Helper.ArrayConcat(hostile, new byte[10])), out _, out _, size =>
        {
            called = true;
            return Memory(1);
        });

        Assert.False(result);
        Assert.False(called);
    }

    [Fact]
    public void TryParse_HostileNameSize_ReturnsFalse()
    {
        var header = Message.Create("name", 10);
        var hostile = (byte[])header.Clone();

        // overwrite declared name size with ushort.MaxValue
        hostile[Message.Prefix.Length] = 0xFF;
        hostile[Message.Prefix.Length + 1] = 0xFF;
        Sign(hostile);

        Assert.False(Message.TryParse(StreamOf(Helper.ArrayConcat(hostile, new byte[10])), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_EmptyStream_ReturnsFalse()
    {
        Assert.False(Message.TryParse(StreamOf(Array.Empty<byte>()), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_BodySizeAboveLong_DoesNotAllocate()
    {
        var hostile = Message.Create("name", 0);
        var body = new byte[10];

        PutBodySize(hostile, ulong.MaxValue);
        Sign(hostile);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var called = false;

        Assert.False(Message.TryParse(StreamOf(Helper.ArrayConcat(hostile, body)), out _, out _, size =>
        {
            called = true;
            return Memory(1);
        }));
        Assert.False(called);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 1024 * 1024);
    }

    [Fact]
    public void TryParse_WrongChecksum_IsData()
    {
        var payload = Helper.ArrayConcat(Message.Create("name", 4), new byte[4]);

        payload[Message.HeaderSize - 1] ^= 0xFF;

        Assert.False(Message.TryParse(StreamOf(payload), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_WrongPrefix_IsData()
    {
        var header = Message.Create("name", 4);

        header[0] ^= 0xFF;
        Sign(header);

        Assert.False(Message.TryParse(StreamOf(Helper.ArrayConcat(header, new byte[4])), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_NameNotUtf8_IsData()
    {
        var header = Message.Create("name", 4);

        // 0xFF is never valid in UTF-8
        header[Message.HeaderSize] = 0xFF;

        Assert.False(Message.TryParse(StreamOf(Helper.ArrayConcat(header, new byte[4])), out var name, out _, Memory));
        Assert.Null(name);
    }

    [Fact]
    public void Create_NameAboveUshort_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Message.Create(new string('a', ushort.MaxValue + 1), 1));
        Assert.NotEmpty(Message.Create(new string('a', ushort.MaxValue), 1));
    }
}
