using System;
namespace Netly
{
    public class HttpServer : IHttpServer
    {
        private HttpListener _listener;
        private EventHandler<object> _onOpen, _onClose, _onError, _onPath, _onWebsocket;
        public bool IsOpen => _listener != null && _listener.IsListening;
        public Host Host { get; private set; } = new Host(IPAddress.Any, 0);
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
        }

        public void Close()
        {

        }

        public void OnClose(Action callback)
        {

        }

        public void On(string path, Action<Request, Response> callback)
        {

        }

        public void OnWebsocket(string path, Action<Request, WebSocketClient> callback)
        {

        }
