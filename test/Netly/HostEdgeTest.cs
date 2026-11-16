using System.Net;
using System.Net.Sockets;

namespace NetlyTest.Netly;

public class HostEdgeTest
{
    [Fact]
    public void IPv6_IsSupported()
    {
        var host = new Host(IPAddress.IPv6Loopback, 8080);

        Assert.Equal(AddressFamily.InterNetworkV6, host.AddressFamily);
        Assert.Equal(8080, host.Port);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public void InvalidPort_Throws(int port)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Host("127.0.0.1", port));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(65535)]
    public void BoundaryPorts_AreAccepted(int port)
    {
        Assert.Equal(port, new Host("127.0.0.1", port).Port);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not an ip")]
    [InlineData("256.256.256.256")]
    public void InvalidIp_Throws(string ip)
    {
        Assert.Throws<FormatException>(() => new Host(ip, 80));
    }

    [Fact]
    public void Copy_IsIndependentOfSource()
    {
        var source = new IPEndPoint(IPAddress.Loopback, 1000);
        var host = new Host(source);

        source.Port = 2000;

        Assert.Equal(1000, host.Port);
    }

    [Fact]
    public void Equals_NullAndOtherTypes_AreFalse()
    {
        var host = new Host("127.0.0.1", 80);

        Assert.False(host.Equals(null));
        Assert.False(host.Equals("127.0.0.1:80"));
    }

    [Fact]
    public void GetHashCode_IsEqualForEqualHosts()
    {
        Assert.Equal(new Host("10.0.0.1", 99).GetHashCode(), new Host("10.0.0.1", 99).GetHashCode());
    }

    [Fact]
    public void Default_IsAnyAndPortZero()
    {
        Assert.Equal(IPAddress.Any, Host.Default.Address);
        Assert.Equal(0, Host.Default.Port);
    }
}
