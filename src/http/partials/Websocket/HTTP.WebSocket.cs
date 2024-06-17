using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        public partial class WebSocket : IHTTP.WebSocket
        {
            internal readonly WebsocketOn _websocketOn = new WebsocketOn();
            internal readonly WebsocketTo _to;

            /// <summary>
            ///     Create Websocket Client Instance
            /// </summary>
            public WebSocket()
            {
                _to = new WebsocketTo(this);
            }

            /// <summary>
            ///     Create Server Side Client Instance
            /// </summary>
            /// <param name="serverSocket"></param>
            /// <param name="request"></param>
            internal WebSocket(System.Net.WebSockets.WebSocket serverSocket, IHTTP.Request request)
            {
                _to = new WebsocketTo(this, serverSocket, request);
            }

            public IHTTP.Request Request => _to.m_request;
            public Dictionary<string, string> Headers => _to.m_headers;
            public Uri Host => _to.m_uri;
            public bool IsOpened => _to.IsConnected();
            public IHTTP.WebSocketOn On => _websocketOn;
            public IHTTP.WebSocketTo To => _to;

            internal void InitWebSocketServerSide()
            {
                _to.InitWebSocketServerSide();
            }
        }
    }
}