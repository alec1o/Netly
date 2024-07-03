using System;
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
            private bool _isOpeningOrClosing;
            private readonly bool _isServer;
            private int _openTimeout;
            private ClientOn On => _client._on;

            public ClientTo()
            {
                _client = null;
                _socket = null;
                _connection = null;
                _isOpeningOrClosing = false;
                _openTimeout = 5000;
            }

            public ClientTo(Client client) : this()
            {
                _client = client;
                _isServer = false;
            }

            public ClientTo(Host host, Socket socket) : this()
            {
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
                _connection = new Connection(host, _socket, _isServer)
                {
                    OnOpen = () =>
                    {
                        // connection opened
                        On.OnOpen?.Invoke(null, null);
                    },
                    OnClose = () =>
                    {
                        // connection closed
                        _connection = null;
                        On.OnClose?.Invoke(null, null);
                    },
                    OnOpenFail = (message) =>
                    {
                        // error on open connection
                        _connection = null;
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

            public void StartConnection()
            {
                _connection.Open(_openTimeout).Wait();
            }
        }
    }
}