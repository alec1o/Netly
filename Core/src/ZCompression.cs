using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Zenet.Core
{
    public static class ZCompression
    {
        public enum Mode { GZIP = 0 }

        public static byte[] StringCompress(string data, Mode mode = Mode.GZIP)
        {
            return Compress(ZEncoding.ToBytes(data, ZEncode.UNICODE), mode);
        }

        public static byte[] Compress(byte[] data, Mode mode = Mode.GZIP)
        {
            if (data == null || data.Length <= 0) return data;

            switch (mode)
            {
                case Mode.GZIP: return ToGZip(data);
                default: return null;
            }
        }

        public static string StringDecompress(byte[] data, Mode mode = Mode.GZIP)
        {
            return ZEncoding.ToString(Decompress(data, mode), ZEncode.UNICODE);
        }

        public static byte[] Decompress(byte[] data, Mode mode = Mode.GZIP)
        {
            if (data == null || data.Length <= 0) return data;

            switch (mode)
            {
                case Mode.GZIP: return FromGZip(data);
                default: return null;
            }
        }

        #region GZip

        private static byte[] ToGZip(byte[] data)
        {
            using (var s = new MemoryStream())
            {
                using (var g = new GZipStream(s, CompressionMode.Compress))
                {
                    g.Write(data, 0, data.Length);
                    g.Close();
                    return s.ToArray();
                }
            }
        }

        private static byte[] FromGZip(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }

        #endregion
    }
}
