using System;
using Netly.Interfaces;

namespace Netly
{
    internal class MapDescriptor
    {
        public bool UseParams { get; }
        public string Path { get; }
        public string Method { get; }
        public bool IsWebsocket { get; }
        public Action<IHTTP.ServerRequest, IHTTP.ServerResponse> HttpCallback { get; }
        public Action<IHTTP.ServerRequest, HTTP.WebSocket> WebsocketCallback { get; }

        public MapDescriptor
        (
            string path,
            bool useParams,
            string method,
            bool isWebsocket,
            Action<IHTTP.ServerRequest, IHTTP.ServerResponse> httpCallback,
            Action<IHTTP.ServerRequest, HTTP.WebSocket> websocketCallback
        )
        {
            Path = path;
            UseParams = useParams;
            Method = method;
            IsWebsocket = isWebsocket;
            HttpCallback = httpCallback;
            WebsocketCallback = websocketCallback;
        }
    }
}