using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        internal class ClientOn : ITCP.ClientOn
        {
            public readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> OnEncryption =
                new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();

            public EventHandler OnClose;
            public EventHandler<Stream> OnData;
            public EventHandler<Exception> OnError;
            public EventHandler<(string name, Stream buffer)> OnEvent;
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

            public void Data(Action<Stream> callback)
            {
                OnData += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke(e));
            }

            public void Event(Action<string, Stream> callback)
            {
                OnEvent += (@object, e) => NDispatcher.Singleton.Submit(() => callback?.Invoke(e.name, e.buffer));
            }

            public void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
            {
                if (callback != null) OnEncryption.Add(callback);
            }
        }
    }
}