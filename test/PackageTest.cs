using Netly.Abstract;
using Netly.Core;

namespace test;

public class PackageTest
{
    [Fact]
    public void EndToEnd()
    {
        byte[] value1 = NE.GetBytes("HELLO WORLD");
        byte[] value2 = NE.GetBytes("NETLY IS EASY");

        byte[] size1 = BitConverter.GetBytes(value1.Length);
        byte[] size2 = BitConverter.GetBytes(value2.Length);


        byte[] result1 = new byte[0];
        byte[] result2 = new byte[0];

        bool isLast = false;

        byte[] buffer = new List<byte[]> { size1, value1, size1 }.SelectMany(x => x).ToArray();

        Package package = new Package();

        package.Output((data) =>
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

        package.Input(buffer);
        package.Input(value2);

        Assert.Equal(value1, result1);
        Assert.Equal(value2, result2);
    }
}

