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
                public readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> m_onEncryption;
                public EventHandler m_onClose;
                public EventHandler<byte[]> m_onData;
                public EventHandler<Exception> m_onError;
                public EventHandler<(string name, byte[] buffer)> m_onEvent;
                public EventHandler<Socket> m_onModify;
                public EventHandler m_onOpen;

                public ClientOn()
                {
                    m_onEncryption = new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();
                }


                public void Open(Action callback)
                {
                    m_onOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Error(Action<Exception> callback)
                {
                    m_onError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
                }

                public void Close(Action callback)
                {
                    m_onClose += (@object, e) => Env.MainThread.Add(() => callback?.Invoke());
                }

                public void Modify(Action<Socket> callback)
                {
                    m_onModify += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e));
                }

                public void Data(Action<byte[]> callback)
                {
                    m_onData += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e));
                }

                public void Event(Action<string, byte[]> callback)
                {
                    m_onEvent += (@object, e) => Env.MainThread.Add(() => callback?.Invoke(e.name, e.buffer));
                }

                public void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
                {
                    if (callback != null) m_onEncryption.Add(callback);
                }
            }
        }
    }
}