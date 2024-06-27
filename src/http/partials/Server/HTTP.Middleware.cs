using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class Middleware : IHTTP.Middleware
        {
            public const string GlobalPath = "*";

            private readonly List<IHTTP.MiddlewareDescriptor> _middlewares;
            public readonly Server _server;

            public Middleware(Server server)
            {
                _server = server;
                _middlewares = new List<IHTTP.MiddlewareDescriptor>();
            }

            public IHTTP.MiddlewareDescriptor[] Middlewares => _middlewares.ToArray();

            public bool Add(Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> middleware)
            {
                return Add(GlobalPath, middleware);
            }

            public bool Add(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> middleware)
            {
                if (middleware == null) return false;

                if (!GlobalPath.Equals(path))
                {
                    path = (path ?? string.Empty).Trim();
                    Path.AddEndOfPath(ref path);
                    if (string.IsNullOrWhiteSpace(path)) return false;
                }

                if (GlobalPath.Equals(path) || Path.IsValid(path))
                {
                    var descriptor = new MiddlewareDescriptor(path, Path.IsParamPath(path), middleware);
                    _middlewares.Add(descriptor);
                    return true;
                }

                return false;
            }
        }
    }
}