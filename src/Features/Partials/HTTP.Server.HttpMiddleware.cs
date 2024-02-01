using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class HttpMiddleware : IMiddleware
            {
                public readonly HTTP.Server m_server;
                public HttpMiddleware(HTTP.Server server)
                {
                    this.m_server = server;
                }
                
                public Dictionary<string, Func<IRequest, IResponse, bool>>[] Middlewares { get; }

                public bool Add(Func<IRequest, IResponse, bool> middleware)
                {
                    throw new NotImplementedException();
                }

                public bool Add(string path, Func<IRequest, IResponse, bool> middleware)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}