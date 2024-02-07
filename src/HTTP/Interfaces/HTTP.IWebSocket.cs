using System;

namespace Netly
{
    public static partial class HTTP
    {
        internal interface IWebSocket
        {
            /// <summary>
            ///     Connection request
            /// </summary>
            IRequest Request { get; }

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
            WebSocket.IOn On { get; }

            /// <summary>
            ///     Event Creator
            /// </summary>
            WebSocket.ITo To { get; }
        }
    }
}