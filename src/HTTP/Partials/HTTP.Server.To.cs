using System;
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
                public readonly Server m_server;
                private HttpListener _listener;
                private bool _tryOpen, _tryClose;

                public _To(Server server)
                {
                    m_server = server;
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

                            m_server._on.m_onModify?.Invoke(null, server);

                            server.Start();

                            Host = host;

                            _listener = server;

                            m_server._on.m_onOpen?.Invoke(null, null);

                            ReceiveRequests();
                        }
                        catch (Exception e)
                        {
                            m_server._on.m_onError?.Invoke(null, e);
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
                            m_server._on.m_onClose?.Invoke(null, null);
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
                            Console.WriteLine("Request entry.");

                            try
                            {
                                context = _listener.GetContext();
                            }
                            catch (Exception e)
                            {
                                context = null;
                                Console.WriteLine($"Request fail: {e}");
                            }
                            finally
                            {
                                if (context != null)
                                    Task.Run(() =>
                                    {
                                        Console.WriteLine("Task Init");

                                        try
                                        {
                                            var task = HandleConnection(context);
                                            Task.WaitAll(task);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine($"Task error: {e}");
                                        }

                                        Console.WriteLine("Task End");
                                    });
                            }
                        }

                        Close();
                    });
                }

                private async Task HandleConnection(HttpListenerContext context)
                {
                    Console.WriteLine("Request processing.");

                    var request = new Request(context.Request);
                    var response = new Response(context.Response);
                    var notFoundMessage = $"{request.Method.Method.ToUpper()} {request.Path}";

                    Console.WriteLine("Request starting.");

                    var skipConnectionByMiddleware = false;

                    void RunMiddlewares(IMiddlewareContainer[] middlewares)
                    {
                        foreach (var middleware in middlewares)
                        {
                            if (skipConnectionByMiddleware) break;

                            var @continue = middleware.Callback(request, response);

                            if (!@continue)
                            {
                                skipConnectionByMiddleware = true;
                                response.Send(500, "Internal Server Error");
                                break;
                            }
                        }
                    }

                    // GLOBAL MIDDLEWARES
                    var globalMiddlewares = m_server.Middleware.Middlewares.ToList()
                        .FindAll(x => x.Path == _Middleware.GLOBAL_PATH);
                    Console.WriteLine($"Run global middleware (skip: {skipConnectionByMiddleware})");
                    RunMiddlewares(globalMiddlewares.ToArray());

                    // LOCAL MIDDLEWARES
                    var localMiddlewares = m_server.Middleware.Middlewares.ToList()
                        .FindAll(x => x.Path != _Middleware.GLOBAL_PATH && Path.ComparePath(request.Path, x.Path));
                    Console.WriteLine($"Run local middleware (skip: {skipConnectionByMiddleware})");
                    RunMiddlewares(localMiddlewares.ToArray());

                    Console.WriteLine($"Done run middleware (skip: {skipConnectionByMiddleware})");

                    if (skipConnectionByMiddleware) return;

                    Console.WriteLine($"Is WebSocket: {request.IsWebSocket}");
                    if (request.IsWebSocket == false) // IS HTTP CONNECTION
                    {
                        var paths = m_server._map.m_mapList.FindAll(x =>
                        {
                            var comparePath = Path.ComparePath(request.Path, x.Path);
                            Console.WriteLine($"Compare Path ({comparePath}): [{request.Path}] [{x.Path}]");
                            if (!comparePath) return false;


                            var compareMethod =
                                string.Equals(x.Method, _Map.ALL_MEHOD, StringComparison.CurrentCultureIgnoreCase)
                                ||
                                string.Equals(request.Method.Method, x.Method,
                                    StringComparison.CurrentCultureIgnoreCase);

                            Console.WriteLine(
                                $"Compare Method ({compareMethod}): [{request.Method.Method.ToUpper()}] [{x.Method.ToUpper()}]");
                            if (!compareMethod) return false;

                            var isWebsocket = x.IsWebsocket;
                            Console.WriteLine($"IsWebSocket: {isWebsocket}");
                            if (isWebsocket) return false;

                            return true;
                        }).ToArray();

                        Console.WriteLine($"HTTP Path len: {paths.Length}");
                        if (paths.Length <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }

                        foreach (var path in paths)
                        {
                            path.HttpCallback?.Invoke(request, response);

                            if (response.IsOpened)
                            {
                                response.Send(508, $"Loop Detected {path.Path}");
                                throw new NotImplementedException(
                                    $"NULL response detected on [path='{path.Path}']");
                            }
                        }
                    }
                    else // IS WEBSOCKET CONNECTION
                    {
                        var paths = m_server._map.m_mapList.FindAll(x =>
                            Path.ComparePath(x.Path, request.Path) && x.IsWebsocket);

                        if (paths.Count <= 0)
                        {
                            response.Send(404, notFoundMessage);
                            return;
                        }


                        var ws = await context.AcceptWebSocketAsync(null);

                        var websocket = new WebSocket(ws.WebSocket, request);

                        foreach (var path in paths) path.WebsocketCallback?.Invoke(request, websocket);

                        websocket.InitWebSocketServerSide();
                    }
                }
            }
        }
    }
}