using System;
using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        public partial class WebSocket : IHTTP.WebSocket
        {
            internal readonly WebsocketOn _on = new WebsocketOn();
            private readonly WebsocketTo _to;

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

            public IHTTP.Request Request => _to.MyRequest;
            public Dictionary<string, string> Headers => _to.Headers;
            public Uri Host => _to.MyUri;
            public bool IsOpened => _to.IsConnected();
            public IHTTP.WebSocketOn On => _on;
            public IHTTP.WebSocketTo To => _to;

            internal void InitWebSocketServerSide()
            {
                _to.InitWebSocketServerSide();
            }
        }
    }
}