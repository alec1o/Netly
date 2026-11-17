using System;

namespace Netly
{
    // a message bigger than the biggest datagram that gets there is sent in fragments. Each one starts with a header
    // that is detected by its content, like the event (prefix and checksum), so a datagram that is not a fragment is
    // delivered as it is: Prefix (2) + Id (2) + Index (1) + Count (1) + Checksum (1), big-endian
    internal static class UdpFragment
    {
        public const int HeaderSize = 7;
        public const int MaxCount = byte.MaxValue;

        private static readonly byte[] Prefix = { 36, 19 };

        public static void Write(byte[] buffer, ushort id, int index, int count)
        {
            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);

            buffer[2] = (byte)(id >> 8);
            buffer[3] = (byte)id;
            buffer[4] = (byte)index;
            buffer[5] = (byte)count;
            buffer[HeaderSize - 1] = Framing.Checksum(buffer, HeaderSize - 1);
        }

        public static bool TryRead(byte[] buffer, int size, out ushort id, out int index, out int count)
        {
            id = 0;
            index = 0;
            count = 0;

            // a fragment has at least one byte after the header
            if (size <= HeaderSize || buffer[0] != Prefix[0] || buffer[1] != Prefix[1]) return false;

            if (buffer[HeaderSize - 1] != Framing.Checksum(buffer, HeaderSize - 1)) return false;

            id = (ushort)((buffer[2] << 8) | buffer[3]);
            index = buffer[4];
            count = buffer[5];

            return count >= 2 && index < count;
        }
    }
}
