using Core = System.Text.Encoding;

namespace Zenet.Package
{
    public static class Encoding
    {
        public static string String(byte[] target, Encode encode = Encode.UTF8)
        {
            switch (encode)
            {
                case Encode.ASCII: return Core.ASCII.GetString(target);
                case Encode.UNICODE: return Core.Unicode.GetString(target);
                case Encode.UTF8: return Core.UTF8.GetString(target);
                default: return null;
            }
        }
        
        public static byte[] Bytes(string target, Encode encode = Encode.UTF8)
        {
            switch (encode)
            {
                case Encode.ASCII: return Core.ASCII.GetBytes(target);
                case Encode.UNICODE: return Core.Unicode.GetBytes(target);
                case Encode.UTF8: return Core.UTF8.GetBytes(target);
                default: return null;
            }
        }
    }
}
