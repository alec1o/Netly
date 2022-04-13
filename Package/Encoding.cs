using Core = System.Text.Encoding;

namespace Zenet.Package
{
    public static class Encoding
    {
        public static string String(byte[] value, Encode encode = Encode.UTF8)
        {
            switch (encode)
            {
                case Encode.ASCII: return Core.ASCII.GetString(value);
                case Encode.UNICODE: return Core.Unicode.GetString(value);
                case Encode.UTF8: return Core.UTF8.GetString(value);
                default: return null;
            }
        }
        
        public static byte[] Bytes(string value, Encode encode = Encode.UTF8)
        {
            switch (encode)
            {
                case Encode.ASCII: return Core.ASCII.GetBytes(value);
                case Encode.UNICODE: return Core.Unicode.GetBytes(value);
                case Encode.UTF8: return Core.UTF8.GetBytes(value);
                default: return null;
            }
        }
    }
}
