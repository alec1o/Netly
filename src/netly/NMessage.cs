using System;
using System.IO;
using System.Text;

namespace Netly
{
    public static class NMessage
    {
        private const long MinSize = 1;

        /// <summary>
        ///     The prefix used in <see cref="NFraming" />.<see cref="NFraming.Prefix" /> is: [0x06, 0x03, 0x09, 0x01] in
        ///     hexadecimal,
        ///     which is [6, 3, 9, 1] in decimal. It serves as a “magic number”
        ///     to identify the start of a message payload.
        ///     <br />
        ///     This value is the reverse of the prefix used in <see cref="NFraming" /> ([1, 9, 3, 6]),
        ///     which refers to the year 1936 when Alan Turing introduced the Turing Machine.
        ///     By reversing the digits to 6391, this prefix symbolically represents
        ///     the message layer built on top of the framing layer, keeping a historical
        ///     and conceptual link to the foundation of computability.
        ///     <br />
        ///     See: https://en.wikipedia.org/wiki/Turing_machine
        /// </summary>
        public static readonly byte[] Prefix = { 6, 3, 9, 1 };

        /// <summary>
        ///     Creates a serialized message header that contains a prefix,
        ///     the encoded name, and the message size.
        /// </summary>
        /// <param name="name">The logical name of the message (must not be empty).</param>
        /// <param name="messageSize">The size of the payload message in bytes (must be greater than 0).</param>
        /// <returns>
        ///     A byte array containing the prefix, name length, message size, and the encoded name.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="name" /> is empty or <paramref name="messageSize" /> is less than 1.
        /// </exception>
        public static byte[] Create(string name, long messageSize)
        {
            name = name ?? string.Empty;

            if (name.Length < MinSize)
                throw new ArgumentOutOfRangeException($"{nameof(name)}, {name.Length}");

            if (messageSize < MinSize)
                throw new ArgumentOutOfRangeException($"{nameof(messageSize)}, {messageSize}");

            var encodedName = Encoding.UTF8.GetBytes(name);
            var encodedNameSize = BitConverter.GetBytes(encodedName.Length);
            var encodedMessageSize = BitConverter.GetBytes(messageSize);

            var buffer = new byte
            [
                Prefix.Length + // Prefix Size (4)
                encodedNameSize.Length + // Encoded Name Size (4)
                encodedMessageSize.Length + // Encoded Message Size (8)
                encodedName.Length // Encoded Name (N >= 1)
            ];

            var offset = 0;

            Buffer.BlockCopy(Prefix, 0, buffer, offset, Prefix.Length);
            offset += Prefix.Length;

            Buffer.BlockCopy(encodedNameSize, 0, buffer, offset, encodedNameSize.Length);
            offset += encodedNameSize.Length;

            Buffer.BlockCopy(encodedMessageSize, 0, buffer, offset, encodedMessageSize.Length);
            offset += encodedMessageSize.Length;

            Buffer.BlockCopy(encodedName, 0, buffer, offset, encodedName.Length);
            offset += encodedName.Length;

            if (offset != buffer.Length) throw new FormatException(nameof(offset));

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
        ///     - This method validates the prefix, name size, and message size.
        ///     - Copies payload bytes into the allocated stream using <paramref name="getStream" />.
        ///     - Resets stream positions appropriately before returning.
        /// </remarks>
        public static bool TryParse(Stream stream, out string name, out Stream message,
            Func<long, Stream> getStream)
        {
            var headerSize =
                Prefix.Length + // Prefix size
                sizeof(int) + // Name size
                sizeof(long); // Message size


            name = null;
            message = null;

            try
            {
                const long bodyPreview =
                    MinSize + // Min Name Buffer
                    MinSize; // Min Message Buffer

                if (stream.Length < headerSize + bodyPreview) return false;

                var header = new byte[headerSize];

                stream.Position = 0;
                var readHeader = stream.Read(header, 0, header.Length);

                if (readHeader != header.Length)
                    return false;

                var offset = 0;

                if (!NHelper.ArraySequenced(Prefix, header))
                    return false;

                offset += Prefix.Length;

                var nameSize = BitConverter.ToInt32(header, offset);
                offset += sizeof(int);

                var messageSize = BitConverter.ToInt64(header, offset);
                offset += sizeof(long);

                if (nameSize < MinSize || messageSize < MinSize)
                    return false;

                var expectedSize = offset + nameSize + messageSize;
                if (expectedSize != stream.Length)
                    return false;

                var nameBuffer = new byte[nameSize];
                if (stream.Read(nameBuffer, 0, nameBuffer.Length) != nameBuffer.Length)
                    return false;

                name = Encoding.UTF8.GetString(nameBuffer);

                message = getStream(messageSize);
                message.Position = 0;

                stream.CopyTo(message);

                if (message.Position != messageSize)
                    throw new InvalidDataException(nameof(message.Position));

                message.Position = 0;

                return true;
            }
            catch (Exception e)
            {
                stream.Position = 0;
                name = null;
                message?.Close();
                message = null;
                NLogger.Singleton.Submit(e);
                return false;
            }
        }
    }
}