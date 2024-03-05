using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server
        {
            internal class _On : IOn
            {
                public EventHandler m_onClose;
                public EventHandler<Exception> m_onError;
                public EventHandler<Socket> m_onModify;
                public EventHandler m_onOpen;
                public EventHandler<IClient> m_onAccept;
                
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

                public void Modify(Action<Socket> callback)
                {
                    m_onModify += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Accept(Action<IClient> callback)
                {
                    m_onAccept += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}