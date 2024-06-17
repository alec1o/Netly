using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class Middleware : IHTTP.Middleware
        {
            public const string GLOBAL_PATH = "*";

            private readonly List<IHTTP.MiddlewareDescriptor> _middlewares;
            public readonly Server m_server;

            public Middleware(Server server)
            {
                m_server = server;
                _middlewares = new List<IHTTP.MiddlewareDescriptor>();
            }

            public IHTTP.MiddlewareDescriptor[] Middlewares => _middlewares.ToArray();

            public bool Add(Func<IHTTP.Request, IHTTP.Response, bool> middleware)
            {
                return Add(GLOBAL_PATH, middleware);
            }

            public bool Add(string path, Func<IHTTP.Request, IHTTP.Response, bool> middleware)
            {
                if (middleware == null) return false;

                path = (path ?? string.Empty).Trim();

                Path.AddEndOfPath(ref path);

                if (string.IsNullOrWhiteSpace(path)) return false;

                if (GLOBAL_PATH.Equals(path) || Path.IsValid(path))
                {
                    _middlewares.Add(new MiddlewareDescriptor(path, Path.IsParamPath(path), middleware));
                    return true;
                }

                return false;
            }
        }
    }
}