using System;
using System.Net.WebSockets;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket : Interfaces.HTTP.IWebSocket
        {
            private readonly _On _on = new _On();
            private readonly _To _to;
            public Uri Host => _to.m_uri;
            public bool IsOpened => _to.IsConnected();
            public Interfaces.HTTP.WebSocket.IOn On => _on;
            public Interfaces.HTTP.WebSocket.ITo To => _to;

            /// <summary>
            /// Create Websocket Client Instance
            /// </summary>
            public WebSocket()
            {
                _to = new _To(this);
            }

            /// <summary>
            /// Create Server Side Client Instance
            /// </summary>
            /// <param name="serverSocket"></param>
            internal WebSocket(ClientWebSocket serverSocket)
            {
                _to = new _To(serverSocket);
            }
        }
    }
}