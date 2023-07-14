using Netly.Abstract;
using Netly.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Netly
{
    public class UdpClient : Client
    {
        private bool _opened;
        private bool useTimeoutConnection;
        private DateTime connectionTimer;
        public const int MIN_TIMEOUT = 500;

        public int Timeout { get; internal set; }
        public UdpClient() { }

        internal UdpClient(string uuid, ref Socket socket, Host host)
        {
            UUID = uuid;
            m_socket = socket;
            Host = host;
            m_serverMode = true;
        }

        public void Open(Host host, int timeout)
        {
            if (timeout < MIN_TIMEOUT) throw new ArgumentException("TIMEOUT is very low");
            Timeout = timeout;
            Open(host);
        }

        internal override void InitServer()
        {
            _opened = true;
            onOpenHandler?.Invoke(this, null);
        }

        public override void Open(Host host)
        {
            if (IsOpened || m_connecting || m_closing || m_serverMode) return;

            m_connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Connect(host.EndPoint);

                    Host = new Host(m_socket.RemoteEndPoint);

                    _opened = true;

                    m_closed = false;

                    onOpenHandler?.Invoke(this, null);

                    Receive();
                }
                catch (Exception e)
                {
                    onErrorHandler?.Invoke(null, e);
                }

                m_connecting = false;
            });
        }

        protected override bool IsConnected()
        {
            if (m_socket == null) return false;
            return _opened;
        }

        public override void ToData(byte[] data)
        {
            if (data == null || m_closed || m_closing) return;
            m_socket?.SendTo(data, SocketFlags.None, Host.EndPoint);
        }

        protected override void Receive()
        {
            int length = 0;
            byte[] buffer = new byte[1024 * 32];
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                // init timer
                UpdateTimeoutConnection();

                while (IsOpened)
                {
                    try
                    {
                        length = m_socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        if (length <= 0)
                        {
                            if (IsOpened) continue;
                            break;
                        }

                        byte[] data = new byte[length];

                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                        (string name, byte[] buffer) content = MessageParser.Verify(data);

                        // update timer
                        UpdateTimeoutConnection();

                        if (content.buffer == null)
                            onDataHandler?.Invoke(null, data);
                        else
                            onEventHandler?.Invoke(null, (content.name, content.buffer));
                    }
                    catch
                    {
                        if (IsOpened) continue;
                        break;
                    }
                }

                Destroy();
            });
        }

        public override void OnClose(Action callback)
        {
            Timeout = 0;
            base.OnClose(callback);
        }

        internal void AddData(byte[] data)
        {
            UpdateTimeoutConnection();

            (string name, byte[] buffer) _content = MessageParser.Verify(data);

            if (_content.buffer == null)
                onDataHandler?.Invoke(null, data);
            else
                onEventHandler?.Invoke(null, (_content.name, _content.buffer));
        }

        private void UpdateTimeoutConnection()
        {
            if (Timeout == 0) return;

            connectionTimer = DateTime.Now.AddMilliseconds(Timeout);

            if (useTimeoutConnection is false)
            {
                useTimeoutConnection = true;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (IsOpened)
                    {
                        Thread.Sleep(Timeout);

                        if (connectionTimer < DateTime.Now)
                        {
                            Destroy();
                        }
                    }

                    useTimeoutConnection = false;
                });
            }
        }

        protected override void Destroy()
        {
            if (m_serverMode)
            {
                _opened = false;
                m_closed = true;
                m_closing = false;
                onCloseHandler?.Invoke(null, null);
                return;
            }

            base.Destroy();
            if (m_closed) _opened = false;
        }
    }
}
