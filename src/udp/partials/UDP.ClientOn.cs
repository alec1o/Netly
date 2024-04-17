using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            internal class ClientOn : Interfaces.UDP.IClientOn
            {
                public EventHandler OnClose;
                public EventHandler<byte[]> OnData;
                public EventHandler<Exception> OnError;
                public EventHandler<(string name, byte[] buffer)> OnEvent;
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
                    OnClose += (@object, e) => MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    OnModify += (@object, e) => MainThread.Add(() => callback?.Invoke(e));
                }

                public void Data(Action<byte[]> callback)
                {
                    OnData += (@object, e) => MainThread.Add(() => callback?.Invoke(e));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    OnEvent += (@object, e) => MainThread.Add(() => callback?.Invoke(e.name, e.buffer));
                }
            }
        }
    }
}