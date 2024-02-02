using System;
using System.Collections.Generic;
using IRequest = Netly.Interfaces.HTTP.IRequest;
using IResponse = Netly.Interfaces.HTTP.IResponse;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Middleware : Interfaces.HTTP.Server.IMiddleware
            {
                public readonly HTTP.Server m_server;

                private readonly List<Dictionary<string, Func<IRequest, IResponse, bool>>> _middlewareList;

                public _Middleware(HTTP.Server server)
                {
                    this.m_server = server;
                    _middlewareList = new List<Dictionary<string, Func<IRequest, IResponse, bool>>>();
                }

                public Dictionary<string, Func<IRequest, IResponse, bool>>[] Middlewares => _middlewareList.ToArray();

                public bool Add(Func<IRequest, IResponse, bool> middleware)
                {
                    if (middleware == null) return false;
                    var item = new Dictionary<string, Func<IRequest, IResponse, bool>> { { "*", middleware } };
                    _middlewareList.Add(item);
                    return true;
                }

                public bool Add(string path, Func<IRequest, IResponse, bool> middleware)
                {
                    path = (path ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(path)) return false;

                    if (Path.IsValid(path))
                    {
                        var item = new Dictionary<string, Func<IRequest, IResponse, bool>> { { "*", middleware } };
                        _middlewareList.Add(item);
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}