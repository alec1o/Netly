using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            private class Session
            {
                public Connection Connection;
                public Client Client;
            }

            private readonly Dictionary<IPEndPoint, Session> _sessions = new Dictionary<IPEndPoint, Session>();
            private readonly object _locker = new object();
            private readonly Server _server;
            private bool _isOpeningOrClosing, _isClosed;
            private Socket _socket;
            private int _handshakeTimeout = 5000;
            private int _noResponseTimeout = 5000;

            public ServerTo(Server server)
            {
                Host = NHost.Default;
                _server = server;
                _socket = null;
                _isOpeningOrClosing = false;
                _isClosed = true;
            }

            public NHost Host { get; private set; }
            public bool IsOpened => _socket != null && !_isClosed;
            private ServerOn On => _server._on;

            public Task Open(NHost host)
            {
                lock (_locker)
                {
                    if (!_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                    _isOpeningOrClosing = true;
                }

                try
                {
                    var socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    On.OnModify?.Invoke(null, socket);

                    Tune(socket);

                    socket.Bind(host.EndPoint);

                    Host = new NHost(socket.LocalEndPoint);

                    _socket = socket;
                    _isClosed = false;

                    new Thread(() => Accept(socket)) { IsBackground = true }.Start();

                    On.OnOpen?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    NLogger.Singleton.Submit(e);
                    _isClosed = true;
                    On.OnError?.Invoke(null, e);
                }
                finally
                {
                    _isOpeningOrClosing = false;
                }

                return Task.CompletedTask;
            }

            public Task Close()
            {
                Session[] sessions;
                Socket socket;

                lock (_locker)
                {
                    if (_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                    _isOpeningOrClosing = true;
                    _isClosed = true;
                    socket = _socket;
                    sessions = _sessions.Values.ToArray();
                    _sessions.Clear();
                }

                try
                {
                    // clients receive the close notice before the socket is closed
                    foreach (var session in sessions) session.Connection.Close();

                    socket?.Close();
                }
                catch (Exception e)
                {
                    NLogger.Singleton.Submit(e);
                }
                finally
                {
                    _socket = null;
                    _isOpeningOrClosing = false;
                    On.OnClose?.Invoke(null, null);
                }

                return Task.CompletedTask;
            }

            public void DataBroadcast(byte[] data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                Broadcast(data, messageType);
            }

            public void DataBroadcast(string data, MessageType messageType)
            {
                if (string.IsNullOrEmpty(data)) return;

                DataBroadcast(data.GetBytes(), messageType);
            }

            public void DataBroadcast(string data, MessageType messageType, Encoding encoding)
            {
                if (string.IsNullOrEmpty(data)) return;

                DataBroadcast(data.GetBytes(encoding), messageType);
            }

            public void EventBroadcast(string name, byte[] data, MessageType messageType)
            {
                if (!IsOpened || string.IsNullOrEmpty(name) || data == null || data.Length <= 0) return;

                Broadcast(NHelper.ArrayConcat(NMessage.Create(name, data.LongLength), data), messageType);
            }

            public void EventBroadcast(string name, string data, MessageType messageType)
            {
                if (string.IsNullOrEmpty(data)) return;

                EventBroadcast(name, data.GetBytes(), messageType);
            }

            public void EventBroadcast(string name, string data, MessageType messageType, Encoding encoding)
            {
                if (string.IsNullOrEmpty(data)) return;

                EventBroadcast(name, data.GetBytes(encoding), messageType);
            }

            public IRUDP.Client[] GetClients()
            {
                lock (_locker)
                {
                    return _sessions.Values.Where(x => x.Connection.IsOpened).Select(x => (IRUDP.Client)x.Client)
                        .ToArray();
                }
            }

            private void Broadcast(byte[] data, MessageType messageType)
            {
                Session[] sessions;

                lock (_locker)
                {
                    sessions = _sessions.Values.ToArray();
                }

                foreach (var session in sessions)
                    try
                    {
                        session.Connection.Send(data, messageType);
                    }
                    catch (Exception e)
                    {
                        NLogger.Singleton.Submit(e);
                    }
            }

            /* ---- ACCEPT --- */

            private static void Tune(Socket socket)
            {
                try
                {
                    socket.ReceiveBufferSize = 4 * 1024 * 1024;
                    socket.SendBufferSize = 4 * 1024 * 1024;
                }
                catch (Exception)
                {
                    // the system keeps the default size
                }

                try
                {
                    // windows: don't fail the receive when a client is gone (icmp port unreachable)
                    socket.IOControl(-1744830452, new byte[] { 0 }, null);
                }
                catch (Exception)
                {
                    // not windows
                }
            }

            private void Accept(Socket socket)
            {
                var buffer = new byte[ushort.MaxValue];
                var any = socket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;

                while (!_isClosed)
                    try
                    {
                        EndPoint endpoint = new IPEndPoint(any, 0);

                        var size = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        if (size > 0) Dispatch((IPEndPoint)endpoint, buffer, size);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException e)
                    {
                        // client gone (connection reset), the socket still works
                        if (_isClosed || e.SocketErrorCode == SocketError.Interrupted) break;
                    }
                    catch (Exception e)
                    {
                        NLogger.Singleton.Submit(e);
                    }
            }

            private void Dispatch(IPEndPoint endpoint, byte[] data, int size)
            {
                Session session;

                lock (_locker)
                {
                    _sessions.TryGetValue(endpoint, out session);
                }

                if (session != null)
                {
                    session.Connection.Receive(data, size);
                    return;
                }

                // only a valid syn creates a connection
                if (!Wire.IsSyn(data, size)) return;

                var remote = new IPEndPoint(endpoint.Address, endpoint.Port);
                var connection = new Connection(_socket, remote, true)
                {
                    HandshakeTimeout = _handshakeTimeout,
                    NoResponseTimeout = _noResponseTimeout
                };
                var client = new Client(connection, new NHost(remote));

                session = new Session { Connection = connection, Client = client };

                connection.OnOpen += () =>
                {
                    On.OnAccept?.Invoke(null, client);
                    client._on.OnModify?.Invoke(null, _socket);
                    client._on.OnOpen?.Invoke(null, null);
                };
                connection.OnOpenFail += _ => Remove(remote, connection);
                connection.OnClose += () => Remove(remote, connection);

                lock (_locker)
                {
                    if (_isClosed) return;

                    _sessions[remote] = session;
                }

                connection.Receive(data, size);
            }

            private void Remove(IPEndPoint endpoint, Connection connection)
            {
                lock (_locker)
                {
                    if (_sessions.TryGetValue(endpoint, out var session) && session.Connection == connection)
                        _sessions.Remove(endpoint);
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
                        $"Isn't possible use {nameof(SetNoResponseTimeout)} with value less than `1000`"
                    );

                _noResponseTimeout = value;
            }
        }
    }
}
