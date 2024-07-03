﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        private class ClientTo : IRUDP.ClientTo
        {
            public bool IsOpened => _connection != null && _connection.IsOpened;
            public Host Host { get; private set; }
            private Client _client;
            private Socket _socket;
            private Connection _connection;
            private bool _isOpeningOrClosing, _isConnecting;
            private readonly bool _isServer;
            private int _openTimeout;
            private ClientOn On => _client._on;

            private ClientTo()
            {
                Host = Host.Default;
                _client = null;
                _socket = null;
                _connection = null;
                _isOpeningOrClosing = false;
                _openTimeout = 5000;
                _isConnecting = false;
            }

            public ClientTo(Client client) : this()
            {
                _client = client;
                _isServer = false;
            }

            public ClientTo(Client client, Host host, Socket socket) : this()
            {
                _client = client;
                _socket = socket;
                _isServer = true;
                InitConnection(ref host);
            }

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

                        InitConnection(ref host);

                        StartConnection();
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
                if (_isOpeningOrClosing || !IsOpened) return Task.CompletedTask;
                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    try
                    {
                        _connection?.Close().Wait();
                        _socket?.Dispose();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        _connection = null;
                        _socket = null;
                        _isOpeningOrClosing = false;
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

            public int GetOpenTimeout()
            {
                return _openTimeout;
            }

            public void SetOpenTimeout(int value)
            {
                if (IsOpened)
                    throw new Exception
                    (
                        $"Isn't possible use `{nameof(SetOpenTimeout)}` while socket is already connected."
                    );

                if (value < 1000)
                    throw new Exception
                    (
                        $"Isn't possible use {nameof(SetOpenTimeout)} with value less than `1000`"
                    );

                _openTimeout = value;
            }

            private void InitConnection(ref Host host)
            {
                var nextHost = host;

                _connection = new Connection(host, _socket, _isServer)
                {
                    OnOpen = () =>
                    {
                        // connection opened
                        Host = nextHost;
                        _isConnecting = false;
                        On.OnOpen?.Invoke(null, null);
                    },
                    OnClose = () =>
                    {
                        // connection closed
                        _connection = null;
                        _isConnecting = false;
                        On.OnClose?.Invoke(null, null);
                    },
                    OnOpenFail = message =>
                    {
                        // error on open connection
                        _connection = null;
                        _isConnecting = false;
                        On.OnError?.Invoke(null, new Exception(message));
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
                    }
                };
            }

            private void StartConnection()
            {
                if (!_isServer)
                {
                    _isConnecting = true;
                    InitReceiver();
                }

                _connection.Open(_openTimeout).Wait();
            }

            public void InjectBuffer(ref byte[] bytes)
            {
                if (IsOpened)
                {
                    _connection?.InjectBuffer(bytes);
                }
            }

            public void StartServerSideConnection(ref Action<bool> callback)
            {
                throw new NotImplementedException();
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
                    {
                        if (!IsOpened)
                        {
                            Close();
                            return;
                        }
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
                        if (!_isConnecting)
                        {
                            Close();
                        }
                    }
                }
            }
        }
    }
}