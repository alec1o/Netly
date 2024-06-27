using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

                        var httpUrl = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{host.Host}:{host.Port}/";

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

            public void WebsocketDataBroadcast(byte[] data, bool isText)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, isText);
                }
            }

            public void WebsocketDataBroadcast(string data, bool isText)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, isText);
                }
            }

            public void WebsocketDataBroadcast(string data, bool isText, Encoding encoding)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Data(data, isText, encoding);
                }
            }

            public void WebsocketEventBroadcast(string name, byte[] data)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data);
                }
            }

            public void WebsocketEventBroadcast(string name, string data)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data);
                }
            }

            public void WebsocketEventBroadcast(string name, string data, Encoding encoding)
            {
                lock (_websocketListLock)
                {
                    foreach (var ws in _websocketList) ws.To.Event(name, data, encoding);
                }
            }

            private void InitAccept()
            {
                _listener.BeginGetContext(AcceptCallback, null);

                void AcceptCallback(IAsyncResult result)
                {
                    if (IsOpened)
                    {
                        var context = _listener.EndGetContext(result);

                        Task.Run(async () =>
                        {
                            try
                            {
                                await HandleConnection(context);
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create($"{this}: {e}");
                            }
                        });

                        InitAccept();
                    }
                    else
                    {
                        Close();
                    }
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

            private async Task HandleConnection(HttpListenerContext context)
            {
                var request = new ServerRequest(context.Request);

                var response = new ServerResponse(context.Response);

                var notFoundMessage = DefaultHtmlBody($"[{request.Method.Method.ToUpper()}] {request.Path}");

                var middlewares = _server.MyMiddleware.Middlewares.ToList();

                if (middlewares.Count > 0)
                {
                    var descriptors = middlewares.Where(x =>
                    {
                        // allow global middleware
                        if (x.Path == Middleware.GlobalPath) return true;

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
                    }).Select(x => (MiddlewareDescriptor)x).ToList();

                    if (descriptors.Count > 0)
                    {
                        var count = descriptors.Count;

                        for (var i = 0; i < count; i++)
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

                        var mainDescriptor = descriptors[0];
                        mainDescriptor.Callback(request, response, () => mainDescriptor.Execute(request, response));
                    }
                }

                // SEARCH ROUTE
                var map = _server.MyMap.m_mapList.FirstOrDefault(x =>
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
                            string.Equals(x.Method, Map.ALL_MEHOD, StringComparison.CurrentCultureIgnoreCase)
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
                    response.Send(404, notFoundMessage);
                    return;
                }

                #region HANDLE HTTP REQUEST

                if (!request.IsWebSocket) // IS HTTP CONNECTION
                {
                    map.HttpCallback(request, response);
                }
                else // IS WEBSOCKET CONNECTION
                {
                    var ws = await context.AcceptWebSocketAsync(null);

                    var websocket = new WebSocket(ws.WebSocket, request);

                    lock (_websocketListLock)
                    {
                        _websocketList.Add(websocket);
                    }

                    websocket.On.Close(() =>
                    {
                        lock (_websocketListLock)
                        {
                            _websocketList.Add(websocket);
                        }
                    });

                    map.WebsocketCallback?.Invoke(request, websocket);

                    websocket.InitWebSocketServerSide();
                }

                #endregion
            }
        }
    }
}