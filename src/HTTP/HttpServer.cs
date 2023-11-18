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
            if (string.IsNullOrWhiteSpace(path)) return;

            _paths.Add((path.Trim(), true));

            _onWebsocket += (_, o) =>
            {
                var pathContainer = (PathContainer)o;

                if (pathContainer.Request.ComparePath(path))
                {
                    MainThread.Add(() => callback?.Invoke(pathContainer.Request, pathContainer.WebSocket));
                }
            };
        }

        private void _ReceiveRequests()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (IsOpen)
                {
                    try
                    {
                        var context = _listener.GetContext();
                        var request = new Request(context.Request);
                        var response = new Response(context.Response);

                        // TODO: Only show on debug mode
                        {
                            string headerDebug = "\n\tHeaders:";
                            foreach (var header in request.Headers.AllKeyValue)
                            {
                                headerDebug += $"\n\t{header.Key}:{header.Value}";
                            }

                            string cookiesDebug = "\n\tCookies:";
                            foreach (var cookie in request.Cookies)
                            {
                                cookiesDebug += $"\n\t{cookie.Name}:{cookie.Value} [{cookie.Port}:{cookie.Path}]";
                            }

                            string queriesDebug = "\n\t:Queries:";
                            foreach (var query in request.Queries.AllKeyValue)
                            {
                                queriesDebug += $"\n\t{query.Key}:{query.Value}";
                            }

                            Console.WriteLine
                            (
                                "Receive connection" +
                                $"\n\tUrl: {request.RawRequest.Url.AbsoluteUri}" +
                                $"\n\tPath: {request.RawRequest.Url.AbsolutePath}" +
                                $"\n\tLocal Path: {request.RawRequest.Url.LocalPath}" +
                                $"\n\tIs Websocket: {context.Request.IsWebSocketRequest}" +
                                $"\n\n{headerDebug}" +
                                $"\n\n{queriesDebug}" +
                                $"\n\n{cookiesDebug}"
                            );
                        }

                        bool foundPath = false;

                        if (request.IsWebSocket is false) // IS HTTP CONNECTION
                        {
                            foreach (var path in _paths)
                            {
                                if (request.ComparePath(path.url))
                                {
                                    if (path.isWebSocket)
                                    {
                                        response.Send(426, "Only websocket connection for use this router");
                                    }
                                    else
                                    {
                                        _onPath?.Invoke(null, new PathContainer(request, response, null));
                                    }

                                    foundPath = true;
                                }
                            }
                        }
                        else
                        {
                            foreach (var path in _paths)
                            {
                                if (request.ComparePath(path.url))
                                {
                                    if (path.isWebSocket)
                                    {
                                        foundPath = true;
                                    }
                                }
                            }

                            if (foundPath)
                            {
                                ThreadPool.QueueUserWorkItem(async __ =>
                                {
                                    var ws = await context.AcceptWebSocketAsync("ws");

                                    var websocket = new WebSocketClient(ws.WebSocket)
                                    {
                                        Headers = request.Headers,
                                        Cookies = request.Cookies,
                                    };

                                    _onWebsocket?.Invoke(null, new PathContainer(request, response, websocket));
                                });
                            }
                        }

                        if (foundPath is false)
                        {
                            if (request.IsWebSocket)
                            {
                                // TODO: Check best way for refuse websocket connection.
                                string data = $"{request.RawRequest.HttpMethod.ToUpper()} {request.Path}";
                                response.Send(404, data);
                            }
                            else
                            {
                                // TODO: Check best may for refuse http connection.
                                string data = $"{request.RawRequest.HttpMethod.ToUpper()} {request.Path}";
                                response.Send(404, data);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Ignored
                        // TODO: DEBUG ERROR
                        Console.WriteLine(e);
                    }
                }

                Close();
            });
        }
    }
}