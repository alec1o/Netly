namespace NetlyTest.Netly;

public class StreamExtensionsTest
{
    [Fact]
    public void GetBytes_ReturnsAllBytes_AndKeepsPosition()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        stream.Position = 3;

        var bytes = stream.GetBytes();

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, bytes);
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public void GetBytes_EmptyStream_ReturnsEmpty()
    {
        Assert.Empty(new MemoryStream().GetBytes());
    }

    [Fact]
    public void FillWithBytes_WritesFromStartAndResetsPosition()
    {
        var stream = new MemoryStream();

        stream.FillWithBytes(new byte[] { 9, 8, 7 });

        Assert.Equal(0, stream.Position);
        Assert.Equal(new byte[] { 9, 8, 7 }, stream.ToArray());
    }
}
