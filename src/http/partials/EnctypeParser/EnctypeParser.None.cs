using System;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal partial class EnctypeParser
        {
            public class None : IHTTP.EnctypeParser
            {
                public bool IsValid { get; } = false;
                public string[] Keys { get; } = Array.Empty<string>();
                public IHTTP.EnctypeObject this[string key] => new EnctypeObject(true, null, null);
            }
        }
    }
}