using System.Text;
using Byter;

namespace Netly
{
    public partial class NetlyEnvironment
    {
        public static class EventManager
        {
            private const string ProtocolKey = "Ny://";

            public static (string name, byte[] data) Verify(byte[] buffer)
            {
                using (Reader r = new Reader(buffer))
                {
                    string key = r.Read<string>(Encoding.ASCII);
                    string name = r.Read<string>(Encoding.UTF8);
                    byte[] data = r.Read<byte[]>();

                    if (r.Success && key is ProtocolKey) return (name, data);

                    return (null, null);
                }
            }

            public static byte[] Create(string name, byte[] data)
            {
                using (Writer w = new Writer())
                {
                    w.Write(ProtocolKey, Encoding.ASCII);
                    w.Write(name, Encoding.UTF8);
                    w.Write(data);

                    return w.GetBytes();
                }
            }
        }
    }
}