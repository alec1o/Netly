using System;
using System.Net;

namespace Netly.Interfaces
{
    internal interface IHttpServer
    {
        bool IsOpened { get; }
        Uri Host { get; }
        IMiddleware Middleware { get; }
        IMap Map { get; }
        IOn<HttpListener> On { get; }
        IToHttpServer To { get; }
    }
}