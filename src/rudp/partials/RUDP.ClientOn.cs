using System;
using System.IO;
using System.Net.Sockets;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        internal class ClientOn : IRUDP.ClientOn
        {
            public EventHandler OnClose;
            public EventHandler<(Stream stream, MessageType messageType)> OnData;
            public EventHandler<Exception> OnError;
            public EventHandler<(string name, Stream stream, MessageType messageType)> OnEvent;
            public EventHandler<Socket> OnModify;
            public EventHandler OnOpen;

            public void Open(Action callback)
            {
                OnOpen += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke());
            }

            public void Error(Action<Exception> callback)
            {
                OnError += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke(@event));
            }

            public void Close(Action callback)
            {
                OnClose += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke());
            }

            public void Modify(Action<Socket> callback)
            {
                OnModify += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke(e));
            }

            public void Data(Action<Stream, MessageType> callback)
            {
                OnData += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke(e.stream, e.messageType));
            }

            public void Event(Action<string, Stream, MessageType> callback)
            {
                OnEvent += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke(e.name, e.stream, e.messageType));
            }
        }
    }
}