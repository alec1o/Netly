using Byter;
using Netly;
using Xunit.Abstractions;

namespace test;

public class MessageFramingTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MessageFramingTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void EndToEnd()
    {
        byte[] value1 = ("HELLO WORLD").GetBytes();
        byte[] value2 = ("NETLY IS EASY").GetBytes();

        byte[] size1 = BitConverter.GetBytes(value1.Length);
        byte[] size2 = BitConverter.GetBytes(value2.Length);


        byte[] result1 = new byte[0];
        byte[] result2 = new byte[0];

        bool isLast = false;

        byte[] buffer = new List<byte[]> { NetlyEnvironment.MessageFraming.Prefix, size1, value1, NetlyEnvironment.MessageFraming.Prefix, size2 }
            .SelectMany(x => x).ToArray();

        NetlyEnvironment.MessageFraming package = new NetlyEnvironment.MessageFraming();

        package.OnData((data) =>
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

        package.OnError((error) => { _testOutputHelper.WriteLine($"OnError: {error}"); });

        package.Add(buffer);
        package.Add(value2);

        Assert.Equal(value1, result1);
        Assert.Equal(value2, result2);
    }
}