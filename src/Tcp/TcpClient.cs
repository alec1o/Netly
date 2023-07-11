using Netly.Abstract;
using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Netly
{
    public class TcpClient : Client
    {
        public bool IsEncrypted { get; private set; }
        private readonly TcpServer Server;

        /// <summary>
        /// TCP client: Instance
        /// </summary>
        /// <param name="messageFraming">true: netly will use its own message framing protocol, set false if your server is not netly and you want to communicate with other libraries</param>
        public TcpClient(bool messageFraming)
        {
            IsEncrypted = false;
            m_serverMode = false;
            MessageFraming = messageFraming;
        }

        internal TcpClient(string uuid, Socket socket, TcpServer server)
        {
            UUID = uuid;
            m_socket = socket;
            Server = server;
            m_serverMode = true;
            IsEncrypted = Server.IsEncrypted;
            MessageFraming = Server.MessageFraming;
            Host = new Host(socket.RemoteEndPoint);

            m_stream = new NetworkStream(m_socket);
            m_sslStream = null;

            try
            {
                Auth();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                Destroy();
            }
        }

        public void UseEncryption(bool value)
        {
            if (IsOpened)
            {
                throw new InvalidOperationException($"You cannot assign the value ({nameof(IsEncrypted)}) while the connection is open.");
            }

            if (!m_serverMode)
            {
                IsEncrypted = value;
            }
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

                    onModifyHandler?.Invoke(null, m_socket);

                    m_socket.Connect(host.EndPoint);

                    Host = new Host(m_socket.RemoteEndPoint);

                    m_stream = new NetworkStream(m_socket);
                    m_sslStream = null;

                    if (IsEncrypted)
                    {
                        Auth();
                    }

                    m_closed = false;

                    Receive();

                    onOpenHandler?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    onErrorHandler?.Invoke(null, e);
                }

                m_connecting = false;
            });
        }

        public override void ToData(byte[] data)
        {
            if (m_closing || m_closed) return;

            byte[] buffer = (MessageFraming) ? Package.Create(ref data) : data;

            if (IsEncrypted)
            {
                m_sslStream?.Write(buffer, 0, buffer.Length);
            }
            else
            {
                m_stream?.Write(buffer, 0, buffer.Length);
            }
        }

        public virtual bool OnEncryptionValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private bool Auth()
        {
            if (m_socket == null) throw new NullReferenceException(nameof(m_socket));

            if (IsEncrypted)
            {
                if (m_serverMode)
                {
                    m_sslStream = new SslStream(m_stream, false);

                    m_sslStream.AuthenticateAsServer
                    (
                        clientCertificateRequired: false,
                        checkCertificateRevocation: true,
                        serverCertificate: Server.Certificate,
                        enabledSslProtocols: Server.EncryptionProtocol
                    );
                }
                else
                {
                    m_sslStream = new SslStream
                    (
                        innerStream: m_stream,
                        leaveInnerStreamOpen: false,
                        userCertificateSelectionCallback: null,
                        userCertificateValidationCallback: new RemoteCertificateValidationCallback(OnEncryptionValidation)
                    );

                    m_sslStream.AuthenticateAsClient(string.Empty);
                }

                m_sslStream.ReadTimeout = 5000;
                m_sslStream.WriteTimeout = 5000;
            }

            return false;
        }

        protected override void Receive()
        {
            int _length = 0;
            byte[] _buffer = new byte[Package.MAX_SIZE];
            Package _package = new Package();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (MessageFraming)
                {
                    _package.Output((buffer) =>
                    {
                        (string name, byte[] buffer) content = MessageParser.Verify(buffer);

                        if (content.buffer == null)
                        {
                            onDataHandler?.Invoke(null, buffer);
                        }
                        else
                        {
                            onEventHandler?.Invoke(null, (content.name, content.buffer));
                        }
                    });
                }

                while (m_socket != null)
                {
                    try
                    {
                        _length = IsEncrypted is false
                            ? m_stream.Read(_buffer, 0, _buffer.Length)
                            : m_sslStream.Read(_buffer, 0, _buffer.Length);

                        if (_length <= 0)
                        {
                            if (IsOpened is true) continue;
                            break;
                        }

                        byte[] buffer = new byte[_length];
                        Array.Copy(_buffer, 0, buffer, 0, buffer.Length);

                        if (MessageFraming)
                        {
                            _package.Input(buffer);
                        }
                        else
                        {
                            (string name, byte[] buffer) content = MessageParser.Verify(buffer);

                            if (content.buffer == null)
                            {
                                onDataHandler?.Invoke(null, buffer);
                            }
                            else
                            {
                                onEventHandler?.Invoke(null, (content.name, content.buffer));
                            }
                        }
                    }
                    catch
                    {
                        if (IsOpened is false) break;
                    }
                }

                _package = null;

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
