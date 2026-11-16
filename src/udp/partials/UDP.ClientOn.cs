using System;
using System.IO;
using System.Net.Sockets;
using Netly.Interfaces;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            private class ClientOn : IUDP.ClientOn
            {
                public EventHandler OnClose;
                public EventHandler<Stream> OnData;
                public EventHandler<Exception> OnError;
                public EventHandler<(string name, Stream stream)> OnEvent;
                public EventHandler<Socket> OnModify;
                public EventHandler OnOpen;

                public void Open(Action callback)
                {
                    OnOpen += (@object, @event) => Dispatcher.Singleton.Submit(() => callback?.Invoke());
                }

                public void Error(Action<Exception> callback)
                {
                    OnError += (@object, @event) => Dispatcher.Singleton.Submit(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    OnClose += (@object, e) => Dispatcher.Singleton.Submit(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    OnModify += (@object, e) => Dispatcher.Singleton.Submit(() => callback?.Invoke(e));
                }

                public void Data(Action<Stream> callback)
                {
                    OnData += (@object, e) => Dispatcher.Singleton.Submit(() => callback?.Invoke(e));
                }

                public void Event(Action<string, Stream> callback)
                {
                    OnEvent += (@object, e) => Dispatcher.Singleton.Submit(() => callback?.Invoke(e.name, e.stream));
                }
            }
        }
    }
}