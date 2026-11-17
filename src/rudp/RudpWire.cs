using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Netly
{
    // packet format (little endian)
    //
    // flags (1 byte): bits 0-2 type, bit 3 reserved, bit 4 first fragment, bit 5 last fragment, bit 6 datagram fragment
    //
    // Reliable:   flags | token(4) | seq(4) | ack(12) | payload
    // Ack:        flags | token(4) | ack(12)
    // KeepAlive:  flags | token(4) | ack(12)
    // Close:      flags | token(4)
    // Unreliable: flags | token(4) | [fragment(4)] | payload
    // Sequenced:  flags | token(4) | seq(4) | [fragment(4)] | payload
    // Handshake:  flags | kind(1) | ...
    //     syn:     version(1) | nonce(4)
    //     syn ack: nonce(4) | token(4) | id(16)
    //     ack:     token(4)
    //
    // ack: next expected seq (4), every seq below it was received, and a bitmap (8): bit i is seq next + 1 + i
    // fragment: message id(2) | index(1) | count(1)
    internal static class RudpWire
    {
        public const byte Version = 1;

        public const byte
            Unreliable = 0,
            Sequenced = 1,
            Reliable = 2,
            Ack = 3,
            KeepAlive = 4,
            Close = 5,
            Handshake = 6;

        public const byte
            TypeMask = 0x07,
            First = 0x10,
            Last = 0x20,
            Fragment = 0x40;

        public const byte
            Syn = 0,
            SynAck = 1,
            HandshakeAck = 2;

        public const int
            TokenSize = 4,
            AckSize = 12,
            FragmentSize = 4,
            ReliableHeader = 1 + TokenSize + 4 + AckSize,
            ControlHeader = 1 + TokenSize + AckSize,
            SynSize = 1 + 1 + 1 + 4,
            SynAckSize = 1 + 1 + 4 + TokenSize + 16,
            HandshakeAckSize = 1 + 1 + TokenSize;

        // datagram limits: ip fragmentation is avoided on the internet (ipv6 minimum mtu), loopback has a big mtu
        public const int InternetDatagram = 1232;
        public const int LoopbackDatagram = 8192;

        private static readonly object RandomLocker = new object();
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
        private static readonly double TicksToMilliseconds = 1000d / Stopwatch.Frequency;

        // monotonic clock in milliseconds
        public static long Now()
        {
            return (long)(Stopwatch.GetTimestamp() * TicksToMilliseconds);
        }

        public static uint RandomUInt()
        {
            var bytes = new byte[4];

            lock (RandomLocker)
            {
                Random.GetBytes(bytes);
            }

            return BitConverter.ToUInt32(bytes, 0);
        }

        public static void Put16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
        }

        public static void Put32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        public static void Put64(byte[] buffer, int offset, ulong value)
        {
            Put32(buffer, offset, (uint)value);
            Put32(buffer, offset + 4, (uint)(value >> 32));
        }

        public static ushort Get16(byte[] buffer, int offset)
        {
            return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
        }

        public static uint Get32(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) |
                          (buffer[offset + 3] << 24));
        }

        public static ulong Get64(byte[] buffer, int offset)
        {
            return Get32(buffer, offset) | ((ulong)Get32(buffer, offset + 4) << 32);
        }

        // sequence numbers wrap around, a is newer than b when the signed distance is positive
        public static bool IsNewer(uint a, uint b)
        {
            return (int)(a - b) > 0;
        }

        public static bool IsSyn(byte[] buffer, int length)
        {
            return length >= SynSize
                   && (buffer[0] & TypeMask) == Handshake
                   && buffer[1] == Syn
                   && buffer[2] == Version;
        }
    }
}
