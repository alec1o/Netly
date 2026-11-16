using System.Text;

namespace NetlyTest.Netly;

public class NMessageEdgeTest
{
    private static Stream Memory(long size) => new MemoryStream((int)size);

    private static MemoryStream StreamOf(byte[] bytes) => new(bytes);

    [Theory]
    [InlineData("")]
    public void Create_InvalidName_Throws(string name)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NMessage.Create(name, 10));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Create_InvalidMessageSize_Throws(long size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NMessage.Create("name", size));
    }

    [Fact]
    public void RoundTrip_Utf8Name()
    {
        const string name = "evento.ação.✓";
        var body = Encoding.UTF8.GetBytes("corpo");
        var payload = NHelper.ArrayConcat(NMessage.Create(name, body.Length), body);

        Assert.True(NMessage.TryParse(StreamOf(payload), out var parsedName, out var message, Memory));
        Assert.Equal(name, parsedName);
        Assert.Equal(body, ((MemoryStream)message).ToArray());
    }

    [Fact]
    public void RoundTrip_LargeBody()
    {
        var body = new byte[1024 * 1024];
        new Random(5).NextBytes(body);
        var payload = NHelper.ArrayConcat(NMessage.Create("big", body.Length), body);

        Assert.True(NMessage.TryParse(StreamOf(payload), out var name, out var message, Memory));
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

            Assert.False(NMessage.TryParse(StreamOf(garbage), out var name, out var message, Memory));
            Assert.Null(name);
            Assert.Null(message);
        }
    }

    [Fact]
    public void TryParse_TruncatedMessage_ReturnsFalse()
    {
        var body = new byte[50];
        var payload = NHelper.ArrayConcat(NMessage.Create("name", body.Length), body);

        for (var cut = 1; cut < payload.Length; cut++)
        {
            var truncated = payload.AsSpan(0, cut).ToArray();
            Assert.False(NMessage.TryParse(StreamOf(truncated), out _, out _, Memory), $"cut {cut}");
        }
    }

    [Fact]
    public void TryParse_WrongBodySize_ReturnsFalse()
    {
        var payload = NHelper.ArrayConcat(NMessage.Create("name", 10), new byte[20]);

        Assert.False(NMessage.TryParse(StreamOf(payload), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_HostileSizes_NeverAllocateStream()
    {
        var header = NMessage.Create("name", 10);
        var hostile = (byte[])header.Clone();

        // overwrite declared message size with long.MaxValue
        BitConverter.GetBytes(long.MaxValue).CopyTo(hostile, NMessage.Prefix.Length + sizeof(int));

        var called = false;
        var result = NMessage.TryParse(StreamOf(NHelper.ArrayConcat(hostile, new byte[10])), out _, out _, size =>
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
        var header = NMessage.Create("name", 10);
        var hostile = (byte[])header.Clone();

        // overwrite declared name size with int.MaxValue
        BitConverter.GetBytes(int.MaxValue).CopyTo(hostile, NMessage.Prefix.Length);

        Assert.False(NMessage.TryParse(StreamOf(NHelper.ArrayConcat(hostile, new byte[10])), out _, out _, Memory));
    }

    [Fact]
    public void TryParse_EmptyStream_ReturnsFalse()
    {
        Assert.False(NMessage.TryParse(StreamOf(Array.Empty<byte>()), out _, out _, Memory));
    }
}
