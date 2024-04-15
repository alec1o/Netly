using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            internal class _On : IOn
            {
                public EventHandler m_onClose;
                public EventHandler<Exception> m_onError;
                public EventHandler<Socket> m_onModify;
                public EventHandler m_onOpen;
                public EventHandler<byte[]> m_onData;
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
                    m_onClose += (@object, e) => MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    m_onModify += (@object, e) => MainThread.Add(() => callback?.Invoke(e));
                }

                public void Data(Action<byte[]> callback)
                {
                    m_onData += (@object, e) => MainThread.Add(() => callback?.Invoke(e));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    m_onEvent += (@object, e) => MainThread.Add(() => callback?.Invoke(e.name, e.buffer));
                }
            }
        }
    }
}