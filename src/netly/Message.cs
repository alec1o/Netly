using System;
using System.IO;
using System.Text;

namespace Netly
{
    internal static class Message
    {
        private const long MinSize = 1;

        /// <summary>
        ///     The magic prefix that starts an event: [19, 36] in decimal, [0x13, 0x24] in hexadecimal.
        ///     Read each byte as a decimal number and it is 19 36: the year 1936, when Alan Turing introduced the
        ///     Turing Machine, a theoretical model of computation whose main challenge was defining when the machine
        ///     should halt, leading to the Halting Problem and the limits of what is computable.
        ///     <br />
        ///     It is the first mark the receiver looks for to tell an event from data.
        ///     <br />
        ///     See: https://en.wikipedia.org/wiki/Turing_machine
        /// </summary>
        public static readonly byte[] Prefix = { 19, 36 };

        /// <summary>
        ///     Header of an event (big-endian): Prefix (2) + NameSize (2, ushort) + BodySize (8, ulong) + Checksum (1),
        ///     followed by the name (UTF-8) and the body.
        ///     The receiver trusts the header (it is an event, otherwise it is data) only when the prefix, the checksum
        ///     (CRC-8 of the fields before it, same algorithm of the <see cref="Framing" />) and the sizes agree with the
        ///     message received.
        /// </summary>
        public static readonly int HeaderSize = Prefix.Length + sizeof(ushort) + sizeof(ulong) + 1;

        /// <summary>
        ///     Creates the header of an event: header fields followed by the encoded name.
        /// </summary>
        /// <param name="name">The logical name of the message (must not be empty, up to 65535 bytes in UTF-8).</param>
        /// <param name="messageSize">The size of the payload message in bytes (must not be negative).</param>
        /// <returns>A byte array containing the header and the encoded name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="name" /> is empty or too big, or <paramref name="messageSize" /> is negative.
        /// </exception>
        public static byte[] Create(string name, long messageSize)
        {
            name = name ?? string.Empty;

            if (name.Length < MinSize)
                throw new ArgumentOutOfRangeException($"{nameof(name)}, {name.Length}");

            if (messageSize < 0)
                throw new ArgumentOutOfRangeException($"{nameof(messageSize)}, {messageSize}");

            var encodedName = Encoding.UTF8.GetBytes(name);

            if (encodedName.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException($"{nameof(name)}, {encodedName.Length}");

            var buffer = new byte[HeaderSize + encodedName.Length];

            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);

            var offset = Prefix.Length;

            buffer[offset] = (byte)(encodedName.Length >> 8);
            buffer[offset + 1] = (byte)encodedName.Length;
            offset += sizeof(ushort);

            for (var i = 0; i < sizeof(ulong); i++)
                buffer[offset + i] = (byte)((ulong)messageSize >> (8 * (sizeof(ulong) - 1 - i)));

            buffer[HeaderSize - 1] = Framing.Checksum(buffer, HeaderSize - 1);

            Buffer.BlockCopy(encodedName, 0, buffer, HeaderSize, encodedName.Length);

            return buffer;
        }

        /// <summary>
        ///     Attempts to parse a message from a given <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The input stream containing the message data.</param>
        /// <param name="name">The parsed message name, if successful.</param>
        /// <param name="message">The output stream containing the message payload, if successful.</param>
        /// <param name="getStream">
        ///     A factory function used to allocate a stream for storing the payload,
        ///     given the expected payload size.
        /// </param>
        /// <returns>
        ///     <c>true</c> if parsing was successful, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     - This method validates the prefix, checksum, name size, and message size.
        ///     - Copies payload bytes into the allocated stream using <paramref name="getStream" />.
        ///     - Resets stream positions appropriately before returning.
        /// </remarks>
        public static bool TryParse(Stream stream, out string name, out Stream message,
            Func<long, Stream> getStream)
        {
            name = null;
            message = null;

            var allocating = false;

            try
            {
                // min name buffer, the message can be empty
                const long bodyPreview = MinSize;

                if (stream.Length < HeaderSize + bodyPreview) return false;

                var header = new byte[HeaderSize];

                stream.Position = 0;
                var readHeader = stream.Read(header, 0, header.Length);

                if (readHeader != header.Length)
                    return false;

                // not an event, it is data
                if (!Helper.ArraySequenced(Prefix, header) ||
                    header[HeaderSize - 1] != Framing.Checksum(header, HeaderSize - 1))
                    return false;

                var offset = Prefix.Length;
                var nameSize = (header[offset] << 8) | header[offset + 1];
                offset += sizeof(ushort);

                ulong bodySize = 0;

                for (var i = 0; i < sizeof(ulong); i++)
                    bodySize = (bodySize << 8) | header[offset + i];

                if (nameSize < MinSize || bodySize > long.MaxValue)
                    return false;

                var messageSize = (long)bodySize;

                // long math, sizes come from the peer
                var expectedSize = HeaderSize + (long)nameSize + messageSize;
                if (expectedSize != stream.Length)
                    return false;

                var nameBuffer = new byte[nameSize];
                if (stream.Read(nameBuffer, 0, nameBuffer.Length) != nameBuffer.Length)
                    return false;

                try
                {
                    name = new UTF8Encoding(false, true).GetString(nameBuffer);
                }
                catch (ArgumentException)
                {
                    // not utf-8, it is data
                    stream.Position = 0;
                    return false;
                }

                allocating = true;

                // allocation errors go to the caller
                message = getStream(messageSize);

                try
                {
                    message.Position = 0;

                    stream.CopyTo(message);

                    if (message.Position != messageSize)
                        throw new InvalidDataException(nameof(message.Position));

                    message.Position = 0;
                }
                catch (Exception)
                {
                    message.Dispose();
                    message = null;
                    throw;
                }

                return true;
            }
            catch (Exception e) when (!allocating)
            {
                stream.Position = 0;
                name = null;
                message?.Close();
                message = null;
                Logger.Singleton.Submit(e);
                return false;
            }
        }
    }
}