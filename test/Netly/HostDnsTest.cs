using System.Net;
using System.Net.Sockets;
using Netly;

namespace NetlyTest.Netly;

public class HostDnsTest
{
    [Fact]
    public void Hostname_IsNotResolvedUntilNeeded()
    {
        Host host = "localhost:8080";

        Assert.False(host.IsResolved);
        Assert.Equal("localhost", host.Name);
        Assert.Equal(8080, host.Port);
        Assert.Equal("localhost:8080", host.ToString());
    }

    [Fact]
    public void Hostname_ResolvesOnResolveOrAddressAccess()
    {
        var host = new Host("localhost", 80);

        host.Resolve();

        Assert.True(host.IsResolved);
        Assert.True(IPAddress.IsLoopback(host.Address));
        Assert.Equal(80, host.IPEndPoint.Port);
    }

    [Fact]
    public void Hostname_AddressAccessResolves()
    {
        var host = new Host("localhost", 80);

        Assert.True(IPAddress.IsLoopback(host.Address));
        Assert.True(host.IsResolved);
    }

    [Fact]
    public void UnknownHostname_ThrowsSocketExceptionOnResolve()
    {
        var host = new Host("host.invalid", 80);

        Assert.False(host.IsResolved);
        Assert.Throws<SocketException>(() => host.Resolve());
    }

    [Fact]
    public void IpString_IsResolvedImmediately()
    {
        var host = new Host("127.0.0.1", 80);

        Assert.True(host.IsResolved);
        Assert.Null(host.Name);
    }

    [Theory]
    [InlineData("bad host!")]
    [InlineData("")]
    [InlineData("a..b")]
    public void InvalidName_Throws(string name)
    {
        Assert.Throws<FormatException>(() => new Host(name, 80));
    }

    [Fact]
    public void Equality_ByNameWhenUnresolved()
    {
        Assert.Equal(new Host("Example.com", 1), new Host("example.com", 1));
        Assert.NotEqual(new Host("example.com", 1), new Host("example.com", 2));
        Assert.Equal(new Host("example.com", 1).GetHashCode(), new Host("EXAMPLE.com", 1).GetHashCode());
    }
}
