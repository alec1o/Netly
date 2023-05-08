using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Netly.Abstract
{
    public abstract class Server<T>
    {
        #region Props

        public Host Host { get; protected set; }
        public List<T> Clients { get; protected set; } = new List<T>();
        public bool IsOpened => IsConnected();

        protected Socket m_socket;
        protected bool m_connecting;
        protected bool m_closing;
        protected bool m_closed;
        protected bool m_opened;

        protected private EventHandler onOpenHandler;
        protected private EventHandler<Exception> onErrorHandler;
        protected private EventHandler onCloseHandler;
        protected private EventHandler<T> onEnterHandler;
        protected private EventHandler<T> onExitHandler;
        protected private EventHandler<(T client, byte[] buffer)> onDataHandler;
        protected private EventHandler<(T client, string name, byte[] buffer)> onEventHandler;
        protected private EventHandler<Socket> onModifyHandler;
        private object destroyLock = new object();
        protected readonly object m_lock = new object();

        #endregion

        protected virtual bool IsConnected()
        {
            return m_socket == null;
        }

        public virtual void Open(Host host)
        {
            Open(host, 0);
        }

        public virtual void Open(Host host, int backlogOrTimeout)
        {
            // override...
        }

        protected virtual void AcceptOrReceive()
        {
            // override...
        }

        protected virtual void Destroy()
        {
            lock (destroyLock)
            {
                m_closing = true;

                if (m_closed is false)
                {
                    m_socket?.Close();
                    onCloseHandler?.Invoke(null, null);
                }

                m_closed = true;
                m_socket = null;
                m_closing = false;
            }
        }

        protected virtual T AddOrRemoveClient(T client, bool removeClient)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            lock (m_lock)
            {
                if (removeClient is false)
                {
                    Clients.Add(client);
                }
                else
                {
                    foreach (T currentUser in Clients)
                    {
                        if (client.Equals(currentUser))
                        {
                            Clients.Remove(currentUser);
                            return client;
                        }
                    }
                }

                return client;
            }
        }

        public virtual void Close()
        {
            if (!IsOpened || m_connecting || m_closing) return;

            m_closing = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Destroy();

                if (m_closed) m_opened = false;

                T[] clients = Clients.ToArray();
                Clients.Clear();

                foreach (T client in clients)
                {
                    object m_object = client;
                    IClient m_client = (IClient)client;
                    m_client?.Close();
                }
            });
        }

        public virtual void ToData(byte[] data)
        {
            if (m_closing || m_closed) return;

            foreach (T client in Clients)
            {
                object m_object = client;
                IClient m_client = (IClient)client;
                m_client?.ToData(data);
            }
        }

        public virtual void ToData(string data)
        {
            ToData(NE.GetBytes(data));
        }

        public virtual void ToEvent(string name, byte[] data)
        {
            if (m_closing || m_closed) return;

            foreach (T client in Clients)
            {

                object m_object = client;
                IClient m_client = (IClient)client;
                m_client?.ToEvent(name, data);

            }
        }

        public virtual void ToEvent(string name, string data)
        {
            ToEvent(name, NE.GetBytes(data));
        }

        #region Callbacks

        public virtual void OnError(Action<Exception> callback)
        {
            onErrorHandler += (_, exception) =>
            {
                MainThread.Add(() => callback?.Invoke(exception));
            };
        }

        public virtual void OnOpen(Action callback)
        {
            onOpenHandler += (_, @null) =>
            {
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public virtual void OnClose(Action callback)
        {
            onCloseHandler += (_, @null) =>
            {
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public virtual void OnEnter(Action<T> callback)
        {
            onEnterHandler += (_, client) =>
            {
                MainThread.Add(() => callback?.Invoke(client));
            };
        }

        public virtual void OnExit(Action<T> callback)
        {
            onExitHandler += (_, client) =>
            {
                MainThread.Add(() => callback?.Invoke(client));
            };
        }

        public virtual void OnData(Action<T, byte[]> callback)
        {
            onDataHandler += (_, data) =>
            {
                MainThread.Add(() => callback?.Invoke(data.client, data.buffer));
            };
        }

        public virtual void OnEvent(Action<T, string, byte[]> callback)
        {
            onEventHandler += (_, data) =>
            {
                MainThread.Add(() => callback?.Invoke(data.client, data.name, data.buffer));
            };
        }

        public virtual void OnModify(Action<Socket> callback)
        {
            onModifyHandler += (_, socket) =>
            {
                MainThread.Add(() => callback?.Invoke(socket));
            };
        }

        #endregion

    }
}
