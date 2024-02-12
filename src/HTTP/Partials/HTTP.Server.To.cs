using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _To : ITo
            {
                private readonly Server _server;
                private HttpListener _listener;
                private bool _tryOpen, _tryClose;

                public _To(Server server)
                {
                    _server = server;
                }

                public bool IsOpened => _listener != null && _listener.IsListening;
                public Uri Host { get; private set; } = new Uri("http://0.0.0.0:80/");

                public void Open(Uri host)
                {
                    if (IsOpened || _tryClose || _tryOpen) return;
                    _tryOpen = true;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            var server = new HttpListener();

                            var httpUrl = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{host.Host}:{host.Port}/";

                            server.Prefixes.Add(httpUrl);

                            _server._on.m_onModify?.Invoke(null, server);

                            server.Start();

                            Host = host;

                            _listener = server;

                            _server._on.m_onOpen?.Invoke(null, null);

                            ReceiveRequests();
                        }
                        catch (Exception e)
                        {
                            _server._on.m_onError?.Invoke(null, e);
                        }
                        finally
                        {
                            _tryOpen = false;
                        }
                    });
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
                            _tryClose = false;
                            _listener = null;
                            _server._on.m_onClose?.Invoke(null, null);
                        }
                    });
                }

                private void ReceiveRequests()
                {
                    Task.Run(() =>
                    {
                        while (IsOpened)
                        {
                            HttpListenerContext context = null;
                            Netly.Logger.PushLog("Request entry.");

                            try
                            {
                                context = _listener.GetContext();
                            }
                            catch (Exception e)
                            {
                                context = null;
                                Netly.Logger.PushLog($"Request fail: {e}");
                            }
                            finally
                            {
                                if (context != null)
                                    Task.Run(() =>
                                    {
                                        Netly.Logger.PushLog("Task Init");

                                        try
                                        {
                                            var task = HandleConnection(context);
                                            Task.WaitAll(task);
                                        }
                                        catch (Exception e)
                                        {
                                            Netly.Logger.PushLog($"Task error: {e}");
                                        }

                                        Netly.Logger.PushLog("Task End");
                                    });
                            }
                        }

                        Close();
                    });
                }

                private async Task HandleConnection(HttpListenerContext context)
                {
                    Netly.Logger.PushLog("Request processing.");

                    var request = new Request(context.Request);
                    var response = new Response(context.Response);
                    var notFoundMessage = $"{request.Method.Method.ToUpper()} {request.Path}";

                    Netly.Logger.PushLog("Request starting.");

                    var skipNextMiddleware = false;

                    void RunMiddlewares(IMiddlewareContainer[] middlewares)
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

                    Netly.Logger.PushLog($"Global middleware. Search middlewares...");

                    var globalMiddlewares = _server.Middleware.Middlewares.ToList().FindAll(x =>
                    {
                        if (x.Path == _Middleware.GLOBAL_PATH)
                        {
                            // is only global path
                            return true;
                        }

                        return false;
                    });

                    if (!skipNextMiddleware)
                    {
                        RunMiddlewares(globalMiddlewares.ToArray());
                        Netly.Logger.PushLog($"[END] running global middleware (next: {!skipNextMiddleware})");
                    }

                    if (skipNextMiddleware) return;

                    #endregion

                    // LOCAL MIDDLEWARES

                    #region LOCAL MIDDLEWARE

                    Netly.Logger.PushLog($"Local middleware. Search middlewares...");

                    var localMiddlewares = _server.Middleware.Middlewares.ToList().FindAll(x =>
                    {
                        // only local middleware is allows
                        if (x.Path == _Middleware.GLOBAL_PATH) return false;

                        if (!x.UseParams)
                        {
                            // simple path compare
                            return Path.ComparePath(request.Path, x.Path);
                        }

                        // compare custom path
                        var result = Path.ParseParam(originalPath: x.Path, inputPath: request.Path);

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
                        Netly.Logger.PushLog($"[END] running local middleware (next: {!skipNextMiddleware})");
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
                            (
                                string.Equals(x.Method, _Map.ALL_MEHOD, StringComparison.CurrentCultureIgnoreCase)
                                ||
                                string.Equals(request.Method.Method, x.Method,
                                    StringComparison.CurrentCultureIgnoreCase)
                            );

                            if (!handleMethod) return false;
                        }

                        // compare regular path
                        if (!x.UseParams)
                        {
                            // simple path compare
                            return Path.ComparePath(request.Path, x.Path);
                        }

                        // compare custom path
                        var result = Path.ParseParam(originalPath: x.Path, inputPath: request.Path);

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
                                    string.Equals(x.Method, _Map.ALL_MEHOD, StringComparison.CurrentCultureIgnoreCase)
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

                        myPaths.ForEach(x => x.WebsocketCallback?.Invoke(request, websocket));

                        websocket.InitWebSocketServerSide();
                    }
                }
            }
        }
    }
}