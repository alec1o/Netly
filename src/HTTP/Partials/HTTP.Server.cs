using System;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server : IServer
        {
            private readonly _Map _map;
            private readonly _Middleware _middleware;
            private readonly _On _on;
            private readonly _To _to;

            public Server()
            {
                _on = new _On();
                _map = new _Map(this);
                _middleware = new _Middleware(this);
                _to = new _To(this);
            }

            public bool IsOpened => _to.IsOpened;
            public Uri Host => _to.Host;
            public IMap Map => _map;
            public IMiddleware Middleware => _middleware;
            public IOn On => _on;
            public ITo To => _to;
        }
    }
}