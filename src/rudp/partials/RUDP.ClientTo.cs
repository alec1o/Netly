using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        private class ClientTo : IRUDP.ClientTo
        {
            private readonly Client _client;
            private readonly bool _isServer;
            private Connection _connection { get; set; }
            private int _handshakeTimeout, _noResponseTimeout;
            private bool _isOpeningOrClosing, _isConnecting, _isClosed;
            private Socket _socket;
            private readonly object _locker = new object();

            private ClientTo()
            {
                Host = Host.Default;
                _client = null;
                _socket = null;
                _connection = null;
                _isOpeningOrClosing = false;
                _handshakeTimeout = 5000;
                _noResponseTimeout = 5000;
                _isConnecting = false;
                _isClosed = true;
            }

            public ClientTo(Client client) : this()
            {
                _client = client;
                _isServer = false;
            }

            public ClientTo(Client client, Host host, Socket socket) : this()
            {
                _client = client;
                Host = host;
                _socket = socket;
                _isServer = true;
                InitConnection(ref host);
            }

            public string Id => _connection == null ? string.Empty : _connection.Id;

            public bool IsOpened => GetConnection();

            private bool GetConnection()
            {
                if (_connection == null) return false;
                if (_isServer && _isConnecting) return true;
                return _connection.IsOpened;
            }

            public Host Host { get; private set; }
            private ClientOn On => _client._on;

            public Task Open(Host host)
            {
                if (_isOpeningOrClosing || IsOpened || _isServer) return Task.CompletedTask;
                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    try
                    {
                        if (host == null) throw new NullReferenceException(nameof(host));

                        _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                        On.OnModify?.Invoke(null, _socket);

                        _socket.Connect(host.EndPoint);

                        InitConnection(ref host);
                    }
                    catch (Exception e)
                    {
                        // error on open connection
                        On.OnError?.Invoke(null, e);
                        _connection = null;
                        // logger
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        _isOpeningOrClosing = false;
                    }
                });
            }

            public Task Close()
            {
                if (_isOpeningOrClosing || _isClosed) return Task.CompletedTask;
                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    try
                    {
                        if (!_isServer)
                        {
                            _socket?.Close();
                            _socket?.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        _isClosed = true;

                        lock (_locker)
                        {
                            if (!_isServer) _socket = null;
                            _connection?.Close();
                            _isConnecting = false;
                            _isOpeningOrClosing = false;
                        }

                        On.OnClose?.Invoke(null, null);
                    }
                });
            }

            public void Data(byte[] data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                try
                {
                    _connection?.Send(ref data, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void Data(string data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                try
                {
                    var bytes = data.GetBytes();
                    _connection?.Send(ref bytes, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void Data(string data, MessageType messageType, Encoding encoding)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                try
                {
                    var bytes = data.GetBytes(encoding);
                    _connection?.Send(ref bytes, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void Event(string name, byte[] data, MessageType messageType)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                try
                {
                    var bytes = NetlyEnvironment.EventManager.Create(name, data);
                    _connection?.Send(ref bytes, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void Event(string name, string data, MessageType messageType)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                try
                {
                    var bytes = NetlyEnvironment.EventManager.Create(name, data.GetBytes());
                    _connection?.Send(ref bytes, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void Event(string name, string data, MessageType messageType, Encoding encoding)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                try
                {
                    var bytes = NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding));
                    _connection?.Send(ref bytes, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public int GetHandshakeTimeout()
            {
                return _handshakeTimeout;
            }

            public int GetNoResponseTimeout()
            {
                return _noResponseTimeout;
            }

            public void SetHandshakeTimeout(int value)
            {
                if (IsOpened)
                    throw new Exception
                    (
                        $"Isn't possible use `{nameof(SetHandshakeTimeout)}` while socket is already connected."
                    );

                if (value < 1000)
                    throw new Exception
                    (
                        $"Isn't possible use {nameof(SetHandshakeTimeout)} with value less than `1000`"
                    );

                _handshakeTimeout = value;
            }

            public void SetNoResponseTimeout(int value)
            {
                if (IsOpened)
                    throw new Exception
                    (
                        $"Isn't possible use `{nameof(SetNoResponseTimeout)}` while socket is already connected."
                    );

                if (value < 1000)
                    throw new Exception
                    (
                        $"Isn't possible use {nameof(SetNoResponseTimeout)} with value less than `2000`"
                    );

                _noResponseTimeout = value;
            }

            private void InitConnection(ref Host host)
            {
                var myHost = new Host(host.IPEndPoint);

                _connection = new Connection(host, _socket, _isServer)
                {
                    OnOpen = () =>
                    {
                        // connection opened
                        lock (_locker)
                        {
                            Host = myHost;
                            _isClosed = false;
                            _isConnecting = false;
                            On.OnOpen?.Invoke(null, null);
                        }
                    },
                    OnClose = () =>
                    {
                        lock (_locker)
                        {
                            _isConnecting = false;
                            _connection.Close();
                            Close();
                        }
                    },
                    OnOpenFail = message =>
                    {
                        // error on open connection
                        lock (_locker)
                        {
                            _isClosed = true;
                            _isConnecting = false;
                            _connection.Close();
                            On.OnError?.Invoke(null, new Exception(message));
                        }
                    },
                    OnData = (data, type) =>
                    {
                        // raw data received
                        On.OnData?.Invoke(null, (data, type));
                    },
                    OnEvent = (name, data, type) =>
                    {
                        // event received
                        On.OnEvent?.Invoke(null, (name, data, type));
                    },
                    HandshakeTimeout = GetHandshakeTimeout(),
                    NoResponseTimeout = GetNoResponseTimeout()
                };

                StartConnection();
            }

            private void StartConnection()
            {
                if (!_isServer)
                {
                    _isConnecting = true;
                    InitReceiver();
                    var task = _connection.Open();
                    task.Start();
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    _connection.Open().Start();
                }
            }

            public void InjectBuffer(ref byte[] bytes)
            {
                _connection?.InjectBuffer(ref bytes);
            }

            public void StartServerSideConnection(ref Action<bool> callback)
            {
                _connection.StartServerSideConnection = callback;
            }

            private void InitReceiver()
            {
                var endpoint = Host.EndPoint;

                var buffer = new byte
                [
                    // Maximum/Default receive buffer length.
                    (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                ];

                ReceiverUpdate();

                void ReceiverUpdate()
                {
                    if (!_isConnecting)
                        if (!IsOpened)
                        {
                            Close();
                            return;
                        }

                    _socket.BeginReceiveFrom
                    (
                        buffer,
                        0,
                        buffer.Length,
                        SocketFlags.None,
                        ref endpoint,
                        ReceiveCallback,
                        null
                    );
                }

                void ReceiveCallback(IAsyncResult result)
                {
                    try
                    {
                        var size = _socket.EndReceiveFrom(result, ref endpoint);

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

                        ReceiverUpdate();

                        InjectBuffer(ref data);
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                        if (!_isConnecting) Close();
                    }
                }
            }
        }
    }
}