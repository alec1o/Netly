using Netly.Abstract;
using Netly.Core;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Netly
{
    public class TcpServer : Server<TcpClient>, IServer<TcpClient>
    {
        public override void Open(Host host, int backlog)
        {
            if (IsOpened || m_connecting || m_closing) return;

            m_connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Bind(host.EndPoint);
                    m_socket.Listen(backlog);

                    Host = new Host(m_socket.LocalEndPoint);

                    m_opened = true;
                    m_closed = false;

                    onOpenHandler?.Invoke(null, null);

                    AcceptOrReceive();
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

            return m_opened;
        }

        protected override void AcceptOrReceive()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (IsOpened)
                {
                    try
                    {
                        var clientSocket = m_socket.Accept();

                        if (clientSocket == null) continue;

                        EndAccept(clientSocket);
                    }
                    catch { }
                }
            });

            void EndAccept(Socket socket)
            {
                TcpClient client = new TcpClient(Guid.NewGuid().ToString(), socket);
                AddOrRemoveClient(client, false);

                client.OnOpen(() =>
                {
                    onEnterHandler?.Invoke(null, client);
                });

                client.OnClose(() =>
                {
                    onExitHandler?.Invoke(null, AddOrRemoveClient(client, true));
                });

                client.OnData((data) =>
                {
                    onDataHandler?.Invoke(null, (client, data));
                });

                client.OnEvent((name, data) =>
                {
                    onEventHandler?.Invoke(null, (client, name, data));
                });

                client.InitServer();
            }
        }

    }
}
