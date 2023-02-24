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
        private bool _timeoutInited;
        private DateTime _lastUpdate;
        public const int DEFAULT_TIMEOUT = 5000;
        public const int MIN_TIMEOUT = 500;

        public int Timeout { get; internal set; }
        public UdpClient() { }

        internal UdpClient(string uuid, ref Socket socket, Host host)
        {
            UUID = uuid;
            m_socket = socket;
            Host = host;
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

            if (Timeout < MIN_TIMEOUT) Timeout = MIN_TIMEOUT;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Connect(host.EndPoint);

                    Host = host;

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
            if (data == null) return;
            m_socket?.SendTo(data, SocketFlags.None, Host.EndPoint);
        }

        protected override void Receive()
        {
            int length = 0;
            byte[] buffer = new byte[Package.MAX_SIZE];
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
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

                if (!m_closing || !m_closed)
                {
                    m_closed = true;
                    m_socket.Dispose();
                    onCloseHandler?.Invoke(null, null);
                    m_socket = null;
                }
            });
        }

        internal void AddData(byte[] data)
        {
            (string name, byte[] buffer) content = MessageParser.Verify(data);

            if (content.buffer == null)
                onDataHandler?.Invoke(null, data);
            else
                onEventHandler?.Invoke(null, (content.name, content.buffer));
        }

        private void ResetTimer()
        {
            if (Timeout == 0) return;

            _lastUpdate = DateTime.Now.AddMilliseconds(Timeout);

            if (_timeoutInited) return;
            _timeoutInited = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (m_socket != null)
                {
                    Thread.Sleep(Timeout);

                    if (_lastUpdate < DateTime.Now)
                    {
                        m_socket?.Shutdown(SocketShutdown.Both);

                        if (!m_closing || !m_closed)
                        {
                            m_closed = true;
                            onCloseHandler?.Invoke(null, null);
                            m_socket?.Dispose();
                            m_socket = null;
                        }
                    }
                }

                _timeoutInited = false;
            });
        }
    }
}
