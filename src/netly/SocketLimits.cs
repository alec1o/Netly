using System;
using System.Net.Sockets;

namespace Netly
{
    // sizes come from the socket (send and receive buffers), never from a constant that only fits one system:
    // macOS refuses a datagram bigger than its send buffer (9216 bytes by default) and the system drops, without any
    // error, a datagram that does not fit the receive buffer of the peer
    internal static class SocketLimits
    {
        private const int Minimum = 512;

        // some systems (linux) keep the double of the buffer that is set, to pay the cost of each packet in the kernel, and
        // give that value back: the bytes a datagram can use are the value divided by this factor. It is detected, not
        // guessed by the name of the system: a buffer is set and read back once.
        private static readonly Lazy<int> Factor = new Lazy<int>(DetectFactor);

        internal static int BufferFactor => Factor.Value;

        private static int DetectFactor()
        {
            try
            {
                using (var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    const int size = 64 * 1024;

                    probe.SendBufferSize = size;

                    return probe.SendBufferSize >= size * 2 ? 2 : 1;
                }
            }
            catch (Exception)
            {
                return 1;
            }
        }

        // biggest message the socket can send, up to the limit of the protocol
        public static int Send(Socket socket, int protocolLimit)
        {
            try
            {
                return Math.Max(Minimum, Math.Min(protocolLimit, socket.SendBufferSize / BufferFactor));
            }
            catch (Exception)
            {
                return protocolLimit;
            }
        }

        // biggest message the socket can hold, up to the limit of the protocol
        public static int Receive(Socket socket, int protocolLimit)
        {
            try
            {
                return Math.Max(Minimum, Math.Min(protocolLimit, socket.ReceiveBufferSize / BufferFactor));
            }
            catch (Exception)
            {
                return protocolLimit;
            }
        }

        // biggest message that fits both sides of the socket
        public static int Both(Socket socket, int protocolLimit)
        {
            return Math.Min(Send(socket, protocolLimit), Receive(socket, protocolLimit));
        }
    }
}
