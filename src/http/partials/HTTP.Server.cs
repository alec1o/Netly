using System;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server : IHTTP.Server
        {
            private readonly Map _map;
            private readonly Middleware _middleware;
            private readonly ServerOn _serverOn;
            private readonly ServerTo _serverTo;

            public Server()
            {
                _serverOn = new ServerOn();
                _map = new Map(this);
                _middleware = new Middleware(this);
                _serverTo = new ServerTo(this);
            }

            public bool IsOpened => _serverTo.IsOpened;
            public Uri Host => _serverTo.Host;
            public IHTTP.Map Map => _map;
            public IHTTP.Middleware Middleware => _middleware;
            public IHTTP.ServerOn On => _serverOn;
            public IHTTP.ServerTo To => _serverTo;
        }
    }
}