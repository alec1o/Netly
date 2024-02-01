using System;
using System.Net.WebSockets;
using Netly.Core;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket
        {
            internal class OnWebSocket : IOnWebSocket
            {
                public EventHandler m_onOpen;
                public EventHandler<Exception> m_onError;
                public EventHandler<WebSocketCloseStatus> m_onClose;
                public EventHandler<ClientWebSocket> m_onModify;
                public EventHandler<(byte[] buffer, bool isText)> m_onData;
                public EventHandler<(string name, byte[] buffer)> m_onEvent;
                

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
                    m_onClose += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}