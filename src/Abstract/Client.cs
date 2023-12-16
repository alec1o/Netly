using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Netly.Abstract
{
    public abstract class Client : IClient
    {
        #region Props

        public bool Framing { get; protected set; }
        public string UUID { get; protected set; }
        public Host Host { get; protected set; } = Host.Default;

        public bool IsOpened => IsConnected();

        protected Socket m_socket;
        protected NetworkStream m_stream;
        protected SslStream m_sslStream;
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
        private readonly object destroyLock = new object();

        #endregion

        protected virtual bool IsConnected()
        {
            return m_socket == null;
        }

        public virtual void Open(Host host)
        {
            // override...
        }

        internal virtual void InitServer()
        {
            m_serverMode = true;
            onOpenHandler?.Invoke(null, null);
            Receive();
        }

        protected virtual void Receive()
        {
            // override...
        }

        public virtual void Close()
        {
            if (!IsOpened || m_connecting || m_closing) return;
            m_closing = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Destroy();
            });
        }

        public virtual void ToData(byte[] data)
        {
            if (m_closing || m_closed) return;

            byte[] buffer = (Framing) ? MessageFraming.CreateMessage(data) : data;

            m_socket?.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public virtual void ToData(string data)
        {
            ToData(NE.GetBytes(data));
        }

        public virtual void ToEvent(string name, byte[] data)
        {
            ToData(EventManager.Create(name, data));
        }

        public virtual void ToEvent(string name, string data)
        {
            ToEvent(name, NE.GetBytes((data)));
        }

        protected virtual void Destroy()
        {
            lock (destroyLock)
            {
                m_closing = true;

                if (m_closed is false)
                {
                    m_socket?.Close();
                    m_stream?.Close();
                    m_sslStream?.Close();
                    onCloseHandler?.Invoke(null, null);
                }

                m_closed = true;
                m_socket = null;
                m_closing = false;
            }
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

        #endregion
    }
}
