using System;

namespace Netly
{
    internal partial class HttpEnctypeParser
    {
        public class None : IHttpEnctypeParser
        {
            public bool IsValid { get; } = false;
            public string[] Keys { get; } = Array.Empty<string>();
            public IHttpEnctypeObject this[string key] => new HttpEnctypeObject(true, null, null);
        }
    }
}