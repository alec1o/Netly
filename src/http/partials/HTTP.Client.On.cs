using System;
using System.Net.Http;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            internal class ClientOn : IHTTP.ClientOn
            {
                public EventHandler OnClose { get; private set; } = delegate { };
                public EventHandler<Exception> OnError { get; private set; } = delegate { };
                public EventHandler<HttpClient> OnModify { get; private set; } = delegate { };
                public EventHandler<IResponse> OnOpen { get; private set; } = delegate { };

                public void Error(Action<Exception> callback)
                {
                    OnError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    OnClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<HttpClient> callback)
                {
                    OnModify += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Open(Action<IResponse> callback)
                {
                    OnOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}