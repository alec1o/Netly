﻿using Netly.Core;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Netly.Abstract
{
    public class TcpServers : Server<TcpClient>, IServer<TcpClient>
    {
        public override void Open(Host host, int backlog)
        {
            Console.WriteLine($"TRY OPEN 1: {IsOpened}, {m_tryOpen}, {m_tryClose}");
            if (IsOpened || m_tryOpen || m_tryClose) return;
            Console.WriteLine($"TRY OPEN 2: {IsOpened}, {m_tryOpen}, {m_tryClose}");

            m_tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Bind(host.EndPoint);
                    m_socket.Listen(backlog);

                    Host = host;

                    m_opened = true;
                    m_invokeClose = false;

                    onOpenHandler?.Invoke(null, null);

                    AcceptOrReceive();
                }
                catch (Exception e)
                {
                    onErrorHandler?.Invoke(null, e);
                }

                m_tryOpen = false;
            });
        }

        protected override bool IsConnected()
        {
            if (m_socket == null) return false;

            return m_opened;
        }

        public override void AcceptOrReceive()
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