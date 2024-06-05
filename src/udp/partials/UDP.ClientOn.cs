using System;
using System.Net.Sockets;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;
namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            private class ClientOn : IUDP.ClientOn
            {
                public EventHandler OnClose;
                public EventHandler<byte[]> OnData;
                public EventHandler<Exception> OnError;
                public EventHandler<(string name, byte[] buffer)> OnEvent;
                public EventHandler<Socket> OnModify;
                public EventHandler OnOpen;

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
                    OnClose += (@object, e) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    OnModify += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e));
                }

                public void Data(Action<byte[]> callback)
                {
                    OnData += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    OnEvent += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e.name, e.buffer));
                }
            }
        }
    }
}