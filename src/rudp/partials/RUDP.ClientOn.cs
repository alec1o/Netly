using System;
using System.Net.Sockets;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientOn : IRUDP.ClientOn
            {
                public EventHandler OnClose;
                public EventHandler<(byte[] data, MessageType messageType)> OnData;
                public EventHandler<Exception> OnError;
                public EventHandler<(string name, byte[] buffer, MessageType messageType)> OnEvent;
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

                public void Data(Action<byte[], MessageType> callback) 
                {
                    OnData += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e.data, e.messageType));
                }

                public void Event(Action<string, byte[], MessageType> callback)
                {
                    OnEvent += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e.name, e.buffer, e.messageType));
                }
            }
        }
    }
}