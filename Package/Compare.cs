namespace Zenet.Package
{
    public static class Compare
    {
        public static bool Int(int value1, int value2)
        {
            return value1 == value2;
        }
        
        public static bool Byte(byte value1, byte value2)
        {
            return value1 == value2;
        }
        
        public static bool String(string value1, string value2)
        {
            return value1 == value2;
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
