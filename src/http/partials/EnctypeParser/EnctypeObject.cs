using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal struct EnctypeObject : IHTTP.EnctypeObject
        {
            public bool IsNull { get; }
            public string String { get; }
            public byte[] Bytes { get; }

            public EnctypeObject(bool @null, string @string, byte[] @byte)
            {
                IsNull = @null;
                String = @string;
                Bytes = @byte;
            }
        }
    }
}