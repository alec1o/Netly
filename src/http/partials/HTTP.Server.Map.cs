using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Netly
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Map : IMap
            {
                internal const string ALL_MEHOD = "*";

                public readonly List<MapContainer> m_mapList;

                public readonly Server m_server;

                public _Map(Server server)
                {
                    m_server = server;
                    m_mapList = new List<MapContainer>();
                }

                public void WebSocket(string path, Action<IRequest, WebSocket> callback)
                {
                    Add
                    (
                        path,
                        string.Empty,
                        true,
                        null,
                        callback
                    );
                }

                public void All(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        ALL_MEHOD,
                        false,
                        callback,
                        null
                    );
                }

                public void Get(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Get.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Put(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Put.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Head(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Head.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Post(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Post.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Patch(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        // HttpMethod.Patch doesn't exist.
                        new HttpMethod("Patch").Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Delete(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Delete.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Trace(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Trace.Method,
                        false,
                        callback,
                        null
                    );
                }

                public void Options(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path,
                        HttpMethod.Options.Method,
                        false,
                        callback,
                        null
                    );
                }

                private void Add
                (
                    string path,
                    string method,
                    bool isWebsocket,
                    Action<IRequest, IResponse> httpCallback,
                    Action<IRequest, WebSocket> websocketCallback
                )
                {
                    path = (path ?? string.Empty).Trim();

                    MyNetly.Logger.PushLog
                    (
                        $"Add Path: {path} | IsValid: {Path.IsValid(path)} | UseParams: {Path.IsParamPath(path)}"
                    );

                    if (Path.IsValid(path))
                    {
                        var map = new MapContainer
                        (
                            path: path,
                            useParams: Path.IsParamPath(path),
                            method: method,
                            isWebsocket: isWebsocket,
                            httpCallback: httpCallback,
                            websocketCallback: websocketCallback
                        );
                        m_mapList.Add(map);
                    }
                }

                internal struct MapContainer
                {
                    public bool UseParams { get; }
                    public string Path { get; }
                    public string Method { get; }
                    public bool IsWebsocket { get; }
                    public Action<IRequest, IResponse> HttpCallback { get; }
                    public Action<IRequest, WebSocket> WebsocketCallback { get; }

                    public MapContainer
                    (
                        string path,
                        bool useParams,
                        string method,
                        bool isWebsocket,
                        Action<IRequest, IResponse> httpCallback,
                        Action<IRequest, WebSocket> websocketCallback
                    )
                    {
                        Path = path;
                        UseParams = useParams;
                        Method = method;
                        IsWebsocket = isWebsocket;
                        HttpCallback = httpCallback;
                        WebsocketCallback = websocketCallback;
                    }
                }
            }
        }
    }
}