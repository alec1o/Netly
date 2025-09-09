using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        internal class ClientTo : ITCP.ClientTo
        {
            private const int EncryptionTimeout = 1000 * 6; // 6 seconds (6000ms)

            private readonly Client _client;
            private readonly bool _isServer;
            private readonly Server _server;
            private readonly Action<Client> _serverValidatorCallback;
            private byte[] _buffer;
            private NetlyEnvironment.MessageFraming _framing;

            private bool
                _isOpening,
                _isClosing,
                _isClosed,
                _initServerValidator;

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
                Host = NHost.Default;
                IsEncrypted = false;
            }

            public ClientTo(Client client) : this()
            {
                _client = client;
                _isClosed = true;
            }

            public ClientTo(Client client, Socket socket, Server server,
                Action<Client> validatorAction) : this()
            {
                _client = client;
                _server = server;
                _socket = socket;
                _netStream = new NetworkStream(_socket);
                _sslStream = new SslStream(_netStream);
                _isServer = true;
                _isClosed = false;
                IsEncrypted = _server.IsEncrypted;
                Host = new NHost(socket.RemoteEndPoint);
                _serverValidatorCallback = validatorAction;
            }

            public NHost Host { get; private set; }
            public bool IsEncrypted { get; private set; }
            public bool IsOpened => IsConnected();

            private bool IsFraming => _client.IsFraming;

            private bool CanSend => _isClosed is false && _isClosing is false && _isOpening is false;


            private ClientOn On => _client._on;

            public async Task Open(NHost host)
            {
                if (_isOpening || _isClosing || IsOpened || _isServer) return;

                _isOpening = true;

                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    On.OnModify?.Invoke(null, _socket);

                    await _socket.ConnectAsync(host.Address, host.Port);

                    Host = new NHost(_socket.RemoteEndPoint);

                    _netStream = new NetworkStream(_socket);

                    if (IsEncrypted) InitEncryption();

                    _isClosed = false;

                    InitReceiver();

                    _isOpening = false; // allow client send data on OnOpen callback.
                    On.OnOpen?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                    On.OnError?.Invoke(null, e);
                }
                finally
                {
                    _isOpening = false;
                }
            }

            public Task Close()
            {
                if (_isOpening || _isClosing) return Task.CompletedTask;

                _isClosing = true;
                _buffer = null;

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
                if (CanSend == false || data == null || data.Length <= 0) return;

                //SendDispatch(data);
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
                if (CanSend == false || string.IsNullOrEmpty(data)) return;

              //  SendDispatch(data.GetBytes());
            }

            public void Data(string data, Encoding encoding)
            {
                if (CanSend == false || string.IsNullOrEmpty(data)) return;

                //SendDispatch(data.GetBytes(encoding));
            }

            public void Event(string name, byte[] data)
            {
                if (CanSend == false || string.IsNullOrEmpty(name) || data == null || data.Length <= 0) return;

               // SendDispatch(NetlyEnvironment.EventManager.Create(name, data));
            }

            public void Event(string name, string data)
            {
                if (CanSend == false || string.IsNullOrEmpty(data) || string.IsNullOrEmpty(name)) return;

                //SendDispatch(NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
            }

            public void Event(string name, string data, Encoding encoding)
            {
                if (CanSend == false || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data)) return;

                //SendDispatch(NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
            }

            /* ---- INTERFACE --- */

            public void InitServerValidator()
            {
                if (_initServerValidator) return;
                _initServerValidator = true;

                if (IsEncrypted)
                {
                    try
                    {
                        InitEncryption();
                        _serverValidatorCallback?.Invoke(_client);
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                        NetlyEnvironment.Logger.Create(
                            $"{GetType()}: {_client.Id}, Encryption error, use non encryption connection (fallback)");
                        IsEncrypted = false;
                        _serverValidatorCallback?.Invoke(_client);
                    }
                }
                else
                {
                    _serverValidatorCallback?.Invoke(_client);
                }
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


            private void InitEncryption()
            {
                if (_socket is null) throw new NullReferenceException(nameof(_socket));

                if (IsEncrypted is false) return;

                Task.Run(async () =>
                {
                    if (_isServer)
                    {
                        _sslStream = new SslStream(_netStream, false);

                        await _sslStream.AuthenticateAsServerAsync
                        (
                            clientCertificateRequired: false, // TODO: clientCertificateRequired is optional
                            checkCertificateRevocation: true,
                            serverCertificate: _server.Certificate,
                            enabledSslProtocols: _server.EncryptionProtocol
                        );
                    }
                    else
                    {
                        _sslStream = new SslStream
                        (
                            innerStream: _netStream,
                            leaveInnerStreamOpen: false,
                            userCertificateSelectionCallback: null,
                            userCertificateValidationCallback: (sender, certificate, chain, errors) =>
                            {
                                var encryptionCallbackList = On.OnEncryption;

                                // callbacks not found.
                                if (encryptionCallbackList.Count <= 0)
                                {
                                    NetlyEnvironment.Logger.Create(
                                        $"[TCP] Encryption Callback Not Found. Client.Id: {_client.Id}");
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

                        await _sslStream.AuthenticateAsClientAsync(string.Empty);
                    }
                }).Wait(EncryptionTimeout);
            }

            private void PublishData(List<byte> bytes)
            {
                /*
                if (Package.ParseMessage(bytes, out var name, out var data))
                    On.OnEvent?.Invoke(null, (name, data));
                else
                    On.OnData?.Invoke(null, bytes);*/
            }

            private void SendDispatch(object package)
            {
/*
                if (_socket == null || _netStream == null || (IsEncrypted && _sslStream == null))
                {
                    package.Clear();
                    return;
                }

                try
                {
                    if (package == null || package.Count <= 0) return;
                    var stream = IsEncrypted ? (Stream)_sslStream : (Stream)_netStream;
                    foreach (var segment in package.Segments) stream.WriteAsync(segment, 0, segment.Length);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }*/
            }

            private void InitReceiver()
            {
                try
                {
                    var bufferSize = _isServer
                        ? (int)_server.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                        : (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);

                    _buffer = new byte[bufferSize];

                    _framing = new NetlyEnvironment.MessageFraming();

                    if (IsFraming)
                    {
                       // _framing.OnData(data => PublishData(data));

                        _framing.OnError(exception =>
                        {
                            NetlyEnvironment.Logger.Create(exception);
                            _ = Close();
                        });
                    }

                    // ReceiveTrigger();
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                    Close();
                }
            }

            private void ReceiveTrigger()
            {
                try
                {
                    var stream = IsEncrypted ? (Stream)_sslStream : (Stream)_netStream;
                    stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveHandler, null);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                    Close();
                }
            }

            private void ReceiveHandler(IAsyncResult result)
            {
                /*
                try
                {
                    var size = IsEncrypted ? _sslStream.EndRead(result) : _netStream.EndRead(result);

                    if (size <= 0)
                    {
                        Close();
                        return;
                    }

                    var bytes = new List<byte>(size);
                    bytes.Insert();

                    Buffer.BlockCopy(_buffer, 0, bytes, 0, bytes.Length);

                    if (IsFraming)
                    {
                        _framing.Add(bytes);
                    }
                    else
                        PublishData(Package.New(()bytes));

                    ReceiveTrigger();
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                    Close();
                }*/
            }

            public Socket GetSocket()
            {
                return _socket;
            }

            public NetworkStream GetNetworkStream()
            {
                return _netStream;
            }

            public SslStream GetSslStream()
            {
                return _sslStream;
            }
        }
    }
}