using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Netly.Packages
{
    public class NTcp : INTcp
    {
        private readonly object _locker = new object();
        private Func<long, Stream> _onStream;
        private readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> _onSecure;
        private byte[] _buffer;
        private LinkedList<byte[]> _frameBuffer;
        private long _frameSize, _maxFrameSize;
        private NetworkStream _networkStream;
        private EventHandler _onConnect, _onDisconnect;
        private EventHandler<Socket> _onCreate;
        private EventHandler<(string Name, byte[] Message)> _onEvent;
        private EventHandler<Exception> _onFail;
        private EventHandler<byte[]> _onMessage;
        private SslStream _secureStream;

        private NTcp()
        {
            _onSecure = new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();
            _buffer = Array.Empty<byte>();
            _server = null;
            _onStream = DefaultOnStream;
            IsConnected = false;
            IsServer = false;
            IsFraming = false;
            IsSecure = false;
            MaxFrameSize = 1024 * 1024 * 20; // 20.00 MB
        }

        private static Stream DefaultOnStream(long size)
        {
            if (size < 1024 * 1024 * 10) return new MemoryStream((int)size); // 10.00 MB
            return new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite);
        }

        public NTcp(bool isFraming) : this()
        {
            IsFraming = isFraming;
        }

        internal NTcp(TCP.Server server) : this()
        {
            IsFraming = server.IsFraming;
            IsSecure = server.IsEncrypted;
            MaxFrameSize = server.Framing.MaxSize;
            IsServer = true;
            IsConnected = true;
            _server = server;
        }

        private TCP.Server _server { get; }

        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsConnected { get; private set; }
        public bool IsSecure { get; private set; }
        public bool IsFraming { get; }
        public bool IsServer { get; }

        public long MaxFrameSize
        {
            get => _maxFrameSize;
            set
            {
                if (value < 1024) throw new ArgumentOutOfRangeException($"Min {nameof(MaxFrameSize)} is 1024: {value}");
                _maxFrameSize = value;
            }
        }

        public Socket Socket { get; private set; }
        public Stream Stream => IsSecure ? _secureStream : (Stream)_networkStream;
        public Host Host { get; private set; }
        public X509Certificate Certificate { get; private set; }

        public void OnFail(Action<Exception> callback)
        {
            _onFail += (sender, exception) => callback?.Invoke(exception);
        }

        public void OnEvent(Action<string, byte[]> callback)
        {
            _onEvent += (sender, data) => callback?.Invoke(data.Name, data.Message);
        }

        public void OnMessage(Action<byte[]> callback)
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

            var list = new LinkedList<byte[]>();
            list.AddLast(message);

            Send(list);
        }

        public void ToEvent(string name, byte[] message)
        {
            if (string.IsNullOrWhiteSpace(name) || message == null || message.Length < 1) return;

            var list = new LinkedList<byte[]>();
            list.AddLast(Framing.CreateMessage(name));
            list.AddLast(message);

            Send(list);
        }

        public void ToConnect(Host host)
        {
            ToConnectAsync(host);
        }

        public Task ToConnectAsync(Host host)
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

                        Host = new Host(socket.RemoteEndPoint);

                        _networkStream = new NetworkStream(socket);

                        if (IsSecure) InitializeSecurity();

                        InitReceiver();

                        IsConnected = false;

                        _onConnect?.Invoke(null, null);
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

        public void ToSecure(bool enableSecureMode, X509Certificate certificate = null)
        {
            IsSecure = enableSecureMode;
            Certificate = certificate;
        }

        public Task ToDisconnectAsync()
        {
            return Task.Run(() =>
            {
                lock (_locker)
                {
                    if (Socket == null) return;

                    try
                    {
                        _frameBuffer?.Clear();
                        _networkStream?.Close();
                        _secureStream?.Close();
                        Socket?.Close();
                        _networkStream?.Dispose();
                        _secureStream?.Dispose();
                        Socket?.Dispose();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        _frameSize = 0;
                        _buffer = null;
                        _frameBuffer = null;
                        Socket = null;
                        _networkStream = null;
                        _secureStream = null;
                        _onDisconnect?.Invoke(null, null);
                    }
                }
            });
        }

        private void Send(LinkedList<byte[]> list)
        {
            try
            {
                if (IsFraming) list.AddFirst(Framing.CreateFrame(list.Sum(x => x.LongLength)));
                foreach (var bytes in list) Stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
            }
            finally
            {
                list.Clear();
            }
        }

        private void InitReceiver()
        {
            if (_networkStream == null)
                _networkStream = new NetworkStream(Socket, true);

            var bufferSize = IsServer
                ? (int)_server.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                : (int)Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);

            _buffer = new byte[bufferSize];

            if (IsFraming) _frameBuffer = new LinkedList<byte[]>();

            ReceiveTrigger();
        }

        private void ReceiveTrigger()
        {
            try
            {
                Stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveHandler, null);
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

                if (size <= 0)
                {
                    ToDisconnect();
                    return;
                }

                _onStream();

                var bytes = new byte[size];

                Buffer.BlockCopy(_buffer, 0, bytes, 0, bytes.Length);

                if (IsFraming)
                {
                    if (Framing.NextFrame(ref _frameSize, ref _frameBuffer, out var frameBuffer))
                    {
                        ReceiveRelease(_frameBuffer);
                        _frameBuffer = frameBuffer;
                    }
                }
                else
                {
                    var list = new LinkedList<byte[]>();
                    list.AddLast(bytes);
                    ReceiveRelease(list);
                }

                ReceiveTrigger();
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

            if (Framing.NextMessage(ref stream, out var name))
                _onEvent?.Invoke(null, (name, null));
            else
                _onMessage?.Invoke(null, stream);
        }

        internal void InitializeSecurity()
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
                        clientCertificateRequired: false, // TODO: clientCertificateRequired is optional
                        checkCertificateRevocation: true,
                        serverCertificate: _server.Certificate,
                        enabledSslProtocols: _server.EncryptionProtocol
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
                            // callbacks not found.
                            if (_onSecure.Count <= 0)
                            {
                                NetlyEnvironment.Logger.Create(
                                    $"[TCP] Encryption Callback Not Found. Client.Id: {Id}");
                                return true;
                            }

                            var isValid = true;

                            foreach (var callback in _onSecure)
                            {
                                isValid = callback.Invoke(certificate, chain, errors);

                                // error on validate certificate
                                if (isValid is false) break;
                            }

                            // all callbacks are true.
                            return isValid;
                        }
                    );

                    await _secureStream.AuthenticateAsClientAsync(string.Empty);
                }
            }).Wait(TimeSpan.FromSeconds(7));
        }
    }
}