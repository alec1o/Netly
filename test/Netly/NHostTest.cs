namespace NetlyTest.Netly;

using System;
using System.Net;
using Xunit;

public class NHostTest
{
    [Fact]
    public void Constructor_WithIPString_CreatesCorrectHost()
    {
        var host = new NHost("127.0.0.1", 3300);

        Assert.Equal(IPAddress.Parse("127.0.0.1"), host.Address);
        Assert.Equal(3300, host.Port);
        Assert.IsType<IPEndPoint>(host.IPEndPoint);
    }

    [Fact]
    public void Constructor_WithIPAddress_CreatesCorrectHost()
    {
        var ip = IPAddress.Loopback;
        var host = new NHost(ip, 8080);

        Assert.Equal(ip, host.Address);
        Assert.Equal(8080, host.Port);
    }

    [Fact]
    public void Constructor_WithIPEndPoint_CreatesCorrectHost()
    {
        var ep = new IPEndPoint(IPAddress.Any, 1234);
        var host = new NHost(ep);

        Assert.Equal(IPAddress.Any, host.Address);
        Assert.Equal(1234, host.Port);
    }

    [Fact]
    public void Constructor_WithEndPoint_CreatesCorrectHost()
    {
        var ep = new IPEndPoint(IPAddress.Loopback, 4321);
        var host = new NHost(ep as EndPoint);

        Assert.Equal(IPAddress.Loopback, host.Address);
        Assert.Equal(4321, host.Port);
    }

    [Fact]
    public void ImplicitConversion_FromString_ValidHost()
    {
        NHost host = "127.0.0.1:3300";

        Assert.Equal(IPAddress.Parse("127.0.0.1"), host.Address);
        Assert.Equal(3300, host.Port);
    }

    [Fact]
    public void ImplicitConversion_InvalidIP_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
        {
            NHost host = "invalidhost:1234";
        });
    }

    [Fact]
    public void Equals_SameHost_ReturnsTrue()
    {
        var a = new NHost("127.0.0.1", 3300);
        var b = new NHost("127.0.0.1", 3300);

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentHost_ReturnsFalse()
    {
        var a = new NHost("127.0.0.1", 3300);
        var b = new NHost("127.0.0.1", 3301);

        Assert.False(a.Equals(b));
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsIPPort()
    {
        var host = new NHost("192.168.0.1", 8080);
        var str = host.ToString();

        Assert.Equal("192.168.0.1:8080", str);
    }
}
