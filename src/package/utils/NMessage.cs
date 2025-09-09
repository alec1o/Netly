using System;
using System.IO;
using System.Text;

namespace Netly
{
    public static class NMessage
    {
        private const long MinSize = 1;

        public static readonly byte[] Prefix = { 6, 3, 9, 1 };

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

                if (!NUtils.ArraySequenced(Prefix, header))
                    return false;

                offset += Prefix.Length;

                var nameSize = BitConverter.ToInt32(header, offset);
                offset += sizeof(int);

                var messageSize = BitConverter.ToInt64(header, offset);
                offset += sizeof(long);

                if (nameSize < MinSize || messageSize < MinSize)
                    return false;

                if (nameSize + messageSize + offset != stream.Length)
                    return false;

                var nameBuffer = new byte[nameSize];
                if (stream.Read(nameBuffer, 0, nameBuffer.Length) != nameBuffer.Length)
                    return false;

                name = Encoding.UTF8.GetString(nameBuffer);

                message = getStream(messageSize);
                message.Position = 0;

                var buffer = new byte[Math.Min(1024 * 32, messageSize)];
                long position = 0;

                for (;;)
                {
                    var size = stream.Read(buffer, 0, buffer.Length);
                    if (size <= 0) break;

                    position += size;
                    message.Write(buffer, 0, size);
                }

                message.Position = 0;

                if (position != messageSize)
                    throw new InvalidDataException(nameof(position));

                return true;
            }
            catch (Exception e)
            {
                stream.Position = 0;
                name = null;
                message?.Close();
                NetlyEnvironment.Logger.Create("# N");
                NetlyEnvironment.Logger.Create(e);
                return false;
            }
        }
    }
}