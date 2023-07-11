using Netly.Abstract;
using Netly.Core;
using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Netly
{
    public class TcpServer : Server<TcpClient>, IServer<TcpClient>
    {
        public bool IsEncrypted { get; private set; }
        public SslProtocols EncryptionProtocol { get; private set; }

        private byte[] _pfxCertificate;
        private string _pfxPassword;

        internal X509Certificate Certificate { get; private set; }

        /// <summary>
        /// TCP server: Instance
        /// </summary>
        /// <param name="messageFraming">true: netly will use its own message framing protocol, set false if your server is not netly and you want to communicate with other libraries</param>
        public TcpServer(bool messageFraming)
        {
            IsEncrypted = false;
            MessageFraming = messageFraming;
        }

        public override void Open(Host host, int backlog)
        {
            if (IsOpened || m_connecting || m_closing) return;

            m_connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (IsEncrypted)
                    {
                        Certificate = new X509Certificate2(_pfxCertificate, _pfxPassword);
                    }

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

        public void UseEncryption(byte[] pfxCertificate, string pfxPassword, SslProtocols encryptionProtocol)
        {
            if (IsOpened)
            {
                throw new InvalidOperationException($"You cannot assign the value ({nameof(IsEncrypted)}) while the connection is open.");
            }

            if (pfxCertificate == null || pfxCertificate.Length <= 0)
            {
                throw new ArgumentNullException($"[Encrypetion Error]: {nameof(IsEncrypted)} is true and ({nameof(pfxCertificate)}) is null/empty");
            }

            IsEncrypted = true;
            EncryptionProtocol = encryptionProtocol;
            _pfxCertificate = pfxCertificate;
            _pfxPassword = pfxPassword;
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
                TcpClient client = new TcpClient(Guid.NewGuid().ToString(), socket, this);
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
