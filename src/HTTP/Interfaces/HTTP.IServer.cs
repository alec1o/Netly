using System;

namespace Netly
{
    public static partial class HTTP
    {
        internal interface IServer
        {
            bool IsOpened { get; }
            Uri Host { get; }
            Server.IMiddleware Middleware { get; }
            Server.IMap Map { get; }
            Server.IOn On { get; }
            Server.ITo To { get; }
        }
    }
}