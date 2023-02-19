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
