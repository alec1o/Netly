using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class UDP
    {
        public partial class Server
        {
            private class ServerTo : IUDP.ServerTo
            {
                private readonly Server _server;
                private readonly object _clientsLocker = new object();

                private bool _isClosed, _isOpeningOrClosing;

                private Socket _socket;

                private ServerTo()
                {
                    _server = null;
                    _socket = null;
                    _isClosed = true;
                    _isOpeningOrClosing = false;
                    Host = Host.Default;
                    Clients = new List<Client>();
                }

                public ServerTo(Server server) : this()
                {
                    _server = server;
                }

                private ServerOn On => _server._on;
                public bool IsOpened => !_isClosed && _socket != null;
                public Host Host { get; private set; }
                public List<Client> Clients { get; }

                public Task Open(Host host)
                {
                    if (_isOpeningOrClosing || !_isClosed) return Task.CompletedTask;

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

                    return Task.Run(async () =>
                    {
                        try
                        {
                            _socket?.Shutdown(SocketShutdown.Both);

                            Client[] clients;

                            lock (_clientsLocker)
                            {
                                clients = Clients.ToArray();
                            }

                            foreach (var client in clients)
                            {
                                await client.To.Close();
                            }

                            lock (_clientsLocker)
                            {
                                Clients.Clear();
                            }

                            _socket?.Close();
                            _socket?.Dispose();
                        }
                        catch
                        {
                            // Ignored
                        }
                        finally
                        {
                            _socket = null;
                            _isClosed = true;
                            _isOpeningOrClosing = false;
                            On.OnClose?.Invoke(null, null);
                        }

                        return Task.CompletedTask;
                    });
                }

                public void DataBroadcast(byte[] data)
                {
                    if (!IsOpened || data == null || data.Length <= 0) return;

                    Broadcast(data);
                }

                public void DataBroadcast(string data)
                {
                    if (!IsOpened || string.IsNullOrEmpty(data)) return;

                    Broadcast(data.GetBytes());
                }

                public void DataBroadcast(string data, Encoding encoding)
                {
                    if (!IsOpened || string.IsNullOrEmpty(data)) return;

                    Broadcast(data.GetBytes(encoding));
                }

                public void EventBroadcast(string name, byte[] data)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || data == null || data.Length <= 0) return;

                    Broadcast(NetlyEnvironment.EventManager.Create(name, data));
                }

                public void EventBroadcast(string name, string data)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data)) return;

                    Broadcast(NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
                }

                public void EventBroadcast(string name, string data, Encoding encoding)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data)) return;

                    Broadcast(NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
                }

                public void Data(Host targetHost, byte[] data)
                {
                    if (!IsOpened || targetHost == null || data == null || data.Length <= 0) return;

                    Send(targetHost, data);
                }

                public void Data(Host targetHost, string data)
                {
                    if (!IsOpened || targetHost == null || string.IsNullOrEmpty(data)) return;

                    Send(targetHost, data.GetBytes());
                }

                public void Data(Host targetHost, string data, Encoding encoding)
                {
                    if (!IsOpened || targetHost == null || string.IsNullOrEmpty(data)) return;

                    Send(targetHost, data.GetBytes(encoding));
                }

                public void Event(Host targetHost, string name, byte[] data)
                {
                    if (!IsOpened || targetHost == null || string.IsNullOrEmpty(name) || data == null ||
                        data.Length <= 0) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data));
                }

                public void Event(Host targetHost, string name, string data)
                {
                    if (!IsOpened || targetHost == null || string.IsNullOrEmpty(name) ||
                        string.IsNullOrEmpty(data)) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
                }

                public void Event(Host targetHost, string name, string data, Encoding encoding)
                {
                    if (!IsOpened || targetHost == null || string.IsNullOrEmpty(name) ||
                        string.IsNullOrEmpty(data)) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
                }

                private void Broadcast(byte[] data)
                {
                    if (data == null || data.Length <= 0) return;

                    try
                    {
                        Client[] clients;

                        lock (_clientsLocker)
                        {
                            if (Clients.Count <= 0) return;
                            clients = Clients.ToArray();
                        }

                        foreach (var client in clients)
                            client?.To.Data(data);
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                }

                private void Send(Host host, byte[] bytes)
                {
                    if (bytes == null || bytes.Length <= 0 || !IsOpened || host == null) return;

                    try
                    {
                        _socket?.BeginSendTo
                        (
                            bytes,
                            0,
                            bytes.Length,
                            SocketFlags.None,
                            host.EndPoint,
                            null,
                            null
                        );
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

                    void AcceptUpdate()
                    {
                        if (!IsOpened)
                        {
                            Close();
                            return;
                        }

                        _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEndPoint,
                            AcceptCallback, null);
                    }

                    void AcceptCallback(IAsyncResult result)
                    {
                        try
                        {
                            var size = _socket.EndReceiveFrom(result, ref remoteEndPoint);

                            if (size <= 0)
                            {
                                AcceptUpdate();
                                return;
                            }

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            var newHost = new Host(remoteEndPoint);

                            // Find a client connected user by endpoint connection (IP, PORT)

                            Client client;

                            lock (_clientsLocker)
                            {
                                client = Clients.FirstOrDefault(x => Host.Equals(newHost, x.Host));
                            }

                            if (client == null)
                            {
                                // Create new client
                                client = new Client(ref newHost, ref _socket);
                                client.On.Close(() =>
                                {
                                    lock (_clientsLocker)
                                    {
                                        Clients.Remove(client);
                                        client = null;
                                    }
                                });

                                // save client on list of connected client
                                lock (_clientsLocker)
                                {
                                    Clients.Add(client);
                                }

                                // invoke accept event
                                On.OnAccept?.Invoke(null, client);

                                // init client server-side behave
                                client.InitServerSide();
                            }

                            // publish data for a connected client
                            client.OnServerBuffer(ref data);
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                        }

                        AcceptUpdate();
                    }
                }
            }
        }
    }
}