using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Netly.Abstract
{
    public abstract class Client : IClient
    {
        #region Props

        public string UUID { get; protected set; }
        public Host Host { get; protected set; }

        public bool IsOpened => IsConnected();

        protected Socket m_socket;
        protected bool m_closed;
        protected bool m_closing;
        protected bool m_connecting;
        protected bool m_serverMode;

        protected private EventHandler onOpenHandler;
        protected private EventHandler<Exception> onErrorHandler;
        protected private EventHandler onCloseHandler;
        protected private EventHandler<byte[]> onDataHandler;
        protected private EventHandler<(string name, byte[] buffer)> onEventHandler;
        protected private EventHandler<Socket> onModifyHandler;

        #endregion
        protected virtual bool IsConnected()
        {
        }
        public virtual void Open(Host host)
        {
        }
        public virtual void Receive()
        {
        }
        public virtual void Close()
        {
        }
        public virtual void ToData(byte[] data)
        {
        }
        public virtual void ToData(string data)
        {
        }
        public virtual void ToEvent(string name, byte[] data)
        {
        }
        public virtual void ToEvent(string name, string data)
        {
        }
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

        public virtual void OnData(Action<byte[]> callback)
        {
            onDataHandler += (_, buffer) =>
            {
                MainThread.Add(() => callback?.Invoke(buffer));
            };
        }

        public virtual void OnEvent(Action<string, byte[]> callback)
        {
            onEventHandler += (_, data) =>
            {
                MainThread.Add(() => callback?.Invoke(data.name, data.buffer));
            };
        }

        public virtual void OnModify(Action<Socket> callback)
        {
            onModifyHandler += (_, socket) =>
            {
                MainThread.Add(() => callback?.Invoke(socket));
            };
        }
    }
}
