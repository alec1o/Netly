namespace NetlyTest.Netly;

public class NMessageTest
{
    private const string Name = "Example";
    private static readonly byte[] Message = "Hello World!"u8.ToArray();

    public NMessageTest(ITestOutputHelper output)
    {
        NLogger.Singleton.OnSubmit(output.WriteLine);
    }

    [Fact]
    public void Start()
    {
        var header = NMessage.Create(Name, Message.LongLength);
        Assert.NotEmpty(header);
        var payload = NHelper.ArrayConcat(header, Message);
        Assert.NotEmpty(payload);

        var stream = NHelper.NewStream(payload.LongLength);
        stream.Write(payload);
        Assert.Equal(payload.LongLength, stream.Position);
        Assert.True(NMessage.TryParse(stream, out var myName, out var myMessage, NHelper.NewStream));
        stream.Close();
        Assert.Equal(Name, myName);
        var data = new byte[myMessage.Length];
        Assert.Equal(Message.Length, myMessage.Read(data, 0, data.Length));
        Assert.Equal(Message, data);
    }

    [Fact]
    public void Create()
    {
        var header = NMessage.Create(Name, Message.Length);

        var size =
            sizeof(int) + // Prefix size
            sizeof(long) + // Package size (payload + header)
            sizeof(int) + // Message.Name size
            Name.Length; // Message.Name buffer

        Assert.Equal(size, header.Length);
    }
}