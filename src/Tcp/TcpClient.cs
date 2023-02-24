using Netly.Abstract;
using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Netly
{
    public class TcpClient : Client
    {
        public TcpClient() { }

        internal TcpClient(string uuid, Socket socket)
        {
            UUID = uuid;
            m_socket = socket;
            Host = new Host(socket.RemoteEndPoint);
        } 

        public override void Open(Host host)
        {
            if (IsOpened || m_connecting || m_closing || m_serverMode) return;

            m_connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    m_socket.NoDelay = true;

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Connect(host.EndPoint);

                    Host = host;

                    m_closed = false;

                    onOpenHandler?.Invoke(null, null);

                    Receive();
                }
                catch (Exception e)
                {
                    onErrorHandler?.Invoke(null, e);
                }

                m_connecting = false;
            });
        }

        protected override void Receive()
        {
            int _length = 0;
            byte[] _buffer = new byte[Package.MAX_SIZE];
            Package _package = new Package();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                _package.Output((buffer) =>
                {
                    (string name, byte[] buffer) content = MessageParser.Verify(buffer);

                    if (content.buffer == null)
                        onDataHandler?.Invoke(null, buffer);
                    else
                        onEventHandler?.Invoke(null, (content.name, content.buffer));
                });

                while (m_socket != null)
                {
                    try
                    {
                        _length = m_socket.Receive(_buffer, 0, _buffer.Length, SocketFlags.None);

                        if (_length <= 0)
                        {
                            if (IsOpened is true) continue;
                            break;
                        }

                        byte[] buffer = new byte[_length];
                        Array.Copy(_buffer, 0, buffer, 0, buffer.Length);

                        _package.Input(buffer);
                    }
                    catch
                    {
                        if (IsOpened is false) break;
                    }
                }

                _package = null;
                m_socket?.Shutdown(SocketShutdown.Both);

                Destroy();
            });
        }

        protected override bool IsConnected()
        {
            try
            {
                if (m_socket == null || !m_socket.Connected) return false;
                const int timeout = 5000;
                return !(m_socket.Poll(timeout, SelectMode.SelectRead) && m_socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }
    }
}
