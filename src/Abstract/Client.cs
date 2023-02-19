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
    }
}
