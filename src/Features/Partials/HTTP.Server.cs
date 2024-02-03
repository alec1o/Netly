using System;
using System.Net;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server : Interfaces.HTTP.IServer
        {
            private readonly _On _on;
            private readonly _To _to;
            private readonly _Map _map;
            private readonly _Middleware _middleware;

            public bool IsOpened => _to.IsOpened;
            public Uri Host => _to.Host;
            public Interfaces.HTTP.Server.IMap Map => _map;
            public Interfaces.HTTP.Server.IMiddleware Middleware => _middleware;
            public Interfaces.HTTP.Server.IOn On => _on;
            public Interfaces.HTTP.Server.ITo To => _to;

            public Server()
            {
                _on = new _On();
                _map = new _Map(this);
                _middleware = new _Middleware(this);
                _to = new _To(this);
            }
        }
    }
}