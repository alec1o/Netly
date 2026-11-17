using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Netly
{
    // what the system and the runtime can do, detected by testing it (not by the name of the system) and kept:
    // every module reads the same answers and a test is made once for the whole process
    internal static class Capabilities
    {
        private static readonly Lazy<int> Factor = new Lazy<int>(DetectBufferFactor, LazyThreadSafetyMode.PublicationOnly);
        private static readonly Lazy<bool> ZeroByte = new Lazy<bool>(DetectZeroByteRead, LazyThreadSafetyMode.PublicationOnly);
        private static readonly Lazy<int> Loopback = new Lazy<int>(DetectLoopbackDatagram, LazyThreadSafetyMode.PublicationOnly);
        private static int _zeroByteDisabled;

        // 1 when the system gives back the buffer that was set, 2 when it keeps the double (linux): the bytes
        // a datagram can use are the value of the socket divided by this factor
        public static int BufferFactor => Factor.Value;

        // a read with a zero byte buffer waits for data without holding a buffer (.NET 5 or newer);
        // it can be turned off when a platform completes it without data, for every connection
        public static bool ZeroByteRead => Volatile.Read(ref _zeroByteDisabled) == 0 && ZeroByte.Value;

        // biggest datagram that a new socket sends to loopback and gets there: some systems drop the bigger ones
        // until the other side answers, and nothing tells it (found by sending, not by the name of the system)
        public static int LoopbackDatagram => Loopback.Value;

        public static void DisableZeroByteRead()
        {
            Interlocked.Exchange(ref _zeroByteDisabled, 1);
        }

        // set a buffer and read it back
        private static int DetectBufferFactor()
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

        // send to a new socket of loopback, the biggest size that arrives: one try with the limit of the protocol
        // (the usual answer), otherwise a binary search
        private static int DetectLoopbackDatagram()
        {
            const int minimum = 512, maximum = UdpClient.MaxDatagramSize;

            try
            {
                if (Reaches(maximum)) return maximum;

                int low = minimum, high = maximum - 1;

                while (low < high)
                {
                    var middle = low + (high - low + 1) / 2;

                    if (Reaches(middle))
                        low = middle;
                    else
                        high = middle - 1;
                }

                return low;
            }
            catch (Exception)
            {
                return maximum;
            }
        }

        // every try uses new sockets: a flow that already got an answer is not the case to measure
        private static bool Reaches(int size)
        {
            try
            {
                using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    try
                    {
                        receiver.ReceiveBufferSize = 256 * 1024;
                        sender.SendBufferSize = 256 * 1024;
                    }
                    catch (Exception)
                    {
                        // the system keeps the default size
                    }

                    sender.SendTo(new byte[size], receiver.LocalEndPoint);

                    // loopback delivers at once, a datagram that is not there after this time was dropped
                    if (receiver.Poll(25 * 1000, SelectMode.SelectRead) is false) return false;

                    return receiver.Receive(new byte[size + 1]) == size;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ".NET 8.0.1" yes, ".NET Core 3.1", ".NET Framework 4.8" and "Mono" no
        private static bool DetectZeroByteRead()
        {
            var name = RuntimeInformation.FrameworkDescription ?? string.Empty;

            return name.StartsWith(".NET ", StringComparison.Ordinal) && name.Length > 5 && char.IsDigit(name[5]);
        }
    }
}
