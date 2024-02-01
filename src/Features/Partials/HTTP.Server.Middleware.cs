using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _Middleware : Interfaces.HTTP.Server.IMiddleware
            {
                public readonly HTTP.Server m_server;
                public _Middleware(HTTP.Server server)
                {
                    this.m_server = server;
                }
                
                public Dictionary<string, Func<Interfaces.HTTP.IRequest, Interfaces.HTTP.IResponse, bool>>[] Middlewares { get; }

                public bool Add(Func<Interfaces.HTTP.IRequest, Interfaces.HTTP.IResponse, bool> middleware)
                {
                    throw new NotImplementedException();
                }

                public bool Add(string path, Func<Interfaces.HTTP.IRequest, Interfaces.HTTP.IResponse, bool> middleware)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}