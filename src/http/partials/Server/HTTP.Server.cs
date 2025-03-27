using System;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server : IHTTP.Server
        {
            internal readonly Map MyMap;
            internal readonly Middleware MyMiddleware;
            internal readonly ServerOn MyServerOn;
            internal readonly ServerTo MyServerTo;

            public Server()
            {
                MyServerOn = new ServerOn();
                MyMap = new Map(this);
                MyMiddleware = new Middleware(this);
                MyServerTo = new ServerTo(this);
            }

            public bool IsOpened => MyServerTo.IsOpened;
            public Uri Host => MyServerTo.Host;
            public IHTTP.Map Map => MyMap;
            public IHTTP.Middleware Middleware => MyMiddleware;
            public IHTTP.ServerOn On => MyServerOn;
            public IHTTP.ServerTo To => MyServerTo;
        }
    }
}