using System;
using System.Net.Sockets;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Server
        {
            private class ServerOn : IRUDP.ServerOn
            {
                public EventHandler<IRUDP.Client> OnAccept;
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

                public void Accept(Action<IRUDP.Client> callback)
                {
                    OnAccept += (@object, @event) => MainThread.Add(() => callback?.Invoke(@event));
                }
            }
        }
    }
}