using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class RUDP
    {
        private class ServerTo : IRUDP.ServerTo
        {
            private readonly List<Client> _clients;
            private readonly object _clientsLocker, _contentsLooker;
            private readonly List<(Host host, byte[] data)> _contents;
            private readonly Server _server;
            private bool _isOpeningOrClosing, _isClosed;
            private Socket _socket;
            private int _handshakeTimeout;
            private int _noResponseTimeout;
            private const bool UseConnectionSecurity = true;

            public ServerTo(Server server)
            {
                Host = Host.Default;
                _server = server;
                _clients = new List<Client>();
                _contents = new List<(Host host, byte[] data)>();
                _clientsLocker = new object();
                _contentsLooker = new object();
                _socket = null;
                _isOpeningOrClosing = false;
                _isClosed = true;
                _handshakeTimeout = 5000; // 5s 
                _noResponseTimeout = 5000; // 5s
            }

            public Host Host { get; private set; }
            public bool IsOpened => _socket != null && !_isClosed;
            private ServerOn On => _server._on;

            public Task Open(Host host)
            {
                if (!_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    try
                    {
                        _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                        On.OnModify?.Invoke(null, _socket);

                        _socket.Bind(host.EndPoint);

                        Host = new Host(_socket.LocalEndPoint);

                        _isClosed = false;

                        InitAccept();

                        On.OnOpen?.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        _isClosed = true;
                        On.OnError?.Invoke(null, e);
                    }
                    finally
                    {
                        _isOpeningOrClosing = false;
                    }
                });
            }

            public Task Close()
            {
                if (_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    try
                    {
                        _socket?.Shutdown(SocketShutdown.Both);

                        Client[] clients;

                        lock (_clientsLocker)
                        {
                            clients = _clients.ToArray();
                            _clients.Clear();
                        }

                        if (clients.Length > 0)
                            foreach (var client in clients)
                                client.To.Close().Wait();

                        _socket?.Close();
                        _socket?.Dispose();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                    finally
                    {
                        _socket = null;
                        _isClosed = true;
                        _isOpeningOrClosing = false;
                        On.OnClose?.Invoke(null, null);
                    }
                });
            }

            public void DataBroadcast(byte[] data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                Broadcast(data, messageType);
            }

            public void DataBroadcast(string data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                Broadcast(data.GetBytes(), messageType);
            }

            public void DataBroadcast(string data, MessageType messageType, Encoding encoding)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                Broadcast(data.GetBytes(encoding), messageType);
            }

            public void EventBroadcast(string name, byte[] data, MessageType messageType)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                Broadcast(name, data, messageType);
            }

            public void EventBroadcast(string name, string data, MessageType messageType)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                Broadcast(name, data.GetBytes(), messageType);
            }

            public void EventBroadcast(string name, string data, MessageType messageType, Encoding encoding)
            {
                if (!IsOpened || name == null || name.Length <= 0 || data == null || data.Length <= 0) return;

                Broadcast(name, data.GetBytes(encoding), messageType);
            }

            public IRUDP.Client[] GetClients()
            {
                lock (_clientsLocker)
                {
                    return _clients.Where(x => x.IsOpened).Select(x => (IRUDP.Client)x).ToArray();
                }
            }

            private void Broadcast(byte[] data, MessageType messageType)
            {
                try
                {
                    Client[] clients;

                    lock (_clientsLocker)
                    {
                        clients = _clients.ToArray();
                    }

                    if (clients.Length > 0)
                        foreach (var client in clients)
                            client?.To.Data(data, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            private void Broadcast(string name, byte[] data, MessageType messageType)
            {
                try
                {
                    Client[] clients;

                    lock (_clientsLocker)
                    {
                        clients = _clients.ToArray();
                    }

                    if (clients.Length > 0)
                        foreach (var client in clients)
                            client?.To.Data(data, messageType);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }


            private void InitAccept()
            {
                var length = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                var buffer = new byte[length > 0 ? length : 4096];
                var remoteEndPoint = Host.EndPoint;

                AcceptUpdate();

                new Thread(ContentUpdate) { IsBackground = true }.Start();

                void AcceptUpdate()
                {
                    if (!IsOpened)
                    {
                        Close();
                        return;
                    }

                    _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEndPoint,
                        AcceptCallback,
                        null);
                }

                void AcceptCallback(IAsyncResult result)
                {
                    try
                    {
                        var size = _socket.EndReceiveFrom(result, ref remoteEndPoint);

                        if (size > 0)
                        {
                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            lock (_contentsLooker)
                            {
                                _contents.Add((new Host(remoteEndPoint), data));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }

                    AcceptUpdate();
                }
            }

            private void ContentUpdate()
            {
                while (IsOpened)
                {
                    // ReSharper disable once InconsistentlySynchronizedField
                    if (_contents.Count <= 0) continue;

                    (Host host, byte[] data) value;
                    Client client;

                    lock (_contentsLooker)
                    {
                        // recheck on thread safe area
                        if (_contents.Count <= 0) continue;

                        // get first element
                        value = _contents[0];

                        // remove first element
                        _contents.RemoveAt(0);
                    }

                    try
                    {
                        lock (_clientsLocker)
                        {
                            // find client
                            client = _clients.FirstOrDefault(x => value.host.Equals(x.Host));
                        }

                        var buffer = value.data;

                        // use existent context
                        if (client != null)
                        {
                            client.InjectBuffer(ref buffer);
                            continue;
                        }

                        #region Prevent Invalid Data

                        if (UseConnectionSecurity)
                        {
                            // detect expired data
                            if (value.data.Length == 1)
                            {
                                if (value.data[0] == Channel.PingPackageBytes[0])
                                {
                                    // this client already disconnected, can't receive ping
                                    continue;
                                }

                                if (value.data[0] == Channel.ClosePackageBytes[0])
                                {
                                    // this client already disconnected, ignore this package
                                    continue;
                                }
                            }

                            // detect if is connection package
                            /*
                            if (!Channel.IsValidEntryPoint(value.data))
                            {
                                continue;
                            }
                            */
                        }

                        #endregion

                        // create new context
                        client = new Client(value.host, _socket)
                        {
                            HandshakeTimeout = GetHandshakeTimeout(),
                            NoResponseTimeout = GetNoResponseTimeout()
                        };

                        // save context
                        lock (_clientsLocker)
                        {
                            _clients.Add(client);
                        }

                        client.StartServerSideConnection(isConnected =>
                        {
                            if (!isConnected)
                            {
                                // remove client from client list
                                lock (_clientsLocker)
                                {
                                    _clients.Remove(client);
                                }

                                client.To.Close();
                            }
                            else // connected successful
                            {
                                client.On.Close(() =>
                                {
                                    // connection closed
                                    lock (_clientsLocker)
                                    {
                                        _clients.Remove(client);
                                    }

                                    client = null;
                                });

                                // invoke new client
                                On.OnAccept?.Invoke(null, client);
                            }
                        });

                        if (client.IsOpened)
                        {
                            client.InjectBuffer(ref buffer);
                        }
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
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
        }
    }
}