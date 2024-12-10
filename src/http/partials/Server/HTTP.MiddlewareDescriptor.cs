using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class MiddlewareDescriptor : IHTTP.MiddlewareDescriptor
        {
            public string Path { get; }
            public MiddlewareDescriptor Next { get; set; }

            public Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> Callback { get; }

            public bool UseParams { get; }

            public MiddlewareDescriptor
                (string path, bool useParams, Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> callback)
            {
                Path = path;
                UseParams = useParams;
                Callback = callback;
                Next = null;
            }

            public void Execute(IHTTP.ServerRequest request, IHTTP.ServerResponse response)
            {
                if (Next == null || Next.Callback == null) return;
                
                Next.Callback(request, response, () => Next.Execute(request, response));
            }
        }
    }
}