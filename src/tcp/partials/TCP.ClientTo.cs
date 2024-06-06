using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client
        {
            internal class ClientTo : ITCP.ClientTo
            {
                private const int _64kb = 1024 * 64; // 65,536 (64kb)
                private const int EncryptionTimeout = 1000 * 6; // 6 Seconds (6,000ms)

                private readonly Client _client;
                private readonly bool _isServer;
                private readonly ITCP.Server _server;
                private readonly Action<Client, bool> _serverValidatorCallback;
                private bool _initServerValidator;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed;

                private NetworkStream _netStream;

                private Socket _socket;
                private SslStream _sslStream;

                /* ---- CONSTRUCTOR --- */

                private ClientTo()
                {
                    _socket = null;
                    _netStream = null;
                    _sslStream = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isServer = false;
                    _isClosed = true;
                    _initServerValidator = false;
                    _serverValidatorCallback = null;
                    Host = Host.Default;
                    IsEncrypted = false;
                }

                public ClientTo(Client client) : this()
                {
                    _client = client;
                    _isClosed = true;
                }

                public ClientTo(Client client, Socket socket, ITCP.Server server,
                    Action<Client, bool> validatorAction) : this()
                {
                    _client = client;
                    _server = server;
                    _socket = socket;
                    SetDefaultSocketOption(ref _socket);
                    _netStream = new NetworkStream(_socket);
                    _isServer = true;
                    _isClosed = false;
                    IsEncrypted = _server.IsEncrypted;
                    _serverValidatorCallback = validatorAction;
                }

                public Host Host { get; private set; }
                public bool IsEncrypted { get; private set; }
                public bool IsOpened => IsConnected();

                private bool IsFraming => _client.IsFraming;

                private bool CanSend => _isClosed is false && _isClosing is false && _isOpening is false;

                private string Id => _client.Id;

                private ClientOn On => _client._on;

                public Task Open(Host host)
                {
                    if (_isOpening || _isClosing || IsOpened || _isServer) return Task.CompletedTask;

                    _isOpening = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


                            SetDefaultSocketOption(ref _socket);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            Host = new Host(_socket.RemoteEndPoint);

                            _netStream = new NetworkStream(_socket);

                            if (IsEncrypted) InitEncryption();

                            _isClosed = false;

                            On.OnOpen?.Invoke(null, null);

                            InitReceiver();
                        }
                        catch (Exception e)
                        {
                            On.OnError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpening = false;
                        }
                    });
                }

                public Task Close()
                {
                    if (_isOpening || _isClosing) return Task.CompletedTask;

                    _isClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _sslStream?.Close();
                            _netStream?.Close();
                            _socket?.Close();

                            _sslStream?.Dispose();
                            _netStream?.Dispose();
                            _socket?.Dispose();
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                        }
                        finally
                        {
                            _sslStream = null;
                            _netStream = null;
                            _socket = null;

                            if (_isClosed is false)
                            {
                                _isClosed = true;
                                On.OnClose?.Invoke(null, null);
                            }

                            _isClosing = false;
                        }
                    });
                }

                public void Data(byte[] data)
                {
                    if (CanSend == false || data == null) return;

                    SendDispatch(data);
                }

                public void Encryption(bool enable)
                {
                    if (_isServer)
                        throw new InvalidOperationException
                        (
                            $"Must not modify ({nameof(Encryption)}) of server-side client."
                        );

                    if (IsOpened)
                        throw new InvalidOperationException
                        (
                            $"Must not modify ({nameof(Encryption)}) while socket is Connected."
                        );

                    IsEncrypted = enable;
                }

                public void Data(string data)
                {
                    if (CanSend == false || data == null) return;

                    SendDispatch(data.GetBytes());
                }

                public void Data(string data, Encoding encoding)
                {
                    if (CanSend == false || data == null) return;

                    SendDispatch(data.GetBytes(encoding));
                }

                public void Event(string name, byte[] data)
                {
                    if (CanSend == false || data == null || name == null) return;

                    SendDispatch(NetlyEnvironment.EventManager.Create(name, data));
                }

                public void Event(string name, string data)
                {
                    if (CanSend == false || data == null || name == null) return;

                    SendDispatch(NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
                }

                public void Event(string name, string data, Encoding encoding)
                {
                    if (CanSend == false || data == null || name == null) return;

                    SendDispatch(NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
                }

                /* ---- INTERFACE --- */

                public void InitServerValidator()
                {
                    if (_initServerValidator) return;
                    _initServerValidator = true;

                    if (IsEncrypted)
                    {
                        void Callback()
                        {
                            try
                            {
                                InitEncryption();
                                _serverValidatorCallback?.Invoke(_client, true);
                            }
                            catch
                            {
                                _serverValidatorCallback?.Invoke(_client, false);
                            }
                        }

                        // It blocks main thread and have a timeout.
                        Task.Run(Callback);
                    }
                    else
                    {
                        _serverValidatorCallback?.Invoke(_client, true);
                    }
                }

                private static void SetDefaultSocketOption(ref Socket socket)
                {
                    var socketLevel = SocketOptionLevel.Socket;

                    socket.SetSocketOption(socketLevel, SocketOptionName.SendBuffer, _64kb);

                    socket.SetSocketOption(socketLevel, SocketOptionName.ReceiveBuffer, _64kb);
                }

                /* ---- INTERNAL --- */

                private bool IsConnected()
                {
                    try
                    {
                        if (_socket == null || !_socket.Connected) return false;
                        const int timeout = 5000;
                        return !(_socket.Poll(timeout, SelectMode.SelectRead) && _socket.Available == 0);
                    }
                    catch
                    {
                        return false;
                    }
                }

                public void InitServerSide()
                {
                    if (_isServer is false) return;

                    On.OnModify?.Invoke(null, _socket);

                    On.OnOpen?.Invoke(null, null);

                    InitReceiver();
                }

                private static void SetEncryptionTimeout(ref SslStream stream, bool reset)
                {
                    var timeout = reset ? Timeout.Infinite : EncryptionTimeout;

                    stream.ReadTimeout = timeout;
                    stream.WriteTimeout = timeout;
                }

                private void InitEncryption()
                {
                    if (_socket is null) throw new NullReferenceException(nameof(_socket));

                    if (IsEncrypted is false) return;

                    if (_isServer)
                    {
                        _sslStream = new SslStream(_netStream, false);

                        SetEncryptionTimeout(ref _sslStream, false);

                        _sslStream.AuthenticateAsServer
                        (
                            clientCertificateRequired: false,
                            checkCertificateRevocation: true,
                            serverCertificate: _server.Certificate,
                            enabledSslProtocols: _server.EncryptionProtocol
                        );
                    }
                    else
                    {
                        _sslStream = new SslStream
                        (
                            _netStream,
                            false,
                            userCertificateSelectionCallback: null,
                            userCertificateValidationCallback: (sender, certificate, chain, errors) =>
                            {
                                var encryptionCallbackList = On.OnEncryption;

                                // callbacks not found.
                                if (encryptionCallbackList.Count <= 0)
                                {
                                    NetlyEnvironment.Logger.Create(
                                        $"[TCP] Encryption Callback Not Found. Client.Id: {Id}");
                                    return true;
                                }

                                var isValid = true;

                                foreach (var callback in encryptionCallbackList)
                                {
                                    isValid = callback.Invoke(certificate, chain, errors);

                                    // error on validate certificate
                                    if (isValid is false) break;
                                }

                                // all callbacks are true.
                                return isValid;
                            }
                        );

                        SetEncryptionTimeout(ref _sslStream, false);

                        _sslStream.AuthenticateAsClient(string.Empty);
                    }

                    SetEncryptionTimeout(ref _sslStream, true);
                }

                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = NetlyEnvironment.EventManager.Verify(bytes);

                    if (content.buffer == null)
                        On.OnData?.Invoke(null, bytes);
                    else
                        On.OnEvent?.Invoke(null, (content.name, content.buffer));
                }

                private void SendDispatch(byte[] bytes)
                {
                    if (_socket == null || _netStream == null || (IsEncrypted && _sslStream == null)) return;

                    try
                    {
                        if (IsFraming) bytes = NetlyEnvironment.MessageFraming.CreateMessage(bytes);

                        if (IsEncrypted)
                            _sslStream?.BeginWrite(bytes, 0, bytes.Length, null, null);
                        else
                            _netStream?.BeginWrite(bytes, 0, bytes.Length, null, null);
                    }
                    catch
                    {
                        // Ignored
                    }
                }

                private void InitReceiver()
                {
                    var buffer = new byte
                    [
                        // Maximum/Default receive buffer length.
                        (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                    ];

                    var framing = new NetlyEnvironment.MessageFraming();

                    if (IsFraming)
                    {
                        framing.OnData(data => PushResult(ref data));

                        // Framing not match:
                        // Endpoint part isn't using framing or is using different framing protocol
                        framing.OnError(exception =>
                        {
                            NetlyEnvironment.Logger.Create(exception);
                            Close();
                        });
                    }

                    ReceiverUpdate();

                    void ReceiverUpdate()
                    {
                        if (IsEncrypted)
                            _sslStream.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, null);
                        else
                            _netStream.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, null);
                    }

                    void ReceiveCallback(IAsyncResult result)
                    {
                        try
                        {
                            var size = IsEncrypted ? _sslStream.EndRead(result) : _netStream.EndRead(result);

                            if (size <= 0)
                            {
                                if (IsOpened)
                                    ReceiverUpdate();
                                else
                                    Close();

                                return;
                            }

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            if (IsFraming)
                                framing.Add(data);
                            else
                                PushResult(ref data);

                            ReceiverUpdate();
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                            Close();
                        }
                    }
                }
            }
        }
    }
}