using System;
using System.Net.Http;
using Env = Netly.NetlyEnvironment;

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
                public EventHandler<HttpClient> m_onModify;
                public EventHandler<IResponse> m_onOpen;

                public void Error(Action<Exception> callback)
                {
                    m_onError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    m_onClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<HttpClient> callback)
                {
                    m_onModify += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Open(Action<IResponse> callback)
                {
                    m_onOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}