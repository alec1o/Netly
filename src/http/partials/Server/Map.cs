using System;
using System.Collections.Generic;
using System.Net.Http;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class Map : IHTTP.Map
        {
            internal const string AllMethod = "*";

            public readonly List<MapDescriptor> MapList;

            public readonly Server Server;

            public Map(Server server)
            {
                Server = server;
                MapList = new List<MapDescriptor>();
            }

            public void WebSocket(string path, Action<IHTTP.ServerRequest, IHTTP.WebSocket> callback)
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

            public void All(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
            {
                Add
                (
                    path,
                    AllMethod,
                    false,
                    callback,
                    null
                );
            }

            public void Get(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Put(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Head(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Post(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Patch(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Delete(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Trace(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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

            public void Options(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
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
                Action<IHTTP.ServerRequest, IHTTP.ServerResponse> httpCallback,
                Action<IHTTP.ServerRequest, WebSocket> websocketCallback
            )
            {
                path = (path ?? string.Empty).Trim();

                NetlyEnvironment.Logger.Create(
                    $"Add Path: {path} | IsValid: {Path.IsValid(path)} | UseParams: {Path.IsParamPath(path)}"
                );

                if (Path.IsValid(path))
                {
                    var map = new MapDescriptor
                    (
                        path,
                        Path.IsParamPath(path),
                        method,
                        isWebsocket,
                        httpCallback,
                        websocketCallback
                    );
                    MapList.Add(map);
                }
            }
        }
    }
}