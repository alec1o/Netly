using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Netly.Packages.Interfaces;

namespace Netly.Packages
{
    public class NTcpClient : INTcpClient
    {
        private readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> _onSecure =
            new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();

        private readonly object _locker = new object();
        private readonly NFraming _framing = new NFraming();
        private byte[] _certificate;
        private string _certificatePassword;
        private NetworkStream _networkStream;
        private Action<bool> _onAccepted;
        private EventHandler _onConnect, _onDisconnect;
        private EventHandler<Socket> _onCreate;
        private EventHandler<(string Name, Stream Stream)> _onEvent;
        private EventHandler<Exception> _onFail;
        private EventHandler<Stream> _onMessage;
        private Func<long, Stream> _onStream;
        private SslStream _secureStream;
        private Func<long, Stream> NewStream => IsServer ? _server.OnStreamObject : _onStream;

        private NTcpClient()
        {
            _server = null;
            _onStream = NUtils.NewStream;
            Host = NHost.Default;
            IsConnected = false;
            SecureProtocol = SslProtocols.Default;
            SecureDomain = string.Empty;
            IsSecure = false;
            IsFraming = false;
            IsServer = false;
            FramingSize = NFraming.DefaultSize;
            Socket = null;
            Certificate = new X509Certificate();
        }

        public NTcpClient(bool isFraming) : this()
        {
            IsFraming = isFraming;
        }

        internal NTcpClient(Socket socket, NTcpServer server) : this()
        {
            IsFraming = server.IsFraming;
            IsSecure = server.IsSecure;
            IsServer = true;
            IsConnected = true;
            Socket = socket;
            _server = server;
        }

        private NTcpServer _server { get; }

        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsConnected { get; private set; }
        public bool IsSecure { get; private set; }
        public bool IsFraming { get; }
        public bool IsServer { get; }
        public SslProtocols SecureProtocol { get; private set; }
        public string SecureDomain { get; private set; }

        public long FramingSize
        {
            get => _framing.MaxSize;
            set
            {
                if (IsConnected)
                    throw new InvalidOperationException($"Must not update {nameof(FramingSize)} now");
                _framing.MaxSize = Math.Max(1024, value);
            }
        }

        public Socket Socket { get; private set; }
        public Stream Stream => IsSecure ? _secureStream : (Stream)_networkStream;
        public NHost Host { get; private set; }
        public X509Certificate Certificate { get; private set; }

        public void OnFail(Action<Exception> callback)
        {
            _onFail += (sender, exception) => callback?.Invoke(exception);
        }

        public void OnEvent(Action<string, Stream> callback)
        {
            _onEvent += (sender, data) => callback?.Invoke(data.Name, data.Stream);
        }

        public void OnMessage(Action<Stream> callback)
        {
            _onMessage += (sender, message) => callback?.Invoke(message);
        }

        public void OnCreate(Action<Socket> callback)
        {
            _onCreate += (sender, socket) => callback?.Invoke(socket);
        }

        public void OnStream(Func<long, Stream> callback)
        {
            _onStream = callback;
        }

        public void OnConnect(Action callback)
        {
            _onConnect += (sender, _) => callback?.Invoke();
        }

        public void OnDisconnect(Action callback)
        {
            _onDisconnect += (sender, _) => callback?.Invoke();
        }

        public void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
        {
            _onSecure.Add(callback);
        }

        public void ToMessage(byte[] message)
        {
            if (message == null || message.Length < 1) return;

            Send(message);
        }

        public void ToEvent(string name, byte[] message)
        {
            if (string.IsNullOrWhiteSpace(name) || message == null || message.Length < 1) return;

            Send(NMessage.Create(name, message.Length), message);
        }

        public void ToConnect(NHost host)
        {
            ToConnectAsync(host);
        }

        public Task ToConnectAsync(NHost host)
        {
            return Task.Run(() =>
            {
                lock (_locker)
                {
                    if (IsConnected || IsServer) return;

                    try
                    {
                        var socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        _onCreate?.Invoke(null, socket);

                        socket.Connect(host.Address, host.Port);

                        Host = new NHost(socket.RemoteEndPoint);

                        _networkStream = new NetworkStream(socket);

                        if (IsSecure)
                        {
                            Certificate = new X509Certificate(_certificate, _certificatePassword);
                            InitializeSecureConnection();
                        }

                        IsConnected = true;

                        _onConnect?.Invoke(null, null);

                        InitReceiver();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                        _onFail?.Invoke(null, e);
                    }
                }
            });
        }

        public void ToDisconnect()
        {
            ToDisconnectAsync();
        }

        public void ToSecure(bool allowSecure, byte[] certificate = null, string password = null,
            SslProtocols protocols = SslProtocols.Default, string secureDomain = null)
        {
            if (IsConnected)
                throw new InvalidOperationException($"Can't call {nameof(ToConnect)}, {nameof(IsConnected)}");

            IsSecure = allowSecure;
            SecureProtocol = protocols;
            SecureDomain = secureDomain;
            _certificatePassword = password;
            _certificate = certificate;
        }

        public Task ToDisconnectAsync()
        {
            if (!IsConnected) return Task.CompletedTask;

            return Task.Run(() =>
            {
                lock (_locker)
                {
                    if (Socket == null) return;

                    try
                    {
                        _framing.Close();
                        _networkStream?.Close();
                        _secureStream?.Close();
                        Socket?.Close();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        IsConnected = false;
                        _networkStream = null;
                        _secureStream = null;
                        Socket = null;
                        _onDisconnect?.Invoke(null, null);
                    }
                }
            });
        }

        internal void OnAccepted(Action<bool> callback)
        {
            _onAccepted = callback;
        }


        private void Send(params byte[][] buffers)
        {
            try
            {
                if (IsFraming)
                {
                    var buffer = NFraming.Create(buffers.Sum(x => x.LongLength));
                    Stream.WriteAsync(buffer, 0, buffer.Length);
                }

                foreach (var buffer in buffers) Stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
            }
        }

        private void InitReceiver()
        {
            if (_networkStream == null)
                _networkStream = new NetworkStream(Socket, true);

            var bufferSize = IsServer
                ? (int)_server.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                : (int)Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);

            _framing.Open();

            ReceiveTrigger(new byte[bufferSize]);
        }

        private void ReceiveTrigger(byte[] buffer)
        {
            try
            {
                Stream.BeginRead(buffer, 0, buffer.Length, ReceiveHandler, buffer);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                ToDisconnect();
            }
        }

        private void ReceiveHandler(IAsyncResult result)
        {
            try
            {
                var size = Stream.EndRead(result);
                var buffer = (byte[])result.AsyncState;

                if (size <= 0)
                {
                    ToDisconnect();
                    return;
                }

                if (IsFraming)
                {
                    _framing.Write(new ArraySegment<byte>(buffer, 0, size), NewStream);
                    while (_framing.Read(out var stream)) ReceiveRelease(stream);
                }
                else
                {
                    var stream = _onStream(size);
                    stream.Write(buffer, 0, size);
                    ReceiveRelease(stream);
                }

                ReceiveTrigger(buffer);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                ToDisconnect();
            }
        }

        private void ReceiveRelease(Stream stream)
        {
            if (stream.Length < 1) throw new ArgumentOutOfRangeException(nameof(stream));

            if (NMessage.TryParse(stream, out var name, out var message, NewStream))
            {
                stream.Close();
                _onEvent?.Invoke(null, (name, message));
            }
            else
            {
                message?.Close();
                _onMessage?.Invoke(null, stream);
            }
        }

        private void InitializeSecureConnection()
        {
            if (Socket is null) throw new NullReferenceException(nameof(Socket));

            if (IsSecure is false) return;

            Task.Run(async () =>
            {
                if (_networkStream == null)
                    _networkStream = new NetworkStream(Socket, true);

                if (IsServer)
                {
                    _secureStream = new SslStream(_networkStream, false);

                    await _secureStream.AuthenticateAsServerAsync
                    (
                        clientCertificateRequired: !_server.AllowUnsecured,
                        checkCertificateRevocation: true,
                        serverCertificate: _server.Certificate,
                        enabledSslProtocols: _server.SecureProtocol
                    );
                }
                else
                {
                    _secureStream = new SslStream
                    (
                        _networkStream,
                        false,
                        (sender, certificate, chain, errors) =>
                        {
                            var list = IsServer ? _server.OnSecureObject : _onSecure;

                            // callbacks not found.
                            if (list.Count <= 0)
                            {
                                NetlyEnvironment.Logger.Create(
                                    $"[TCP] Encryption Callback Not Found. Client.Id: {Id}");
                                return true;
                            }

                            var isValid = true;

                            foreach (var callback in list)
                            {
                                isValid = callback.Invoke(certificate, chain, errors);

                                // error on validate certificate
                                if (isValid is false) break;
                            }

                            // all callbacks are true.
                            return isValid;
                        }
                    );

                    await (Certificate == null
                        ? _secureStream.AuthenticateAsClientAsync(SecureDomain)
                        : _secureStream.AuthenticateAsClientAsync
                        (
                            SecureDomain,
                            new X509CertificateCollection(new[] { Certificate }),
                            SecureProtocol,
                            true
                        ));
                }
            }).Wait(TimeSpan.FromSeconds(7));
        }

        internal void ServerInitializer()
        {
            try
            {
                var success = true;

                if (IsSecure)
                    try
                    {
                        InitializeSecureConnection();
                    }
                    catch (Exception e)
                    {
                        success = _server.AllowUnsecured;
                        if (success) IsSecure = false;
                        NetlyEnvironment.Logger.Create(e);
                    }

                _onAccepted?.Invoke(success);

                InitReceiver();
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                _onAccepted?.Invoke(false);
            }
        }
    }
}