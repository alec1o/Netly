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

        // biggest message the socket can send, up to the limit of the protocol
        public static int Send(Socket socket, int protocolLimit)
        {
            try
            {
                return Math.Max(Minimum, Math.Min(protocolLimit, socket.SendBufferSize / Capabilities.BufferFactor));
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
                return Math.Max(Minimum, Math.Min(protocolLimit, socket.ReceiveBufferSize / Capabilities.BufferFactor));
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
