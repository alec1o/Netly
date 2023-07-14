using Netly.Abstract;
using Netly.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Netly
{
    public class UdpServer : Server<UdpClient>, IServer<UdpClient>
    {
        public int Timeout { get; private set; }

        public override void Open(Host host, int timeout)
        {
            if (timeout < UdpClient.MIN_TIMEOUT) throw new ArgumentException("TIMEOUT is very low");
            Timeout = timeout;
            Open(host);
        }

        public override void Open(Host host)
        {
            if (IsOpened || m_connecting || m_closing) return;

            m_connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    m_socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Bind(host.EndPoint);

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

        protected override void AcceptOrReceive()
        {
            int length = 0;
            byte[] buffer = new byte[1024*64];
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (IsOpened)
                {
                    try
                    {
                        length = m_socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        if (length <= 0) continue;

                        byte[] data = new byte[length];
                        Array.Copy(buffer, 0, data, 0, data.Length);
                        EndAccept(endpoint, data);
                    }
                    catch { }
                }
            });
        }

        private void EndAccept(EndPoint endpoint, byte[] buffer)
        {
            // find existed client
            string endpointString = ((IPEndPoint)endpoint).ToString();

            foreach (UdpClient target in Clients)
            {
                if (target.Host.IPEndPoint.ToString() == endpointString)
                {
                    target.AddData(buffer);
                    return;
                }
            }

            // create new client
            UdpClient client = new UdpClient(Guid.NewGuid().ToString(), ref m_socket, new Host(endpoint));
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

            client.Timeout = Timeout;
            client.InitServer();
            client.AddData(buffer);
        }

        public override void OnClose(Action callback)
        {
            Timeout = 0;
            base.OnClose(callback);
        }

        protected override bool IsConnected()
        {
            if (m_socket == null) return false;

            return m_opened;
        }
    }
}
