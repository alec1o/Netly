using System;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal partial class EnctypeParser
        {
            public class Json : IHTTP.EnctypeParser
            {
                private readonly byte[] _buffer;

                public Json(ref byte[] buffer)
                {
                    _buffer = buffer;
                }

                public bool IsValid { get; }
                public string[] Keys { get; }

                public IHTTP.EnctypeObject this[string key] => throw new NotImplementedException();
            }
        }
    }
}