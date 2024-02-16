using System;
using Netly.Core;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            internal class _IOn : IOn
            {
                public EventHandler m_onClose;
                public EventHandler<Exception> m_onError;
                public EventHandler<System.Net.Http.HttpClient> m_onModify;
                public EventHandler m_onOpen;
                public EventHandler<IRequest> m_onFetch;
                
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

                public void Modify(Action<System.Net.Http.HttpClient> callback)
                {
                    m_onModify += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Fetch(Action<IRequest> callback)
                {
                    m_onFetch += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}