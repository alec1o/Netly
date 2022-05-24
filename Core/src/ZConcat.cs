using System.Collections.Generic;
using System.Linq;

namespace Zenet.Core
{
    public static class ZConcat
    {
        public static string String(params string[] values)
        {
            var list = string.Empty;

            foreach (var value in values)
            {
                if (value != null) list += value;
            }

            return list;
        }

        public static byte[] Bytes(params byte[][] values)
        {
            var list = new List<byte[]>();

            foreach (var value in values)
            {
                if(value != null) list.Add(value);
            }

            return list.SelectMany(bytes => bytes).ToArray();
        }
    }
}
