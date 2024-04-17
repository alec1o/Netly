using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server
        {
            internal class ServerOn : Interfaces.UDP.IServerOn
            {
                public EventHandler<Interfaces.UDP.IClient> OnAccept;
                public EventHandler OnClose;
                public EventHandler<Exception> OnError;
                public EventHandler<Socket> OnModify;
                public EventHandler OnOpen;

                public void Open(Action callback)
                {
                    OnOpen += (@object, @event) => MainThread.Add(() => callback?.Invoke());
                }

                public void Error(Action<Exception> callback)
                {
                    OnError += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    OnClose += (@object, @event) => MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    OnModify += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Accept(Action<Interfaces.UDP.IClient> callback)
                {
                    OnAccept += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}