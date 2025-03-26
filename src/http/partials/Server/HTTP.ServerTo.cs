using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class ServerTo : IHTTP.ServerTo
        {
            private readonly Server _server;
            private readonly List<WebSocket> _websocketList = new List<WebSocket>();
            private readonly object _websocketListLock = new object();
            private HttpListener _listener;
            private bool _tryOpen, _tryClose;

            public ServerTo(Server server)
            {
                _server = server;
            }

            public bool IsOpened => _listener != null && _listener.IsListening;
            public Uri Host { get; private set; } = new Uri("http://0.0.0.0:80/");

            public Task Open(Uri host)
            {
                if (IsOpened || _tryClose || _tryOpen) return Task.CompletedTask;
                _tryOpen = true;

                return Task.Run(() =>
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

                        _server.MyServerOn.OnModify?.Invoke(null, server);

                        server.Start();

                        Host = host;

                        _listener = server;

                        _server.MyServerOn.OnOpen?.Invoke(null, null);

                        InitAccept();
                    }
                    catch (Exception e)
                    {
                        _server.MyServerOn.OnError?.Invoke(null, e);
                    }
                    finally
                    {
                        _tryOpen = false;
                    }
                });
            }

            public Task Close()
            {
                if (_tryOpen || _tryClose || _listener == null) return Task.CompletedTask;

                _tryClose = true;

                return Task.Run(() =>
                {
                    try
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                            _listener.Abort();
                            _listener.Close();
                        }

                        lock (_websocketListLock)
                        {
                            foreach (var socket in _websocketList) socket.To.Close();
                        }
                    }
                    catch
                    {
                        // Ignored
                    }
                    finally
                    {
                        _tryClose = false;
                        _listener = null;
                        _server.MyServerOn.OnClose?.Invoke(null, null);
                    }
                });
            }

            public void WebsocketDataBroadcast(byte[] data, HTTP.MessageType messageType)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, messageType);
                }
            }

            public void WebsocketDataBroadcast(string data, HTTP.MessageType messageType)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, messageType);
                }
            }

            public void WebsocketDataBroadcast(string data, HTTP.MessageType messageType, Encoding encoding)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, messageType, encoding);
                }
            }

            public void WebsocketEventBroadcast(string name, byte[] data, HTTP.MessageType messageType)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data, messageType);
                }
            }

            public void WebsocketEventBroadcast(string name, string data, HTTP.MessageType messageType)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data, messageType);
                }
            }

            public void WebsocketEventBroadcast(string name, string data, MessageType messageType, Encoding encoding)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data, messageType, encoding);
                }
            }

            private void InitAccept()
            {
                _listener.BeginGetContext(AcceptCallback, null);

                return;

                void AcceptCallback(IAsyncResult result)
                {
                    if (IsOpened)
                    {
                        HandleContext(_listener.EndGetContext(result));
                        InitAccept();
                    }
                    else
                    {
                        Close();
                    }
                }

                void HandleContext(HttpListenerContext context)
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
                            if (socket?.WebSocket != null)
                                await socket.WebSocket.CloseAsync
                                (
                                    WebSocketCloseStatus.InternalServerError,
                                    string.Empty,
                                    CancellationToken.None
                                );

                            NetlyEnvironment.Logger.Create($"{this}: {e}");
                        }
                    });
                }
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
                var request = new ServerRequest(context.Request);

                var response = new ServerResponse(context.Response, socketContext);

                var middlewares = _server.MyMiddleware.Middlewares.Length <= byte.MinValue
                    ? new List<IHTTP.MiddlewareDescriptor>()
                    : _server.MyMiddleware.Middlewares.ToList();

                var descriptors = middlewares.Where(x =>
                {
                    // allow global middleware
                    if (x.Path == Middleware.GlobalPath) return true;

                    if (!x.UseParams)
                        // simple path compare
                        return Path.ComparePath(request.Path, x.Path);

                    // compare custom path
                    var result = Path.ParseParam(x.Path, request.Path);

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
                }).Select(x => (MiddlewareDescriptor)x).ToList();

                descriptors.Add
                (
                    new MiddlewareDescriptor
                    (
                        path: Middleware.GlobalPath,
                        useParams: false,
                        callback: (_, __, next) => MapMiddlewareCallback(context, request, response, next))
                );

                for (var i = 0; i < descriptors.Count; i++)
                {
                    var descriptor = descriptors[i];

                    try
                    {
                        descriptor.Next = descriptors[i + 1];
                    }
                    catch
                    {
                        descriptor.Next = null;
                    }
                }

                descriptors[0].Callback(request, response, () => descriptors[0].Execute(request, response));
            }

            private async void MapMiddlewareCallback(HttpListenerContext context, ServerRequest request,
                ServerResponse response, Action next)
            {
                if (!response.IsOpened)
                {
                    // request is already response by another middleware
                    next();
                    return;
                }

                // SEARCH ROUTE

                var map = _server.MyMap.MapList.FirstOrDefault(x =>
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
                            string.Equals(x.Method, Map.AllMethod, StringComparison.CurrentCultureIgnoreCase)
                            ||
                            string.Equals(request.Method.Method, x.Method,
                                StringComparison.CurrentCultureIgnoreCase);

                        if (!handleMethod) return false;
                    }

                    // compare regular path
                    if (!x.UseParams)
                        // simple path compare
                        return Path.ComparePath(request.Path, x.Path);

                    // compare custom path
                    var result = Path.ParseParam(x.Path, request.Path);

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
                            await response.WebSocketContext.WebSocket.CloseOutputAsync
                            (
                                WebSocketCloseStatus.EndpointUnavailable,
                                string.Empty,
                                CancellationToken.None
                            );
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                        }
                        finally
                        {
                            response.WebSocketContext.WebSocket.Dispose();
                            context.Response.OutputStream.Close();
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
                    var websocket = new WebSocket(response.WebSocketContext.WebSocket, request);

                    websocket.On.Open(() =>
                    {
                        lock (_websocketListLock)
                        {
                            _websocketList.Add(websocket);
                        }
                    });

                    websocket.On.Close(() =>
                    {
                        lock (_websocketListLock)
                        {
                            _websocketList.Remove(websocket);
                        }
                    });

                    map.WebsocketCallback?.Invoke(request, websocket);

                    websocket.InitWebSocketServerSide();
                }

                #endregion

                next();
            }
        }
    }
}