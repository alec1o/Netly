using System;
using System.Net.WebSockets;
using Netly.Core;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket : IWebSocket
        {
            public bool IsOpened { get; private set; }
            public Uri Host { get; private set; }
            internal readonly OnWebSocket _onWebSocket;
            internal readonly ToWebSocket _toWebSocket;

            public WebSocket()
            {
                WebSocket instance = this;
                _onWebSocket = new OnWebSocket(ref instance);
                _toWebSocket = new ToWebSocket(ref instance);
                IsOpened = false;
                Host = new Uri("https://www.example.com/");
                On = _onWebSocket;
                To = _toWebSocket;
            }


            public IOnWebSocket On { get; }
            public IToWebSocket To { get; }


            internal class OnWebSocket : IOnWebSocket
            {
                public readonly WebSocket m_socket;
                public EventHandler m_onOpen;
                public EventHandler<Exception> m_onError;
                public EventHandler m_onClose;
                public EventHandler<ClientWebSocket> m_onModify;
                public EventHandler<(byte[] buffer, bool isText)> m_onData;
                public EventHandler<(string name, byte[] buffer)> m_onEvent;
                public EventHandler<WebSocketCloseStatus> m_onCloseWithStatus;

                public OnWebSocket(ref WebSocket websocket)
                {
                    m_socket = websocket;
                }

                public void Open(Action callback)
                {
                    m_onOpen += (@object, @event) => MainThread.Add(() => callback?.Invoke());
                }

                public void Error(Action<Exception> callback)
                {
                    m_onError += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    m_onClose += (@object, @event) => MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<ClientWebSocket> callback)
                {
                    m_onModify += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Data(Action<byte[], bool> callback)
                {
                    m_onData += (@object, @event) =>
                        MainThread.Add(() => callback?.Invoke(@event.buffer, @event.isText));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    m_onEvent += (@object, @event) =>
                        MainThread.Add(() => callback?.Invoke(@event.name, @event.buffer));
                }

                public void Close(Action<WebSocketCloseStatus> callback)
                {
                    m_onCloseWithStatus += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }

            internal class ToWebSocket : IToWebSocket
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