public class FixMessageFraming(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void EndToEnd()
    {
        var value1 = "HELLO WORLD".GetBytes();
        var value2 = "NETLY IS EASY".GetBytes();

        var size1 = BitConverter.GetBytes(value1.Length);
        var size2 = BitConverter.GetBytes(value2.Length);


        var result1 = Array.Empty<byte>();
        var result2 = Array.Empty<byte>();

        var isLast = false;

        var buffer = new List<byte[]> { MessageFraming.Prefix, size1, value1, MessageFraming.Prefix, size2 }
            .SelectMany(x => x).ToArray();

        var package = new MessageFraming();

        package.OnData(data =>
        {
            if (isLast is false)
            {
                result1 = new byte[data.Length];
                data.CopyTo(result1, 0);
            }
            else
            {
                result2 = new byte[data.Length];
                data.CopyTo(result2, 0);
            }

            isLast = !isLast;
        });

        package.OnError(error => { testOutputHelper.WriteLine($"OnError: {error}"); });

        package.Add(buffer);
        package.Add(value2);

        Assert.Equal(value1, result1);
        Assert.Equal(value2, result2);
    }
}