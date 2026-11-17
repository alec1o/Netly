using System.Net.Sockets;

namespace NetlyTest.Netly;

// ZeroByteRead is a switch of the whole process, nothing else may run while a test turns it off
[Collection(NetlyTest.Support.StressCollection.Name)]
public class CapabilitiesTests
{
    [Fact]
    public void BufferFactor_IsDetectedOnceAndKept()
    {
        var first = Capabilities.BufferFactor;

        Assert.Contains(first, new[] { 1, 2 });

        // the answer is a fact of the system, every call and every thread gets the same
        var results = new int[32];
        Parallel.For(0, results.Length, i => results[i] = Capabilities.BufferFactor);

        Assert.All(results, value => Assert.Equal(first, value));
    }

    [Fact]
    public void BufferFactor_MatchesWhatTheSystemDoes()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SendBufferSize = 64 * 1024;

        Assert.Equal(socket.SendBufferSize >= 128 * 1024 ? 2 : 1, Capabilities.BufferFactor);
    }

    [Fact]
    public void ZeroByteRead_IsOnForANewRuntime()
    {
        // the tests run on .NET 8 or newer
        Assert.True(Capabilities.ZeroByteRead);
    }

    [Fact]
    public void ZeroByteRead_CanBeTurnedOff_ForTheWholeProcess()
    {
        // not run beside the others: the switch is global and sticky
        var field = typeof(Capabilities).GetField("_zeroByteDisabled",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        Assert.True(Capabilities.ZeroByteRead);

        try
        {
            Capabilities.DisableZeroByteRead();

            Assert.False(Capabilities.ZeroByteRead);
        }
        finally
        {
            field.SetValue(null, 0);
        }

        Assert.True(Capabilities.ZeroByteRead);
    }
}
