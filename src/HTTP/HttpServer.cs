using System;
namespace Netly
{
    public class HttpServer : IHttpServer
    {
        private HttpListener _listener;
        private EventHandler<object> _onOpen, _onClose, _onError, _onPath, _onWebsocket;
        private readonly List<(string url, bool isWebSocket)> _paths;

        public bool IsOpen => _listener != null && _listener.IsListening;
        public Host Host { get; private set; } = new Host(IPAddress.Any, 0);

        public HttpServer()
        {
            _paths = new List<(string url, bool isWebSocket)>();
        }

        private struct PathContainer
        {
            public Request Request { get; private set; }
            public Response Response { get; private set; }
            public WebSocketClient WebSocket { get; private set; }

            public PathContainer(Request request, Response response, WebSocketClient webSocketClient)
            {
                this.Request = request;
                this.Response = response;
                this.WebSocket = webSocketClient;
            }
        }

        public void Open(Host host)
        {

        }

        public void OnOpen(Action callback)
        {
            if (callback == null) return;

            _onOpen += (_, __) => MainThread.Add(() => callback?.Invoke());
        }

        public void OnError(Action<Exception> callback)
        {
            if (callback == null) return;

            _onError += (_, o) => MainThread.Add(() => callback?.Invoke((Exception)o));
        }

        public void Close()
        {

        }

        public void OnClose(Action callback)
        {
            if (callback == null) return;

            _onClose += (_, __) => MainThread.Add(() => callback?.Invoke());
        }

        public void On(string path, Action<Request, Response> callback)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            _paths.Add((path.Trim(), false));

            _onPath += (_, o) =>
            {
                var pathContainer = (PathContainer)o;

                if (pathContainer.Request.ComparePath(path))
                {
                    MainThread.Add(() => callback?.Invoke(pathContainer.Request, pathContainer.Response));
                }
            };
        }

        public void OnWebsocket(string path, Action<Request, WebSocketClient> callback)
        {

        }
