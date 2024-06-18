using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        public partial class Server
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

                            _server._serverOn.OnModify?.Invoke(null, server);

                            server.Start();

                            Host = host;

                            _listener = server;

                            _server._serverOn.OnOpen?.Invoke(null, null);

                            ReceiveRequests();
                        }
                        catch (Exception e)
                        {
                            _server._serverOn.OnError?.Invoke(null, e);
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
                            _server._serverOn.OnClose?.Invoke(null, null);
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

                private void ReceiveRequests()
                {
                    var thread = new Thread(() =>
                    {
                        while (IsOpened)
                        {
                            HttpListenerContext context = null;
                            NetlyEnvironment.Logger.Create("Request entry.");

                            try
                            {
                                context = _listener.GetContext();
                            }
                            catch (Exception e)
                            {
                                context = null;
                                NetlyEnvironment.Logger.Create($"Request fail: {e}");
                            }
                            finally
                            {
                                if (context != null)
                                    Task.Run(() =>
                                    {
                                        NetlyEnvironment.Logger.Create("Task Init");

                                        try
                                        {
                                            var task = HandleConnection(context);
                                            Task.WaitAll(task);
                                        }
                                        catch (Exception e)
                                        {
                                            NetlyEnvironment.Logger.Create($"Task error: {e}");
                                        }

                                        NetlyEnvironment.Logger.Create("Task End");
                                    });
                            }
                        }

                        Close();
                    }) { IsBackground = true };

                    thread.Start();
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
                    NetlyEnvironment.Logger.Create("Request processing.");

                    var request = new ServerRequest(context.Request);
                    var response = new ServerResponse(context.Response);
                    var notFoundMessage = DefaultHtmlBody($"[{request.Method.Method.ToUpper()}] {request.Path}");

                    NetlyEnvironment.Logger.Create("Request starting.");

                    var skipNextMiddleware = false;

                    void RunMiddlewares(IHTTP.MiddlewareDescriptor[] middlewares)
                    {
                        foreach (var middleware in middlewares)
                        {
                            if (skipNextMiddleware) break;

                            var @continue = middleware.Callback(request, response);

                            if (!@continue)
                            {
                                skipNextMiddleware = true;
                                response.Send(500, "Internal Server Error");
                                break;
                            }
                        }
                    }

                    // GLOBAL MIDDLEWARES

                    #region GLOBAL MIDDLEWARE

                    NetlyEnvironment.Logger.Create("Global middleware. Search middlewares...");

                    var globalMiddlewares = _server.Middleware.Middlewares.ToList().FindAll(x =>
                    {
                        if (x.Path == HTTP.Middleware.GlobalPath)
                            // is only global path
                            return true;

                        return false;
                    });

                    if (!skipNextMiddleware)
                    {
                        RunMiddlewares(globalMiddlewares.ToArray());
                        NetlyEnvironment.Logger.Create(
                            $"[END] running global middleware (next: {!skipNextMiddleware})");
                    }

                    if (skipNextMiddleware) return;

                    #endregion

                    // LOCAL MIDDLEWARES

                    #region LOCAL MIDDLEWARE

                    NetlyEnvironment.Logger.Create("Local middleware. Search middlewares...");

                    var localMiddlewares = _server.Middleware.Middlewares.ToList().FindAll(x =>
                    {
                        // only local middleware is allowing
                        if (x.Path == HTTP.Middleware.GlobalPath) return false;

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

                    if (!skipNextMiddleware)
                    {
                        RunMiddlewares(localMiddlewares.ToArray());
                        NetlyEnvironment.Logger.Create(
                            $"[END] running local middleware (next: {!skipNextMiddleware})");
                    }

                    if (skipNextMiddleware) return;

                    #endregion


                    // CALLBACK FOUNDER

                    #region CALLBACK FOUNDER

                    var myPaths = _server._map.m_mapList.FindAll(x =>
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
                                string.Equals(x.Method, HTTP.Map.ALL_MEHOD, StringComparison.CurrentCultureIgnoreCase)
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

                    #endregion

                    if (request.IsWebSocket == false) // IS HTTP CONNECTION
                    {
                        /*
                            var paths = _server._map.m_mapList.FindAll(x =>
                            {
                                var comparePath = Path.ComparePath(request.Path, x.Path);
                                Netly.Logger.PushLog($"Compare Path ({comparePath}): [{request.Path}] [{x.Path}]");
                                if (!comparePath) return false;


                                var compareMethod =
                                    string.Equals(x.Method, _Map.ALL_METHOD, StringComparison.CurrentCultureIgnoreCase)
                                    ||
                                    string.Equals(request.Method.Method, x.Method,
                                        StringComparison.CurrentCultureIgnoreCase);

                                Netly.Logger.PushLog(
                                    $"Compare Method ({compareMethod}): [{request.Method.Method.ToUpper()}] [{x.Method.ToUpper()}]");
                                if (!compareMethod) return false;

                                var isWebsocket = x.IsWebsocket;
                                Netly.Logger.PushLog($"IsWebSocket: {isWebsocket}");
                                if (isWebsocket) return false;

                                return true;
                            }).ToArray();
                        */

                        if (myPaths.Count <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }

                        myPaths.ForEach(x =>
                        {
                            x.HttpCallback?.Invoke(request, response);

                            if (response.IsOpened)
                            {
                                response.Send(508, $"Loop Detected {x.Path}");
                                throw new NotImplementedException($"NULL response detected on [path='{x.Path}']");
                            }
                        });
                    }
                    else // IS WEBSOCKET CONNECTION
                    {
                        /*
                            var paths = _server._map.m_mapList.FindAll(x =>
                            Path.ComparePath(x.Path, request.Path) && x.IsWebsocket);
                        */

                        if (myPaths.Count <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }

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

                        myPaths.ForEach(x => x.WebsocketCallback?.Invoke(request, websocket));

                        websocket.InitWebSocketServerSide();
                    }
                }
            }
        }
    }
}