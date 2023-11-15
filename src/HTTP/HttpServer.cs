using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.SymbolStore;
using System.Net;
using System.Threading;
using Netly.Core;

namespace Netly
{
    public class HttpServer : IHttpServer
    {
        private HttpListener _listener;
        private EventHandler<object> _onOpen, _onClose, _onError, _onPath, _onWebsocket;
        private bool _tryOpen, _tryClose;
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
            if (IsOpen || _tryClose || _tryOpen) return;
            _tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    HttpListener server = new HttpListener();

                    string protocol = "http://";
                    string url = $"{protocol}{host.Address}:{host.Port}/";
                    Console.WriteLine(url);
                    server.Prefixes.Add(url);

                    server.Start();

                    _listener = server;

                    _onOpen?.Invoke(null, null);

                    _ReceiveRequests();
                }
                catch (Exception e)
                {
                    _onError?.Invoke(null, e);
                }
                finally
                {
                    _tryOpen = false;
                }
            });
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
            if (_tryOpen || _tryClose || _listener == null) return;

            _tryClose = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (_listener != null)
                    {
                        _listener.Stop();
                        _listener.Abort();
                        _listener.Close();
                    }

                    // TODO: Close all http connection

                    // TODO: Close all websocket connection
                }
                catch
                {
                    // Ignored
                }
                finally
                {
                    _listener = null;
                    _onClose(null, null);
                }
            });
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
