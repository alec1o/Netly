using System.Net.Sockets;

namespace Zenet.Network
{
    public class EasySocket
    {
        public static Socket CopySocket(Socket mirror)
        {
            if (mirror == null) return null;

            var socket = new Socket(mirror.AddressFamily, mirror.SocketType, mirror.ProtocolType);

            try { socket.Blocking = mirror.Blocking; } catch { }

            try { socket.DontFragment = mirror.DontFragment; } catch { }

            try { socket.DualMode = mirror.DualMode; } catch { }

            try { socket.EnableBroadcast = mirror.EnableBroadcast; } catch { }

            try { socket.ExclusiveAddressUse = mirror.ExclusiveAddressUse; } catch { }

            try { socket.LingerState = mirror.LingerState; } catch { }

            try { socket.NoDelay = mirror.NoDelay; } catch { }

            try { socket.ReceiveBufferSize = mirror.ReceiveBufferSize; } catch { }

            try { socket.ReceiveTimeout = mirror.ReceiveTimeout; } catch { }

            try { socket.SendBufferSize = mirror.SendBufferSize; } catch { }

            try { socket.MulticastLoopback = mirror.MulticastLoopback; } catch { }

            try { socket.SendTimeout = mirror.SendTimeout; } catch { }

            try { socket.Ttl = mirror.Ttl; } catch { }

            try { socket.UseOnlyOverlappedIO = mirror.UseOnlyOverlappedIO; } catch { }

            return socket;
        }
    }
}