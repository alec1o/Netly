using System;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal struct MiddlewareDescriptor : IHTTP.MiddlewareDescriptor
        {
            public string Path { get; }
            public bool UseParams { get; }
            public Func<IHTTP.Request, IHTTP.ServerResponse, bool> Callback { get; }

            public MiddlewareDescriptor(string path, bool useParams,
                Func<IHTTP.Request, IHTTP.ServerResponse, bool> callback)
            {
                Path = path;
                UseParams = useParams;
                Callback = callback;
            }
        }
    }
}