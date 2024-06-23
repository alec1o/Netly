using System;
using System.Collections.Generic;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface WebSocket
        {
            /// <summary>
            ///     Connection request
            /// </summary>
            ServerRequest ServerRequest { get; }

            /// <summary>
            ///     Connection Headers
            /// </summary>
            Dictionary<string, string> Headers { get; }

            /// <summary>
            ///     Return true if connection is opened
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            ///     Client Uri
            /// </summary>
            Uri Host { get; }

            /// <summary>
            ///     Event Handler
            /// </summary>
            WebSocketOn On { get; }

            /// <summary>
            ///     Event Creator
            /// </summary>
            WebSocketTo To { get; }
        }
    }
}