using System.IO;

namespace Netly
{
    public static class NStream
    {
        public static byte[] GetBytes(this Stream stream)
        {
            var position = stream.Position;

            stream.Position = 0;
            var bytes = new byte[stream.Length];
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = position;
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