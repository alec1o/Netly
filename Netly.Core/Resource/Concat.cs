using System.Collections.Generic;
using System.Linq;

namespace Netly.Core
{
    /// <summary>
    /// A module used to concatenate data
    /// </summary>
    public static class Concat
    {
        /// <summary>
        /// Concatenate string values
        /// </summary>
        /// <param name="values">Values</param>
        /// <returns>Returns a string with concatenated data</returns>
        public static string String(params string[] values)
        {
            string list = string.Empty;

            foreach (string value in values)
            {
                list += value;
            }

            return list;
        }

        /// <summary>
        /// Concatenate bytes values
        /// </summary>
        /// <param name="values">Values</param>
        /// <returns>Returns a bytes with concatenated data</returns>
        public static byte[] Bytes(params byte[][] values)
        {
            var list = new List<byte[]>();

            foreach (byte[] value in values)
            {
                list.Add(value);
            }

            return list.SelectMany(bytes => bytes).ToArray();
        }
    }
}
