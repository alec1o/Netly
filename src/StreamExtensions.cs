using System;
using System.IO;

namespace Netly
{
    /// <summary>
    ///     Extension methods for <see cref="Stream" />.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        ///     Returns a copy of the whole stream as an array. The stream position is not changed.
        /// </summary>
        /// <param name="stream">A readable and seekable stream.</param>
        /// <returns>All bytes of the stream, or an empty array when the stream is empty.</returns>
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

        /// <summary>
        ///     Writes the bytes from the start of the stream and sets the position back to 0.
        /// </summary>
        /// <param name="stream">A writable and seekable stream.</param>
        /// <param name="bytes">The bytes to write.</param>
        public static void FillWithBytes(this Stream stream, byte[] bytes)
        {
            stream.Position = 0;
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
        }
    }
}