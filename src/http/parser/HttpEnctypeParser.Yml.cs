using System;

namespace Netly
{
    internal partial class HttpEnctypeParser
    {
        public class Yml : IHttpEnctypeParser
        {
            private readonly byte[] _buffer;

            public Yml(ref byte[] buffer)
            {
                _buffer = buffer;
            }

            public bool IsValid { get; }
            public string[] Keys { get; }

            public IHttpEnctypeObject this[string key] => throw new NotImplementedException();
        }
    }
}