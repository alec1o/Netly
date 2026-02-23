using System;
using System.IO;

namespace Netly
{
    public static class NStream
    {
        public static byte[] GetBytes(this Stream stream)
        {
            var position = stream.Position;

            stream.Position = 0;

            if (stream.Length < 1)
                return Array.Empty<byte>();

            var bytes = new byte[stream.Length];

            var count = stream.Read(bytes, 0, bytes.Length);

            stream.Position = position;

            if (count != bytes.Length)
                throw new IndexOutOfRangeException(
                    $"Error on stream read, Can't read the stream totally ({count} from {bytes.Length})");

            return bytes;
        }

        public static void FillWithBytes(this Stream stream, byte[] bytes)
        {
            stream.Position = 0;
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
        }
    }
}