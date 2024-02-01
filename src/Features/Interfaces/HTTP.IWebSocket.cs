using System;

namespace Netly.Interfaces
{
    public partial class HTTP
    {
        internal interface IWebSocket
        {
            /// <summary>
            /// Return true if connection is opened
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            /// Client Uri
            /// </summary>
            Uri Host { get; }

            /// <summary>
            /// Event Handler
            /// </summary>
            WebSocket.IOn On { get; }

            /// <summary>
            /// Event Creator
            /// </summary>
            WebSocket.ITo To { get; }
        }
    }
}