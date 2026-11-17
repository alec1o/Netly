using Message = Netly.Message;
namespace NetlyTest.Netly;

public class MessageTest
{
    private const string Name = "Example";
    private static readonly byte[] Body = "Hello World!"u8.ToArray();

    [Fact]
    public void Start()
    {
        var header = Message.Create(Name, Body.LongLength);
        Assert.NotEmpty(header);
        var payload = Helper.ArrayConcat(header, Body);
        Assert.NotEmpty(payload);

        var stream = Helper.NewStream(payload.LongLength);
        stream.Write(payload);
        Assert.Equal(payload.LongLength, stream.Position);
        Assert.True(Message.TryParse(stream, out var myName, out var myMessage, Helper.NewStream));
        stream.Close();
        Assert.Equal(Name, myName);
        var data = new byte[myMessage.Length];
        Assert.Equal(Body.Length, myMessage.Read(data, 0, data.Length));
        Assert.Equal(Body, data);
    }

    [Fact]
    public void Create()
    {
        var header = Message.Create(Name, Body.Length);

        var size =
            sizeof(ushort) + // Prefix
            sizeof(ushort) + // Name size
            sizeof(ulong) + // Body size
            sizeof(byte) + // Checksum
            Name.Length; // Name

        Assert.Equal(size, header.Length);
        Assert.Equal(Message.HeaderSize + Name.Length, header.Length);
    }

    [Fact]
    public void Create_IsBigEndian()
    {
        var header = Message.Create("ab", 0x0102);

        Assert.Equal(new byte[] { 19, 36, 0x00, 0x02, 0, 0, 0, 0, 0, 0, 0x01, 0x02 },
            header.Take(12).ToArray());
        Assert.Equal((byte)'a', header[Message.HeaderSize]);
        Assert.Equal((byte)'b', header[Message.HeaderSize + 1]);
    }
}