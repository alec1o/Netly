
namespace Netly
{
    internal struct HttpEnctypeObject : IHttpEnctypeObject
    {
        public bool IsNull { get; }
        public string String { get; }
        public byte[] Bytes { get; }

        public HttpEnctypeObject(bool @null, string @string, byte[] @byte)
        {
            IsNull = @null;
            String = @string;
            Bytes = @byte;
        }
    }
}