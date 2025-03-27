using System;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        internal interface Server
        {
            bool IsOpened { get; }
            Uri Host { get; }
            Middleware Middleware { get; }
            Map Map { get; }
            ServerOn On { get; }
            ServerTo To { get; }
        }
    }
}