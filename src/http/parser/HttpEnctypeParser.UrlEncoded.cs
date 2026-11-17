using System;

namespace Netly
{
    internal partial class HttpEnctypeParser
    {
        public class UrlEncoded : IHttpEnctypeParser
        {
            private readonly byte[] _buffer;

            public UrlEncoded(ref byte[] buffer)
            {
                _buffer = buffer;
            }

            public bool IsValid { get; }
            public string[] Keys { get; }

            public IHttpEnctypeObject this[string key] => throw new NotImplementedException();
        }
    }
}