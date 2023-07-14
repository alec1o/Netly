using Netly.Abstract;
using Netly.Core;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Netly
{
    public class TcpClient : Client
    {
        public bool IsEncrypted { get; private set; }
        private readonly TcpServer Server;
        private Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> _onValidation = null;

        /// <summary>
        /// TCP client: Instance<br/><br/>
        /// framing: Enable or disable Netly MessageFraming<code>Netly.Core.MessageFraming</code><br/><br/>
        /// --------------- (recommended)<br/>
        /// *True: netly will use its own message framing protocol <br/><br/>
        /// --------------- (not recommended)<br/>
        /// *False: you will receive raw stream data.<br/>
        /// Just recommended if your server is not netly and you want to communicate with other libraries or use own framing protocol
        /// <code>
        /// TcpServer.OnData((byte[] rawdata) => { raw tcp stream, todo: make own framing })
        /// TcpClient.OnData((byte[] rawdata) => { raw tcp stream, todo: make own framing })
        /// </code>
        /// </summary>
        /// <param name="framing">Enable or disable Netly MessageFraming</param>
        public TcpClient(bool framing = true)
        {
            IsEncrypted = false;
            m_serverMode = false;
            Framing = framing;
        }

        internal TcpClient(string uuid, Socket socket, TcpServer server, out bool success)
        {
            UUID = uuid;
            m_socket = socket;
            Server = server;
            m_serverMode = true;
            IsEncrypted = Server.IsEncrypted;
            Framing = Server.Framing;
            Host = new Host(socket.RemoteEndPoint);

            m_stream = new NetworkStream(m_socket);
            m_sslStream = null;

            try
            {
                Auth();
                success = true;
            }
            catch (Exception e)
            {
                success = false;
                onErrorHandler?.Invoke(null, e);
                Destroy();
            }
        }

        public void UseEncryption(bool enableEncryption, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> onValidation = null)
        {
            _onValidation = onValidation;

            if (IsOpened)
            {
                throw new InvalidOperationException($"You cannot assign the value ({nameof(IsEncrypted)}) while the connection is open.");
            }

            if (!m_serverMode)
            {
                IsEncrypted = enableEncryption;
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

            byte[] buffer = (Framing) ? MessageFraming.CreateMessage(data) : data;

            if (IsEncrypted)
            {
                m_sslStream?.Write(buffer, 0, buffer.Length);
            }
            else
            {
                m_stream?.Write(buffer, 0, buffer.Length);
            }
        }

        private bool OnEncryptionValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_onValidation == null)
            {
                return true;
            }

            // return result of own validation callback
            return _onValidation(sender, certificate, chain, sslPolicyErrors);
        }

        private void Auth()
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
        }

        protected override void Receive()
        {
            int _length = 0;
            byte[] _buffer = new byte[1024 * 32]; // 32 KB
            MessageFraming _framing = new MessageFraming();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Framing)
                {
                    _framing.OnData((buffer) =>
                    {
                        (string name, byte[] buffer) content = EventManager.Verify(buffer);

                        if (content.buffer == null)
                        {
                            onDataHandler?.Invoke(null, buffer);
                        }
                        else
                        {
                            onEventHandler?.Invoke(null, (content.name, content.buffer));
                        }
                    });

                    _framing.OnError((e) =>
                    {
                        _framing?.Clear();
                        _framing = null;
                        onErrorHandler?.Invoke(null, e);
                        Destroy();
                    });
                }

                if (IsEncrypted)
                {
                    m_sslStream.ReadTimeout = 5000;
                    m_sslStream.WriteTimeout = 5000;
                }
                else
                {
                    m_stream.ReadTimeout = 5000;
                    m_stream.WriteTimeout = 5000;
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

                        if (Framing)
                        {
                            _framing.Add(buffer);
                        }
                        else
                        {
                            (string name, byte[] buffer) content = EventManager.Verify(buffer);

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

                _framing?.Clear();
                _framing = null;

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
