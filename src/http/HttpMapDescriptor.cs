using System;

namespace Netly
{
    internal class HttpMapDescriptor
    {
        public bool UseParams { get; }
        public string Path { get; }
        public string Method { get; }
        public bool IsWebsocket { get; }
        public Action<HttpServerRequest, HttpServerResponse> HttpCallback { get; }
        public Action<HttpServerRequest, WebSocketClient> WebsocketCallback { get; }

        public HttpMapDescriptor
        (
            string path,
            bool useParams,
            string method,
            bool isWebsocket,
            Action<HttpServerRequest, HttpServerResponse> httpCallback,
            Action<HttpServerRequest, WebSocketClient> websocketCallback
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