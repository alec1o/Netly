using System;
using System.Net;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server : Interfaces.IHttpServer
        {
            internal readonly OnHttpServer m_on;
            internal readonly ToHttpServer m_to;
            internal readonly HttpMap m_map;
            internal readonly HttpMiddleware m_middleware;

            public bool IsOpened => m_to.m_isOpened;
            public Uri Host => m_to.m_uri;
            public IMap Map => m_map;
            public IMiddleware Middleware => m_middleware;
            public IOn<HttpListener> On => m_on;
            public IToHttpServer To => m_to;

            public Server()
            {
                m_on = new OnHttpServer();
                m_map = new HttpMap(this);
                m_middleware = new HttpMiddleware(this);
                m_to = new ToHttpServer(this);
            }
        }
    }
}