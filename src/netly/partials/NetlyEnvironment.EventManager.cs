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
                var primitive = new Primitive(buffer);

                var key = primitive.Get.String();
                var name = primitive.Get.String();
                var data = primitive.Get.Bytes();

                primitive.Reset();

                if (primitive.IsValid && key is ProtocolKey) return (name, data);
                
                return (null, null);
            }

            public static byte[] Create(string name, byte[] data)
            {
                var primitive = new Primitive();

                primitive.Add.String(ProtocolKey);
                primitive.Add.String(name);
                primitive.Add.Bytes(data);

                var buffer = primitive.GetBytes();

                primitive.Reset();

                return buffer;
            }
        }
    }
}