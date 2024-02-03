﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using Netly.Interfaces;
using IRequest = Netly.Interfaces.HTTP.IRequest;
using IResponse = Netly.Interfaces.HTTP.IResponse;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Map : Interfaces.HTTP.Server.IMap
            {
                internal const string ALL_MEHOD = "*";

                internal struct MapContainer
                {
                    public string Path { get; private set; }
                    public string Method { get; private set; }
                    public bool IsWebsocket { get; private set; }
                    public Action<IRequest, IResponse> HttpCallback { get; private set; }
                    public Action<IRequest, WebSocket> WebsocketCallback { get; private set; }

                    public MapContainer
                    (
                        string path,
                        string method,
                        bool isWebsocket,
                        Action<IRequest, IResponse> httpCallback,
                        Action<IRequest, WebSocket> websocketCallback
                    )
                    {
                        this.Path = path;
                        this.Method = method;
                        this.IsWebsocket = isWebsocket;
                        this.HttpCallback = httpCallback;
                        this.WebsocketCallback = websocketCallback;
                    }
                }

                public readonly HTTP.Server m_server;

                public readonly List<MapContainer> m_mapList;

                public _Map(HTTP.Server server)
                {
                    this.m_server = server;
                    m_mapList = new List<MapContainer>();
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

                    if (Path.IsValid(path))
                    {
                        var map = new MapContainer
                        (
                            path: path,
                            method: String.Empty,
                            isWebsocket: true,
                            httpCallback: httpCallback,
                            websocketCallback: websocketCallback
                        );
                        m_mapList.Add(map);
                    }
                }

                public void WebSocket(string path, Action<IRequest, HTTP.WebSocket> callback)
                {
                    Add
                    (
                        path: path,
                        method: String.Empty,
                        isWebsocket: true,
                        httpCallback: null,
                        websocketCallback: callback
                    );
                    
                }

                public void All(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: ALL_MEHOD,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Get(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Get.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Put(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Put.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Head(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Head.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Post(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Post.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Patch(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        // HttpMethod.Patch doesn't exist.
                        method: new HttpMethod("Patch").Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Delete(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Delete.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Trace(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Trace.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }

                public void Options(string path, Action<IRequest, IResponse> callback)
                {
                    Add
                    (
                        path: path,
                        method: HttpMethod.Options.Method,
                        isWebsocket: true,
                        httpCallback: callback,
                        websocketCallback: null
                    );
                }
            }
        }
    }
}