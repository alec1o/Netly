using System.IO;
using System.IO.Compression;

namespace Netly.Core
{
    /// <summary>
    /// It is a module used to compress data
    /// </summary>
    public static class Compress
    {
        /// <summary>
        /// Target compression method
        /// </summary>
        public enum Mode { GZIP }

        /// <summary>
        /// It is used to compress data
        /// </summary>
        /// <param name="value">The data to be compressed</param>
        /// <param name="mode">The method used to compress data</param>
        /// <returns>Returns the compressed data, or null (if not possible to compress)</returns>
        public static byte[] Encode(byte[] value, Mode mode = Mode.GZIP)
        {
            if (value == null)
            {
                return null;
            }

            switch (mode)
            {
                case Mode.GZIP:
                    {
                        try
                        {
                            using (var memory = new MemoryStream())
                            using (var gzip = new GZipStream(memory, CompressionMode.Compress))
                            {
                                gzip.Write(value, 0, value.Length);
                                gzip.Close();

                                return memory.ToArray();
                            }
                        }
                        catch
                        {
                            return null;
                        }
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        /// <summary>
        /// It is used to decompress data
        /// </summary>
        /// <param name="value">The data to be decompressed</param>
        /// <param name="mode">The method used to decompress data</param>
        /// <returns>Returns the unzipped data, or null (if not possible to decompress)</returns>
        public static byte[] Decode(byte[] value, Mode mode = Mode.GZIP)
        {
            if (value == null)
            {
                return null;
            }

            switch (mode)
            {
                case Mode.GZIP:
                    {
                        try
                        {
                            using (var memory = new MemoryStream(value))
                            using (var gzip = new GZipStream(memory, CompressionMode.Decompress))
                            using (var backup = new MemoryStream())
                            {
                                gzip.CopyTo(backup);
                                return backup.ToArray();
                            }
                        }
                        catch
                        {
                            return null;
                        }
                    }
                default:
                    {
                        return null;
                    }
            }
        }

    }
}
