using System;
using System.Collections.Generic;
using System.Text;

namespace Zenet.Core
{
    public static class ZCompression
    {
        public enum Mode { ZLIB = 0, DEFLATE = 1, GZIP = 2, GZIP2 = 3 }

        public static byte[] Compress(byte[] data, Mode mode)
        {
            if (data == null || data.Length <= 0) return data;

            switch (mode)
            {
                case Mode.ZLIB: return ToZLIB(data);
                case Mode.DEFLATE: return null;
                case Mode.GZIP: return null;
                case Mode.GZIP2: return null;
                default: return null;
            }
        }

        public static byte[] Decompress(byte[] data, Mode mode)
        {
            if (data == null || data.Length <= 0) return data;

            switch (mode)
            {
                case Mode.ZLIB: return FromZLIB(data);
                case Mode.DEFLATE: return null;
                case Mode.GZIP: return null;
                case Mode.GZIP2: return null;
                default: return null;
            }
        }

        #region ZLIB

        private static byte[] ToZLIB(byte[] data)
        {
            return null;
        }

        private static byte[] FromZLIB(byte[] data)
        {
            return null;
        }

        #endregion
    }
}
