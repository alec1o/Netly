using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client
        {
            internal class _To : ITo
            {
                public Host Host { get; private set; }
                public bool IsEncrypted { get; private set; }
                public bool IsOpened => IsConnected();

                private bool IsFraming => _client._isFraming;

                private bool CanSend => _isClosed || _isClosing || _isOpening;

                private string Id => _client._id;

                private _On On => _client._on;

                private Socket _socket;
                private NetworkStream _netStream;
                private SslStream _sslStream;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed;

                private readonly Client _client;
                private readonly IServer _server;
                private readonly bool _isServer;
                private readonly List<byte[]> _dataList;

                /* ---- CONSTRUCTOR --- */

                private _To()
                {
                    _dataList = new List<byte[]>();
                    _socket = null;
                    _netStream = null;
                    _sslStream = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isServer = false;
                    _isClosed = true;
                    Host = Host.Default;
                    IsEncrypted = false;
                }

                public _To(Client client)
                {
                    _client = client;
                    _isClosed = true;
                }

                public _To(Client client, Socket socket, IServer server, out bool success) : this()
                {
                    _client = client;
                    _server = server;
                    _socket = socket;
                    _netStream = new NetworkStream(_socket);
                    _isServer = true;
                    _isClosed = false;
                    IsEncrypted = _server.IsEncrypted;

                    if (IsEncrypted)
                    {
                        try
                        {
                            InitEncryption();
                            success = true;
                        }
                        catch (Exception e)
                        {
                            Netly.Logger.PushError(e);
                            success = false;
                        }
                    }
                    else
                    {
                        success = true;
                    }
                }

                /* ---- INTERFACE --- */

                public void Open(Host host)
                {
                    if (_isOpening || _isClosing || IsOpened || _isServer) return;

                    _isOpening = true;

                    Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            On.m_onModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            Host = new Host(_socket.RemoteEndPoint);

                            _netStream = new NetworkStream(_socket);

                            if (IsEncrypted) InitEncryption();

                            _isClosed = false;

                            On.m_onOpen?.Invoke(null, null);
                            
                            InitReceiver();
                        }
                        catch (Exception e)
                        {
                            On.m_onError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpening = false;
                        }
                    });
                }

                public void Close()
                {
                    if (_isOpening || _isClosing) return;

                    _isClosing = true;

                    Task.Run(() =>
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
                            Netly.Logger.PushError(e);
                        }
                        finally
                        {
                            _dataList.Clear();
                            _sslStream = null;
                            _netStream = null;
                            _socket = null;

                            if (_isClosed is false)
                            {
                                _isClosed = true;
                                On.m_onClose?.Invoke(null, null);
                            }

                            _isClosing = false;
                        }
                    });
                }

                public void Data(byte[] data)
                {
                    if (CanSend == false || data == null) return;

                    _dataList.Add(data);
                }

                public void Encryption(bool enable)
                {
                    if (_isServer)
                    {
                        throw new InvalidOperationException
                        (
                            $"Must not modify ({nameof(Encryption)}) of server-side client."
                        );
                    }

                    if (IsOpened)
                    {
                        throw new InvalidOperationException
                        (
                            $"Must not modify ({nameof(Encryption)}) while socket is Connected."
                        );
                    }

                    IsEncrypted = enable;
                }

                public void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (CanSend == false || data == null) return;

                    _dataList.Add(NE.GetBytes(data, encoding));
                }

                public void Event(string name, byte[] data)
                {
                    if (CanSend == false || data == null || name == null) return;

                    _dataList.Add(EventManager.Create(name, data));
                }

                public void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (CanSend == false || data == null || name == null) return;

                    _dataList.Add(EventManager.Create(name, NE.GetBytes(data, encoding)));
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

                    On.m_onModify?.Invoke(null, _socket);

                    On.m_onOpen?.Invoke(null, null);

                    InitReceiver();
                }

                private void InitEncryption()
                {
                    if (_socket is null) throw new NullReferenceException(nameof(_socket));

                    if (IsEncrypted is false) return;

                    if (_isServer)
                    {
                        _sslStream = new SslStream(_netStream, false);

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
                            innerStream: _netStream,
                            leaveInnerStreamOpen: false,
                            userCertificateSelectionCallback: null,
                            userCertificateValidationCallback: (sender, certificate, chain, errors) =>
                            {
                                var encryptionCallbackList = On.m_onEncryption;

                                // callbacks not found.
                                if (encryptionCallbackList.Count <= 0)
                                {
                                    Netly.Logger.PushWarning(
                                        $"[TCP] Encryption Callback Not Found. Client.Id: {Id}");
                                    return true;
                                }

                                bool isValid = true;

                                foreach (var callback in encryptionCallbackList)
                                {
                                    isValid = callback.Invoke(certificate, chain, errors);

                                    // error on validate certificate
                                    if (isValid is false)
                                    {
                                        break;
                                    }
                                }

                                // all callbacks is true.
                                return isValid;
                            }
                        );

                        _sslStream.AuthenticateAsClient(string.Empty);
                    }
                }


                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = EventManager.Verify(bytes);

                    if (content.buffer == null)
                    {
                        On.m_onData?.Invoke(null, bytes);
                    }
                    else
                    {
                        On.m_onEvent?.Invoke(null, (content.name, content.buffer));
                    }
                }

                private void ReceiveJob()
                {
                    // Is max TCP packet size.
                    byte[] buffer = new byte[1024 * 64]; // 65536 (64kb).

                    MessageFraming framing = new MessageFraming();

                    if (IsFraming)
                    {
                        framing.OnData(data => PushResult(ref data));

                        // Framing not match:
                        // Endpoint part isn't using framing or is using different framing protocol
                        framing.OnError(exception =>
                        {
                            Netly.Logger.PushError(exception);
                            Close();
                        });
                    }

                    while (_socket != null)
                    {
                        try
                        {
                            int size = IsEncrypted
                                ? _sslStream.Read(buffer, 0, buffer.Length)
                                : _netStream.Read(buffer, 0, buffer.Length);

                            if (size <= 0)
                            {
                                if (IsOpened) continue;
                                break;
                            }

                            byte[] data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            if (IsFraming)
                            {
                                framing.Add(data);
                            }
                            else
                            {
                                PushResult(ref data);
                            }
                        }
                        catch (Exception e)
                        {
                            Netly.Logger.PushError(e);
                            if (!IsOpened) break;
                        }
                    }

                    Close();
                }

                private void SendJob()
                {
                    while (_socket != null && _netStream != null || (IsEncrypted && _sslStream != null))
                    {
                        if (_dataList.Count <= 0) continue;

                        try
                        {
                            byte[] bytes = _dataList[0];
                            _dataList.RemoveAt(0);

                            if (IsFraming)
                            {
                                bytes = MessageFraming.CreateMessage(bytes);
                            }

                            if (IsEncrypted)
                            {
                                _sslStream?.Write(bytes, 0, bytes.Length);
                            }
                            else
                            {
                                _netStream?.Write(bytes, 0, bytes.Length);
                            }
                        }
                        catch
                        {
                            // Ignored
                        }
                    }
                }

                private void InitReceiver()
                {
                    // true: thread is destroyed normally when program end (non-force required)
                    // false: this thread will be persistent and will block the destruction of main process (force quit required)
                    const bool isBackground = true;

                    var sendJob = new Thread(SendJob) { IsBackground = isBackground };
                    var receiveJob = new Thread(ReceiveJob) { IsBackground = isBackground };

                    sendJob.Start();
                    receiveJob.Start();
                }
            }
        }
    }
}