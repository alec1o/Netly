using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client
        {
            internal class ClientOn : ITCP.ClientOn
            {
                public readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> OnEncryption;
                public EventHandler OnClose;
                public EventHandler<byte[]> OnData;
                public EventHandler<Exception> OnError;
                public EventHandler<(string name, byte[] buffer)> OnEvent;
                public EventHandler<Socket> OnModify;
                public EventHandler OnOpen;

                public ClientOn()
                {
                    OnEncryption = new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();
                }


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

                public void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
                {
                    if (callback != null) OnEncryption.Add(callback);
                }
            }
        }
    }
}