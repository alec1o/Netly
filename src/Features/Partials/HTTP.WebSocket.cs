using System;
using System.Net.WebSockets;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket : IWebSocket
        {
            private readonly OnWebSocket _onWebSocket = new OnWebSocket();
            private readonly ToWebSocket _toWebSocket;
            public Uri Host => _toWebSocket.m_uri;
            public bool IsOpened => _toWebSocket.IsConnected();
            public IOnWebSocket On => _onWebSocket;
            public IToWebSocket To => _toWebSocket;


            /// <summary>
            /// Create Websocket Client Instance
            /// </summary>
            public WebSocket()
            {
                _toWebSocket = new ToWebSocket(this);
            }

            /// <summary>
            /// Create Server Side Client Instance
            /// </summary>
            /// <param name="serverSocket"></param>
            internal WebSocket(ClientWebSocket serverSocket)
            {
                _toWebSocket = new ToWebSocket(serverSocket);
            }
        }
    }
}