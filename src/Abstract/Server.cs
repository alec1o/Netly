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
        protected bool m_tryOpen;
        protected bool m_tryClose;
        protected bool m_invokeClose;
        protected bool m_opened;

        protected private EventHandler onOpenHandler;
        protected private EventHandler<Exception> onErrorHandler;
        protected private EventHandler onCloseHandler;
        protected private EventHandler<T> onEnterHandler;
        protected private EventHandler<T> onExitHandler;
        protected private EventHandler<(T client, byte[] buffer)> onDataHandler;
        protected private EventHandler<(T client, string name, byte[] buffer)> onEventHandler;
        protected private EventHandler<Socket> onModifyHandler;
        protected readonly object m_lock = new object();

        #endregion

        protected virtual bool IsConnected()
        {
        }

        public virtual void Open(Host host)
        {
        }
        public virtual void Open(Host host, int backlog)
        {
        }
        public virtual void AcceptOrReceive()
        {
        }
        protected virtual T AddOrRemoveClient(T client, bool removeClient)
        {
        public virtual void Close()
        {
        public virtual void ToData(byte[] data)
        {
        public virtual void ToData(string data)
        {
        }
        public virtual void ToEvent(string name, byte[] data)
        {
        }
        public virtual void ToEvent(string name, string data)
        {
        }
        #region Callbacks

        public virtual void OnError(Action<Exception> callback)
        {
        }

        public virtual void OnOpen(Action callback)
        {
        }

        public virtual void OnClose(Action callback)
        {
        }

        public virtual void OnEnter(Action<T> callback)
        {
        }

        public virtual void OnExit(Action<T> callback)
        {
        }

        public virtual void OnData(Action<T, byte[]> callback)
        {
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
