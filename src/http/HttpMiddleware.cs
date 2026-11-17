using System;
using System.Collections.Generic;

namespace Netly
{
    public sealed class HttpMiddleware
    {
        public const string GlobalPath = "*";

        private readonly List<HttpMiddlewareDescriptor> _middlewares;

        internal HttpMiddleware()
        {
            _middlewares = new List<HttpMiddlewareDescriptor>();
        }

        public HttpMiddlewareDescriptor[] Middlewares => _middlewares.ToArray();

        public bool Add(Action<HttpServerRequest, HttpServerResponse, Action> middleware)
        {
            return Add(GlobalPath, middleware);
        }

        public bool Add(string path, Action<HttpServerRequest, HttpServerResponse, Action> middleware)
        {
            if (middleware == null) return false;

            if (!GlobalPath.Equals(path))
            {
                path = (path ?? string.Empty).Trim();
                HttpPath.AddEndOfPath(ref path);
                if (string.IsNullOrWhiteSpace(path)) return false;
            }

            if (GlobalPath.Equals(path) || HttpPath.IsValid(path))
            {
                var descriptor = new HttpMiddlewareDescriptor(path, HttpPath.IsParamPath(path), middleware);
                _middlewares.Add(descriptor);
                return true;
            }

            return false;
        }
    }
}