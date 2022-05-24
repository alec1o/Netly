using System.Text;

namespace Zenet.Core
{
    public static class ZEncoding
    {
        public static string ToString(byte[] value, ZEncode encode = ZEncode.UTF8)
        {
            switch (encode)
            {
                case ZEncode.ASCII:
                    return Encoding.ASCII.GetString(value);

                case ZEncode.UNICODE:
                    return Encoding.Unicode.GetString(value);

                case ZEncode.UTF8:
                    return Encoding.UTF8.GetString(value);

                default:
                    return null;
            }
        }
        
        public static byte[] ToBytes(string value, ZEncode encode = ZEncode.UTF8)
        {
            switch (encode)
            {
                case ZEncode.ASCII:
                    return Encoding.ASCII.GetBytes(value);

                case ZEncode.UNICODE:
                    return Encoding.Unicode.GetBytes(value);

                case ZEncode.UTF8:
                    return Encoding.UTF8.GetBytes(value);

                default:
                    return null;
            }
        }
    }
}
