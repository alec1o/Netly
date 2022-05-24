namespace Zenet.Core
{
    public static class ZCompare
    {
        public static bool Int(int value1, int value2)
        {
            return value1 == value2;
        }
        
        public static bool Byte(byte value1, byte value2)
        {
            return value1 == value2;
        }
        
        public static bool String(params string[] values)
        {
            if (values.Length <= 1) return true;

            for (int i = 0; i < values.Length; i++)
            {
                try
                {
                    string value1 = values[i];

                    if (i + 1 > values.Length)
                    {
                        return true;
                    }

                    string value2 = values[i + 1];

                    if (value1 != value2)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Bytes(byte[] value1, byte[] value2)
        {
            if (value1 == value2) return true;
            if (value1 == null || value2 == null || value1.Length != value2.Length) return false;

            for (int i = 0; i < value1.Length; i++)
            {
                if (value1[i] != value2[i]) return false;
            }

            return true;
        }
    }
}
