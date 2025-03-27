using System;
using System.Net;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;

namespace Netly
{
    public partial class HTTP
    {
        internal class ServerOn : IHTTP.ServerOn
        {
            public EventHandler OnClose { get; private set; }
            public EventHandler<Exception> OnError { get; private set; }
            public EventHandler<HttpListener> OnModify { get; private set; }
            public EventHandler OnOpen { get; private set; }

            public void Open(Action callback)
            {
                OnOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
            }

            public void Error(Action<Exception> callback)
            {
                OnError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
            }

            public void Close(Action callback)
            {
                OnClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
            }

            public void Modify(Action<HttpListener> callback)
            {
                OnModify += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
            }
        }
    }
}