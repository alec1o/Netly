namespace NetlyTest.Netly;

using System;
using System.IO;
using Xunit;

public class SendStreamTests
{
    private static MemoryStream PastTheEnd()
    {
        return new MemoryStream(new byte[] { 1, 2, 3 }) { Position = 10 };
    }

    [Fact]
    public void Tcp_PositionPastTheEnd_Throws()
    {
        var client = new TcpClient();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(PastTheEnd()));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendEvent("e", PastTheEnd()));
    }

    [Fact]
    public void Udp_PositionPastTheEnd_Throws()
    {
        var client = new UdpClient();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(PastTheEnd()));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendEvent("e", PastTheEnd()));
    }

    [Fact]
    public void Rudp_PositionPastTheEnd_Throws()
    {
        var client = new RudpClient();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(PastTheEnd()));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendEvent("e", PastTheEnd()));
    }

    [Fact]
    public void WebSocket_PositionPastTheEnd_Throws()
    {
        var client = new WebSocketClient();

        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendData(PastTheEnd()));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.SendEvent("e", PastTheEnd()));
    }
}
