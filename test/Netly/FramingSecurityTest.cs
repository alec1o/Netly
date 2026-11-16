namespace NetlyTest.Netly;

public class FramingSecurityTest
{
    private static byte[] Frame(byte[] body) => Helper.ArrayConcat(Framing.Create(body.Length), body);

    private static List<byte[]> ReadAll(Framing framing)
    {
        var list = new List<byte[]>();
        while (framing.Read(out var stream)) list.Add(((MemoryStream)stream).ToArray());
        return list;
    }

    private static Stream Memory(long size) => new MemoryStream((int)size);

    [Fact]
    public void HeaderAboveMaxSize_Throws_AndNeverAllocatesStream()
    {
        var framing = new Framing { MaxSize = 1024 };
        var called = false;

        var header = Framing.Create(1025);

        Assert.Throws<InvalidDataException>(() => framing.Write(new ArraySegment<byte>(header), size =>
        {
            called = true;
            return Memory(size);
        }));
        Assert.False(called);
    }

    [Fact]
    public void HugeHeader_Throws_AndNeverAllocatesStream()
    {
        var framing = new Framing();
        var called = false;
        var header = Framing.Create(long.MaxValue);

        Assert.Throws<InvalidDataException>(() => framing.Write(new ArraySegment<byte>(header), size =>
        {
            called = true;
            return Memory(1);
        }));
        Assert.False(called);
    }

    [Fact]
    public void SizeEqualToMaxSize_IsAccepted()
    {
        var framing = new Framing { MaxSize = 8 };
        var body = new byte[8];

        framing.Write(new ArraySegment<byte>(Frame(body)), Memory);

        Assert.Single(ReadAll(framing));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void InvalidSize_Throws(long size)
    {
        var framing = new Framing();
        var header = Helper.ArrayConcat(Framing.Prefix, BitConverter.GetBytes(size));

        Assert.Throws<InvalidDataException>(() => framing.Write(new ArraySegment<byte>(header), Memory));
    }

    [Fact]
    public void InvalidPrefix_Throws()
    {
        var framing = new Framing();
        var header = Helper.ArrayConcat(new byte[] { 9, 9, 9, 9 }, BitConverter.GetBytes(4L));

        Assert.Throws<InvalidDataException>(() => framing.Write(new ArraySegment<byte>(header), Memory));
    }

    [Fact]
    public void HeaderAndBodySentByteByByte_AreReassembled()
    {
        var framing = new Framing();
        var body = Enumerable.Range(1, 50).Select(x => (byte)x).ToArray();

        foreach (var b in Frame(body))
            framing.Write(new ArraySegment<byte>(new[] { b }), Memory);

        var result = ReadAll(framing);
        Assert.Single(result);
        Assert.Equal(body, result[0]);
    }

    [Fact]
    public void ManyMessagesInOneSegment_AreSplitCorrectly()
    {
        var framing = new Framing();
        var bodies = Enumerable.Range(1, 30).Select(i => Enumerable.Repeat((byte)i, i).ToArray()).ToList();
        var all = Helper.ArrayConcat(bodies.Select(Frame).ToArray());

        framing.Write(new ArraySegment<byte>(all), Memory);

        var result = ReadAll(framing);
        Assert.Equal(bodies.Count, result.Count);
        for (var i = 0; i < bodies.Count; i++) Assert.Equal(bodies[i], result[i]);
    }

    [Fact]
    public void SegmentWithOffset_IsRespected()
    {
        var framing = new Framing();
        var body = new byte[] { 1, 2, 3, 4, 5 };
        var frame = Frame(body);
        var padded = Helper.ArrayConcat(new byte[] { 0xFF, 0xFF, 0xFF }, frame, new byte[] { 0xEE });

        framing.Write(new ArraySegment<byte>(padded, 3, frame.Length), Memory);

        Assert.Equal(body, Assert.Single(ReadAll(framing)));
    }

    [Fact]
    public void IncompleteMessage_IsNotReadable()
    {
        var framing = new Framing();
        var frame = Frame(new byte[100]);

        framing.Write(new ArraySegment<byte>(frame, 0, frame.Length - 1), Memory);

        Assert.False(framing.Read(out _));
    }

    [Fact]
    public void CustomStreamFactory_CanUseDisk()
    {
        var framing = new Framing();
        var files = new List<string>();
        var body = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();

        framing.Write(new ArraySegment<byte>(Frame(body)), size =>
        {
            var path = Path.GetTempFileName();
            files.Add(path);
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096,
                FileOptions.DeleteOnClose);
        });

        Assert.True(framing.Read(out var stream));
        using (stream)
        {
            var read = new byte[stream.Length];
            stream.Position = 0;
            Assert.Equal(read.Length, stream.Read(read, 0, read.Length));
            Assert.Equal(body, read);
        }
    }

    [Fact]
    public void WriteAfterClose_Throws()
    {
        var framing = new Framing();
        framing.Close();

        Assert.Throws<InvalidOperationException>(() =>
            framing.Write(new ArraySegment<byte>(Frame(new byte[4])), Memory));
    }

    [Fact]
    public void Reopen_DiscardsPartialMessage()
    {
        var framing = new Framing();
        var frame = Frame(new byte[10]);
        framing.Write(new ArraySegment<byte>(frame, 0, 6), Memory);

        framing.Open();
        framing.Write(new ArraySegment<byte>(Frame(new byte[] { 7, 7 })), Memory);

        Assert.Equal(new byte[] { 7, 7 }, Assert.Single(ReadAll(framing)));
    }
}
