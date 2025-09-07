using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Netly.Packages.Utils
{
    internal static class NTcpMessage
    {
        private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("Ny://");
        private const long sizeLength = sizeof(long), minNameLength = 1, minMessageLength = 1;

        internal static byte[] Create(string name, long messageSize)
        {
            name = name ?? string.Empty;

            if (name.Length < minNameLength)
                throw new ArgumentOutOfRangeException($"{nameof(name)}, {name.Length}");

            if (messageSize < minNameLength)
                throw new ArgumentOutOfRangeException($"{nameof(messageSize)}, {messageSize}");

            var body = Encoding.UTF8.GetBytes(name);
            var buffer = new byte[Prefix.Length + body.Length];

            Buffer.BlockCopy(Prefix, 0, buffer, 0, Prefix.Length);
            Buffer.BlockCopy(body, 0, buffer, Prefix.Length, body.Length);

            return buffer;
        }

        internal static bool Parse(Stream stream, Func<long, Stream> getStream, out string name, out Stream message,
            out bool close)
        {
            name = null;
            message = null;
            close = false;

            try
            {
                var head = new byte[Prefix.Length + sizeLength];

                if (stream.Read(head, 0, head.Length) != head.Length) return false;

                if (Prefix.Where((value, index) => value != head[index]).Any()) return false;

                var size = BitConverter.ToInt64(head, Prefix.Length);

                if (size < minMessageLength) return false;

                message = getStream(size);

                if (message == null)
                {
                    stream.Close();
                    close = true;
                    return false;
                }

                var buffer = new byte[1024 * 32];
                long totalSize = 0;

                for (;;)
                {
                    try
                    {
                        var count = stream.Read(buffer, 0, buffer.Length);

                        if (count <= 0 || totalSize >= size) break;

                        message.Write(buffer, 0, count);

                        totalSize += count;
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                }

                if (totalSize != size)
                {
                    stream.Close();
                    message.Close();
                    return false;
                }

                stream.Close();
                return true;
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                message?.Close();
                return false;
            }
        }
    }
}