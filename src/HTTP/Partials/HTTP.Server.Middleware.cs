using System;
using System.Collections.Generic;

namespace Netly
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Middleware : IMiddleware
            {
                public const string GLOBAL_PATH = "*";

                private readonly List<IMiddlewareInfo> _middlewares;
                public readonly Server m_server;

                public _Middleware(Server server)
                {
                    m_server = server;
                    _middlewares = new List<IMiddlewareInfo>();
                }

                public IMiddlewareInfo[] Middlewares => _middlewares.ToArray();

                public bool Add(Func<IRequest, IResponse, bool> middleware)
                {
                    return Add(GLOBAL_PATH, middleware);
                }

                public bool Add(string path, Func<IRequest, IResponse, bool> middleware)
                {
                    if (middleware == null) return false;

                    path = (path ?? string.Empty).Trim();
                    
                    Path.AddEndOfPath(ref path);

                    if (string.IsNullOrWhiteSpace(path)) return false;

                    if (GLOBAL_PATH.Equals(path) || Path.IsValid(path))
                    {
                        _middlewares.Add(new MiddlewareInfo(path, Path.IsParamPath(path) ,middleware));
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}