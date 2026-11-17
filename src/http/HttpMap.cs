using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Netly
{
    public sealed class HttpMap
    {
        internal const string AllMethod = "*";

        internal readonly List<HttpMapDescriptor> MapList;

        internal HttpMap()
        {
            MapList = new List<HttpMapDescriptor>();
        }

        public void WebSocket(string path, Action<HttpServerRequest, WebSocketClient> callback)
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

        public void All(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Get(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Put(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Head(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Post(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Patch(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Delete(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Trace(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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

        public void Options(string path, Action<HttpServerRequest, HttpServerResponse> callback)
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
            Action<HttpServerRequest, HttpServerResponse> httpCallback,
            Action<HttpServerRequest, WebSocketClient> websocketCallback
        )
        {
            path = (path ?? string.Empty).Trim();

            Logger.Singleton.Submit(
                $"Add Path: {path} | IsValid: {HttpPath.IsValid(path)} | UseParams: {HttpPath.IsParamPath(path)}"
            );

            if (HttpPath.IsValid(path))
            {
                var map = new HttpMapDescriptor
                (
                    path,
                    HttpPath.IsParamPath(path),
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