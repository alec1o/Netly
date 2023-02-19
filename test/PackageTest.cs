using Netly.Core;

namespace test;

public class PackageTest
{
    [Fact]
    public void EndToEnd()
    {
        byte[] value1 = NE.GetBytes("HELLO");
        byte[] value2 = NE.GetBytes("WORLD");

        byte[] size1 = BitConverter.GetBytes(value1.Length);
        byte[] size2 = BitConverter.GetBytes(value2.Length);

        byte[] data1 = new byte[0];
        byte[] data2 = new byte[0];

        byte[] buffer = new List<byte[]> { size1, value1, size1 }.SelectMany(x => x).ToArray();

        Package package = new Package();

        package.Output((data) =>
        {
            if (data1 == null)
            {
                data1 = data;
            }
            else
            {
                data2 = data;
            }
        });

        package.Input(buffer);
        package.Input(value2);

        Assert.Equal(value1, data1);
        Assert.Equal(value2, data2);
    }
}

