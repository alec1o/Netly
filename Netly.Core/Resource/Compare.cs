namespace Netly.Core
{
    /// <summary>
    /// Compare data values
    /// </summary>
    public static class Compare
    {
        /// <summary>
        /// Compare string type value
        /// </summary>
        /// <param name="values">Values</param>
        /// <returns>Returns (True) when all data contains the same value</returns>
        public static bool String(params string[] values)
        {
            string last = values[0];

            foreach (string value in values)
            {
                if (last != value)
                {
                    return false;
                }

                last = value;
            }

            return true;
        }

        /// <summary>
        /// Compare byte[] type value
        /// </summary>
        /// <param name="values">Values</param>
        /// <returns>Returns (True) when all data contains the same value</returns>
        public static bool Bytes(params byte[][] values)
        {
            byte[] last = values[0];

            foreach (byte[] value in values)
            {
                if (!CompareBytes(last, value))
                {
                    return false;
                }

                last = value;
            }

            return true;

            bool CompareBytes(byte[] a, byte[] b)
            {
                if (a == b) return true;
                if (a == null || b == null || a.Length != b.Length) return false;

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }

                return true;
            }
        }
    }
}
