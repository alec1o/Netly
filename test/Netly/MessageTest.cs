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
            sizeof(int) + // Prefix size
            sizeof(long) + // Package size (payload + header)
            sizeof(int) + // Message.Name size
            Name.Length; // Message.Name buffer

        Assert.Equal(size, header.Length);
    }
}