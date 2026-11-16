using System.Net;
using System.Net.Sockets;

namespace NetlyTest.Support;

/// <summary>
///     Udp relay between one client and one server, it can lose, duplicate and delay packets.
/// </summary>
internal sealed class UdpProxy : IDisposable
{
    private readonly Socket _front = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private readonly Socket _back = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private readonly Random _random;
    private readonly object _locker = new();
    private EndPoint? _client;
    private volatile bool _closed;
    private long _toServerBytes, _toClientBytes, _toServerPackets, _toClientPackets;

    public UdpProxy(NHost server, double loss = 0, double duplicate = 0, int maxDelayMs = 0, int seed = 1)
    {
        Loss = loss;
        Duplicate = duplicate;
        MaxDelayMs = maxDelayMs;
        _random = new Random(seed);

        // the relay must not be the bottleneck: a drop here looks like a network loss
        _front.ReceiveBufferSize = _back.ReceiveBufferSize = 8 * 1024 * 1024;
        _front.SendBufferSize = _back.SendBufferSize = 8 * 1024 * 1024;

        _front.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _back.Connect(server.EndPoint);
        Host = new NHost(_front.LocalEndPoint!);

        new Thread(() => Relay(_front, true)) { IsBackground = true }.Start();
        new Thread(() => Relay(_back, false)) { IsBackground = true }.Start();
    }

    public NHost Host { get; }
    public double Loss { get; set; }
    public double Duplicate { get; set; }
    public int MaxDelayMs { get; set; }
    public bool Blackhole { get; set; }

    public long ToServerBytes => Interlocked.Read(ref _toServerBytes);
    public long ToClientBytes => Interlocked.Read(ref _toClientBytes);
    public long ToServerPackets => Interlocked.Read(ref _toServerPackets);
    public long ToClientPackets => Interlocked.Read(ref _toClientPackets);

    private double Next()
    {
        lock (_locker) return _random.NextDouble();
    }

    private void Relay(Socket source, bool fromClient)
    {
        var buffer = new byte[65536];

        while (!_closed)
            try
            {
                int size;

                if (fromClient)
                {
                    EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                    size = source.ReceiveFrom(buffer, ref endpoint);
                    _client = endpoint;
                }
                else
                {
                    size = source.Receive(buffer);
                }

                if (size <= 0) continue;

                var data = buffer.AsSpan(0, size).ToArray();

                if (fromClient)
                {
                    Interlocked.Add(ref _toServerBytes, size);
                    Interlocked.Increment(ref _toServerPackets);
                }
                else
                {
                    Interlocked.Add(ref _toClientBytes, size);
                    Interlocked.Increment(ref _toClientPackets);
                }

                if (Blackhole || Next() < Loss) continue;

                Deliver(data, fromClient);

                if (Next() < Duplicate) Deliver(data, fromClient);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                if (_closed) return;
            }
    }

    private void Deliver(byte[] data, bool toServer)
    {
        var delay = MaxDelayMs > 0 ? (int)(Next() * MaxDelayMs) : 0;

        if (delay <= 0)
        {
            Send(data, toServer);
            return;
        }

        Task.Delay(delay).ContinueWith(_ => Send(data, toServer));
    }

    private void Send(byte[] data, bool toServer)
    {
        try
        {
            if (toServer) _back.Send(data);
            else if (_client != null) _front.SendTo(data, _client);
        }
        catch (Exception)
        {
            // closed
        }
    }

    public void Dispose()
    {
        _closed = true;
        _front.Close();
        _back.Close();
    }
}
