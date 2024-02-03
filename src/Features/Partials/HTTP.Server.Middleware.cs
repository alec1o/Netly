using System;
using System.Collections.Generic;
using IRequest = Netly.Interfaces.HTTP.IRequest;
using IResponse = Netly.Interfaces.HTTP.IResponse;
using IMiddlewareContainer = Netly.Interfaces.HTTP.Server.IMiddlewareContainer;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Middleware : Interfaces.HTTP.Server.IMiddleware
            {
                public const string GLOBAL_PATH = "*";
                public readonly HTTP.Server m_server;

                private readonly List<IMiddlewareContainer> _middlewares;

                public _Middleware(HTTP.Server server)
                {
                    this.m_server = server;
                    _middlewares = new List<IMiddlewareContainer>();
                }

                public IMiddlewareContainer[] Middlewares => _middlewares.ToArray();

                public bool Add(Func<IRequest, IResponse, bool> middleware)
                {
                    return Add(GLOBAL_PATH, middleware);
                }

                public bool Add(string path, Func<IRequest, IResponse, bool> middleware)
                {
                    if (middleware == null) return false;
                    
                    path = (path ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(path)) return false;

                    if (GLOBAL_PATH.Equals(path) || Path.IsValid(path))
                    {
                        _middlewares.Add(new MiddlewareContainer(path, middleware));
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}