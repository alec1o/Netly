using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public class HttpServer : IHttpServer
    {
        private HttpListener _listener;
        private EventHandler<object> _onOpen, _onClose, _onError;
        private bool _tryOpen, _tryClose;

        private readonly List<(string path, bool mapAllMethod, HttpMethod method, Action<Request, Response> callback)>
            _httpMap;

        private readonly List<(string path, Action<Request, WebSocketClient> callback)> _wsMap;

        public bool IsOpen => _listener != null && _listener.IsListening;
        public Uri Host { get; private set; } = new Uri("http://0.0.0.0:80/");

        public HttpServer()
        {
            _httpMap =
                new List<(string path, bool mapAllMethod, HttpMethod method, Action<Request, Response> callback)>();
            _wsMap = new List<(string path, Action<Request, WebSocketClient> callback)>();
        }

        public void Open(Uri host)
        {
            if (IsOpen || _tryClose || _tryOpen) return;
            _tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    HttpListener server = new HttpListener();

                    string httpUrl = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{host.Host}:{host.Port}/";

                    server.Prefixes.Add(httpUrl);

                    server.Start();

                    Host = host;

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

            Task.Run(() =>
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

        public void MapAll(string path, Action<Request, Response> callback)
        {
            InternalMap(path, true, HttpMethod.Options, callback);
        }

        public void MapGet(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Get, callback);
        }

        public void MapPut(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Put, callback);
        }

        public void MapHead(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Head, callback);
        }

        public void MapPost(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Post, callback);
        }

        public void MapPatch(string path, Action<Request, Response> callback)
        {
            // I DON'T KNOW WHY!
            // But (HttpMethod.Patch) don't exist.
            // Creating manual HttpMethod.
            // TODO: Fix it for use HttpMethod.Patch
            var method = new HttpMethod("PATCH");
            InternalMap(path, false, method, callback);
        }

        public void MapTrace(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Trace, callback);
        }

        public void MapDelete(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Delete, callback);
        }

        public void MapOptions(string path, Action<Request, Response> callback)
        {
            InternalMap(path, false, HttpMethod.Options, callback);
        }

        private void InternalMap(string path, bool mapAllMethods, HttpMethod method, Action<Request, Response> callback)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            _httpMap.Add((path.Trim(), mapAllMethods, method, callback));
        }

        public void MapWebSocket(string path, Action<Request, WebSocketClient> callback)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            _wsMap.Add((path.Trim(), callback));
        }

        private void _ReceiveRequests()
        {
            Task.Run(() =>
            {
                while (IsOpen)
                {
                    try
                    {
                        var context = _listener.GetContext();
                        var request = new Request(context.Request);
                        var response = new Response(context.Response);
                        var notFoundMessage = $"{request.RawRequest.HttpMethod.ToUpper()} {request.Path}";

                        #region Debug
#if false
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
#endif
                        #endregion

                        if (request.IsWebSocket == false) // IS HTTP CONNECTION
                        {
                            var paths = _httpMap.FindAll(x =>
                                    request.ComparePath(x.path) && (request.Method == x.method || x.mapAllMethod))
                                .ToArray();

                            if (paths.Length <= 0)
                            {
                                response.Send(404, notFoundMessage);
                                continue;
                            }

                            foreach (var path in paths)
                            {
                                path.callback?.Invoke(request, response);
                            }
                        }
                        else // IS WEBSOCKET CONNECTION
                        {
                            var paths = _wsMap.FindAll(x => x.path == request.Path);

                            if (paths.Count <= 0)
                            {
                                response.Send(404, notFoundMessage);
                                continue;
                            }

                            Task.Run(async () =>
                            {
                                var ws = await context.AcceptWebSocketAsync(subProtocol: null);

                                var websocket = new WebSocketClient(ws.WebSocket)
                                {
                                    Headers = request.Headers,
                                    Cookies = request.Cookies,
                                    Uri = request.RawRequest.Url,
                                };

                                foreach (var path in paths)
                                {
                                    path.callback?.Invoke(request, websocket);
                                }

                                websocket.InitWebSocketServerSide();
                            });
                        }
                    }
                    catch
                    {
                        // Ignored
                    }
                }

                Close();
            });
        }
    }
}