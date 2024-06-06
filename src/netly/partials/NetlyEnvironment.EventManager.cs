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
                using (var r = new Reader(buffer))
                {
                    var key = r.Read<string>(Encoding.ASCII);
                    var name = r.Read<string>(Encoding.UTF8);
                    var data = r.Read<byte[]>();

                    if (r.Success && key is ProtocolKey) return (name, data);

                    return (null, null);
                }
            }

            public static byte[] Create(string name, byte[] data)
            {
                using (var w = new Writer())
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