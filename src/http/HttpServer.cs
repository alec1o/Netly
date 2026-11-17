using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     HTTP server with routes, middleware and WebSocket routes.
    /// </summary>
    public class HttpServer
    {
        private readonly List<WebSocketClient> _websocketList = new List<WebSocketClient>();
        private readonly object _websocketListLock = new object();
        private HttpListener _listener;
        private bool _tryOpen, _tryClose;
        private EventHandler _onOpen, _onClose;
        private EventHandler<HttpListener> _onModify;
        private EventHandler<Exception> _onError;

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a server.
        /// </summary>
        public HttpServer()
        {
            Map = new HttpMap();
            Middleware = new HttpMiddleware();
        }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this server.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the server is listening.
        /// </summary>
        public bool IsOpened => _listener != null && _listener.IsListening;

        /// <summary>
        ///     Address where the server is listening.
        /// </summary>
        public Uri Host { get; private set; } = new Uri("http://0.0.0.0:80/");

        /// <summary>
        ///     Routes of the server.
        /// </summary>
        public HttpMap Map { get; }

        /// <summary>
        ///     Middleware that runs before the routes.
        /// </summary>
        public HttpMiddleware Middleware { get; }

        /// <summary>
        ///     Called with the new listener before it starts, change the listener options here.
        /// </summary>
        public void OnModify(Action<HttpListener> callback)
        {
            _onModify += (sender, listener) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(listener));
        }

        /// <summary>
        ///     Called when the server starts listening.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _onOpen += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the server is closed.
        /// </summary>
        public void OnClose(Action callback)
        {
            _onClose += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the server fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data to all WebSocket clients connected to the routes of this server.
        /// </summary>
        public void BroadcastWebSocketData(byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var socket in WebSockets()) socket.SendData(data, delivery);
        }

        /// <summary>
        ///     Sends a named event to all WebSocket clients connected to the routes of this server.
        /// </summary>
        public void BroadcastWebSocketEvent(string name, byte[] data,
            WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var socket in WebSockets()) socket.SendEvent(name, data, delivery);
        }

        private WebSocketClient[] WebSockets()
        {
            lock (_websocketListLock)
            {
                return _websocketList.ToArray();
            }
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts listening and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Uri host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            if (IsOpened || _tryClose || _tryOpen) return;

            _tryOpen = true;

            Task.Run(() =>
            {
                try
                {
                    var server = new HttpListener();

                    var hostScheme = host.Host.ToLower();

                    switch (hostScheme)
                    {
                        case "0.0.0.0":
                        case "localhost":
                        case "::":
                        case "*":
                        case ".":
                            hostScheme = "*";
                            break;
                    }

                    var httpUrl = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{hostScheme}:{host.Port}/";

                    server.Prefixes.Add(httpUrl);

                    _onModify?.Invoke(null, server);

                    server.Start();

                    Host = host;

                    _listener = server;

                    _onOpen?.Invoke(null, null);

                    AcceptTrigger();
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                    _onError?.Invoke(null, e);
                }
                finally
                {
                    _tryOpen = false;
                }
            });
        }

        /// <summary>
        ///     Stops listening and returns without waiting, <see cref="OnClose" /> is called when it is done.
        /// </summary>
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

                    foreach (var socket in WebSockets()) socket.Close();
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                }
                finally
                {
                    _tryClose = false;
                    _listener = null;
                    _onClose?.Invoke(null, null);
                }
            });
        }

        /* ---- ACCEPT --- */

        private void AcceptTrigger()
        {
            for (;;)
            {
                IAsyncResult result;

                try
                {
                    result = _listener.BeginGetContext(AcceptHandler, null);
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                    return;
                }

                // a context that was ready inside BeginGetContext is handled here, a recursion would overflow the stack
                if (result.CompletedSynchronously is false || AcceptComplete(result) is false) return;
            }
        }

        private void AcceptHandler(IAsyncResult result)
        {
            if (result.CompletedSynchronously) return;

            if (AcceptComplete(result)) AcceptTrigger();
        }

        // true when the next accept must be started
        private bool AcceptComplete(IAsyncResult result)
        {
            try
            {
                if (IsOpened)
                {
                    HandleContext(_listener.EndGetContext(result));
                    return true;
                }

                Close();
            }
            catch (Exception e)
            {
                // one request that fails (aborted by the client) must not stop the server
                Logger.Singleton.Submit(e);

                return IsOpened;
            }

            return false;
        }

        private void HandleContext(HttpListenerContext context)
        {
            Task.Run(async () =>
            {
                HttpListenerWebSocketContext socket = null;

                try
                {
                    if (context.Request.IsWebSocketRequest)
                        socket = await context.AcceptWebSocketAsync(null);

                    HandleConnection(context, socket);
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit($"{this}: {e}");
                    if (socket?.WebSocket != null)
                        await socket.WebSocket.CloseAsync
                        (
                            WebSocketCloseStatus.InternalServerError,
                            string.Empty,
                            CancellationToken.None
                        );
                }
            });
        }

        private static string DefaultHtmlBody(string content)
        {
            var html =
                $@"<body style='background-color: #0a0f19'>
                            <p style='font-size: 12px; color: #fff; letter-spacing: 2.5px; font-family: monospace, cursive'>{content}</p>
                        </body>";

            return html;
        }

        private void HandleConnection(HttpListenerContext context, HttpListenerWebSocketContext socketContext)
        {
            var request = new HttpServerRequest(context.Request);

            var response = new HttpServerResponse(context.Response, socketContext);

            var middlewares = Middleware.Middlewares.Length <= byte.MinValue
                ? new List<HttpMiddlewareDescriptor>()
                : Middleware.Middlewares.ToList();

            var descriptors = middlewares.Where(x =>
            {
                // allow global middleware
                if (x.Path == HttpMiddleware.GlobalPath) return true;

                if (!x.UseParams)
                    // simple path compare
                    return HttpPath.ComparePath(request.Path, x.Path);

                // compare custom path
                var result = HttpPath.ParseParam(x.Path, request.Path);

                // is custom path invalid?
                if (!result.Valid) return false;

                // set values in request object
                foreach (var myParam in result.Params)
                {
                    // only add value if not exist for prevent exception "Key in use!"
                    // optimization: if key exist it mean that it was added before with other callback
                    if (request.Params.ContainsKey(myParam.Key)) break;
                    // save params on object
                    request.Params.Add(myParam.Key, myParam.Value);
                }

                return true;
            }).ToList();

            descriptors.Add
            (
                new HttpMiddlewareDescriptor
                (
                    path: HttpMiddleware.GlobalPath,
                    useParams: false,
                    callback: (_, __, next) => MapMiddlewareCallback(context, request, response, next))
            );

            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];

                descriptor.Next = i + 1 < descriptors.Count ? descriptors[i + 1] : null;
            }

            try
            {
                descriptors[0].Callback(request, response, () => descriptors[0].Execute(request, response));
            }
            catch (Exception e)
            {
                Fail(response, e);
            }
        }

        // app failed: OnError and 500, the server keeps running
        private void Fail(HttpServerResponse response, Exception e)
        {
            Logger.Singleton.Submit(e);
            _onError?.Invoke(null, e);

            try
            {
                response.Send(500);
            }
            catch (Exception exception)
            {
                Logger.Singleton.Submit(exception);
            }
        }

        private async void MapMiddlewareCallback(HttpListenerContext context, HttpServerRequest request,
            HttpServerResponse response, Action next)
        {
            // async void: an exception here would crash the process
            try
            {
                await MapCallback(context, request, response, next);
            }
            catch (Exception e)
            {
                Fail(response, e);
            }
        }

        private async Task MapCallback(HttpListenerContext context, HttpServerRequest request,
            HttpServerResponse response, Action next)
        {
            if (!response.IsOpened)
            {
                // request is already response by another middleware
                next();
                return;
            }

            // SEARCH ROUTE

            var map = Map.MapList.FirstOrDefault(x =>
            {
                // websocket connection
                if (request.IsWebSocket)
                {
                    if (!x.IsWebsocket) return false;
                }
                // http connection
                else
                {
                    // handle all method
                    var handleMethod =
                        string.Equals(x.Method, HttpMap.AllMethod, StringComparison.CurrentCultureIgnoreCase)
                        ||
                        string.Equals(request.Method.Method, x.Method,
                            StringComparison.CurrentCultureIgnoreCase);

                    if (!handleMethod) return false;
                }

                // compare regular path
                if (!x.UseParams)
                    // simple path compare
                    return HttpPath.ComparePath(request.Path, x.Path);

                // compare custom path
                var result = HttpPath.ParseParam(x.Path, request.Path);

                // custom path is valid.
                if (result.Valid)
                {
                    // set values in request object
                    foreach (var myParam in result.Params)
                    {
                        // only add value if not exist for prevent exception "Key in use!"
                        // optimization: if key exist it mean that it was added before with other callback
                        if (request.Params.ContainsKey(myParam.Key)) break;
                        // save params on object
                        request.Params.Add(myParam.Key, myParam.Value);
                    }

                    return true;
                }

                return false;
            });

            if (map == null)
            {
                if (request.IsWebSocket)
                {
                    try
                    {
                        // https://github.com/dotnet/runtime/issues/81762#issuecomment-1421047699
                        await response.WebSocketContext.WebSocket.CloseAsync
                        (
                            WebSocketCloseStatus.EndpointUnavailable,
                            "Empty handler.",
                            CancellationToken.None
                        );
                    }
                    catch (Exception e)
                    {
                        Logger.Singleton.Submit(e);
                    }
                    finally
                    {
                        try
                        {
                            context.Response.Close();
                        }
                        catch (Exception e)
                        {
                            Logger.Singleton.Submit(e);
                        }
                    }

                    return;
                }

                if (context.Response.OutputStream.CanWrite)
                {
                    response.Headers["X-XSS-Protection"] = "1; mode=block";
                    response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    response.Headers["Server"] = "NETLY HTTP/S";
                    response.Send(404, DefaultHtmlBody($"[{request.Method.Method.ToUpper()}] {request.Path}"));
                }

                next();
                return;
            }

            #region HANDLE HTTP REQUEST

            if (!request.IsWebSocket) // IS HTTP CONNECTION
            {
                map.HttpCallback(request, response);
            }
            else // IS WEBSOCKET CONNECTION
            {
                var websocket = new WebSocketClient(response.WebSocketContext.WebSocket, request);

                websocket.OnOpen(() =>
                {
                    lock (_websocketListLock)
                    {
                        _websocketList.Add(websocket);
                    }
                });

                websocket.OnClose(() =>
                {
                    lock (_websocketListLock)
                    {
                        _websocketList.Remove(websocket);
                    }
                });

                map.WebsocketCallback?.Invoke(request, websocket);

                websocket.ServerInitializer();
            }

            #endregion

            next();
        }
    }
}
