using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netly
{
    public sealed class HttpMiddlewareDescriptor
    {
        public string Path { get; }
        internal HttpMiddlewareDescriptor Next { get; set; }

        internal Action<HttpServerRequest, HttpServerResponse, Action> Callback { get; }

        public bool UseParams { get; }

        internal HttpMiddlewareDescriptor
            (string path, bool useParams, Action<HttpServerRequest, HttpServerResponse, Action> callback)
        {
            Path = path;
            UseParams = useParams;
            Callback = callback;
            Next = null;
        }

        internal void Execute(HttpServerRequest request, HttpServerResponse response)
        {
            Next?.Callback(request, response, () => Next?.Execute(request, response));
        }
    }
}