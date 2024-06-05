using System;
using System.Net.WebSockets;
using Env = Netly.NetlyEnvironment;
namespace Netly
{
    public partial class HTTP
    {
        public partial class WebSocket
        {
            private class _On : IOn
            {
                public EventHandler<WebSocketCloseStatus> m_onClose;
                public EventHandler<(byte[] buffer, bool isText)> m_onData;
                public EventHandler<Exception> m_onError;
                public EventHandler<(string name, byte[] buffer)> m_onEvent;
                public EventHandler<ClientWebSocket> m_onModify;
                public EventHandler m_onOpen;

                public void Open(Action callback)
                {
                    m_onOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Error(Action<Exception> callback)
                {
                    m_onError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    m_onClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<ClientWebSocket> callback)
                {
                    m_onModify += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Data(Action<byte[], bool> callback)
                {
                    m_onData += (@object, @event) =>
                        Env.MainThread.Add(() => callback?.Invoke(@event.buffer, @event.isText));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    m_onEvent += (@object, @event) =>
                        Env.MainThread.Add(() => callback?.Invoke(@event.name, @event.buffer));
                }

                public void Close(Action<WebSocketCloseStatus> callback)
                {
                    m_onClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}