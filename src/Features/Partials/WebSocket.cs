using System;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket : IWebSocket
        {
            public bool IsOpened { get; private set; }
            public Uri Host { get; private set; }

            public WebSocket()
            {
                WebSocket instance = this;
                
                IsOpened = false;
                Host = new Uri("https://www.example.com/");
                On = new OnWebSocket(ref instance);
                To = new ToWebSocket(ref instance);
            }


            public IOnWebSocket On { get; }
            public IToWebSocket To { get; }
            
            
            private class OnWebSocket : IOnWebSocket
            {
                public readonly WebSocket m_socket;
                
                public OnWebSocket(ref WebSocket websocket)
                {
                    m_socket = websocket;
                }
                
                public void Open(Action callback)
                {
                    throw new NotImplementedException();
                }

                public void Error(Action<Exception> callback)
                {
                    throw new NotImplementedException();
                }

                public void Close(Action callback)
                {
                    throw new NotImplementedException();
                }

                public void Modify(Action<ClientWebSocket> callback)
                {
                    throw new NotImplementedException();
                }

                public void Data(Action<byte[], bool> callback)
                {
                    throw new NotImplementedException();
                }

                public void Event(Action<string, byte[], bool> callback)
                {
                    throw new NotImplementedException();
                }

                public void Close(Action<WebSocketCloseStatus> callback)
                {
                    throw new NotImplementedException();
                }
            }
            
            private class ToWebSocket : IToWebSocket
            {
                public readonly WebSocket m_socket;

                public ToWebSocket(ref WebSocket websocket)
                {
                    m_socket = websocket;
                }
                
                public void Open(Uri host)
                {
                    throw new NotImplementedException();
                }

                public void Close()
                {
                    throw new NotImplementedException();
                }

                public void Data(byte[] buffer, bool isText)
                {
                    throw new NotImplementedException();
                }

                public void Data(string buffer, bool isText)
                {
                    throw new NotImplementedException();
                }

                public void Event(string name, byte[] buffer)
                {
                    throw new NotImplementedException();
                }

                public void Event(string name, string buffer)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}