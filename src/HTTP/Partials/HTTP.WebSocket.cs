using System;

namespace Netly
{
    public partial class HTTP
    {
        public partial class WebSocket : IWebSocket
        {
            private readonly _On _on = new _On();
            private readonly _To _to;

            /// <summary>
            ///     Create Websocket Client Instance
            /// </summary>
            public WebSocket()
            {
                _to = new _To(this);
            }

            /// <summary>
            ///     Create Server Side Client Instance
            /// </summary>
            /// <param name="serverSocket"></param>
            /// <param name="request"></param>
            internal WebSocket(System.Net.WebSockets.WebSocket serverSocket, IRequest request)
            {
                _to = new _To(this, serverSocket, request);
            }

            public IRequest Request => _to.m_request;
            public Uri Host => _to.m_uri;
            public bool IsOpened => _to.IsConnected();
            public IOn On => _on;
            public ITo To => _to;

            internal void InitWebSocketServerSide()
            {
                _to.InitWebSocketServerSide();
            }
        }
    }
}