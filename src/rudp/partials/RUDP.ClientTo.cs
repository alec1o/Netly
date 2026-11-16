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
            private readonly Client _client;
            private readonly bool _isServer;
            private readonly object _locker = new object();
            private Connection _connection;
            private Socket _socket;
            private bool _isOpening;
            private int _handshakeTimeout = 5000, _noResponseTimeout = 5000;

            public ClientTo(Client client)
            {
                _client = client;
                _isServer = false;
                Host = NHost.Default;
            }

            // server side: the connection is created by the server
            public ClientTo(Client client, Connection connection, NHost host)
            {
                _client = client;
                _isServer = true;
                _connection = connection;
                Host = host;
                _handshakeTimeout = connection.HandshakeTimeout;
                _noResponseTimeout = connection.NoResponseTimeout;

                connection.OnMessage += OnMessage;
                connection.OnClose += () => On.OnClose?.Invoke(null, null);
            }

            public string Id => _connection == null ? string.Empty : _connection.Id;
            public bool IsOpened => _connection != null && _connection.IsOpened;
            public NHost Host { get; private set; }
            private ClientOn On => _client._on;

            public Task Open(NHost host)
            {
                lock (_locker)
                {
                    if (_isOpening || _isServer || IsOpened) return Task.CompletedTask;

                    _isOpening = true;
                }

                var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                try
                {
                    if (host == null) throw new ArgumentNullException(nameof(host));

                    var socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _socket = socket;

                    On.OnModify?.Invoke(null, socket);

                    socket.Connect(host.EndPoint);

                    var connection = new Connection(socket, host.IPEndPoint, false)
                    {
                        HandshakeTimeout = _handshakeTimeout,
                        NoResponseTimeout = _noResponseTimeout
                    };

                    connection.OnOpen += () =>
                    {
                        Host = new NHost(host.IPEndPoint);
                        _isOpening = false;
                        On.OnOpen?.Invoke(null, null);
                        source.TrySetResult(true);
                    };

                    connection.OnOpenFail += message =>
                    {
                        _isOpening = false;
                        CloseSocket(socket);
                        On.OnError?.Invoke(null, new Exception(message));
                        source.TrySetResult(false);
                    };

                    connection.OnClose += () =>
                    {
                        CloseSocket(socket);
                        On.OnClose?.Invoke(null, null);
                    };

                    connection.OnMessage += OnMessage;

                    _connection = connection;

                    StartReceiver(socket, connection);
                    connection.StartClient();
                }
                catch (Exception e)
                {
                    NLogger.Singleton.Submit(e);
                    _isOpening = false;
                    CloseSocket(_socket);
                    On.OnError?.Invoke(null, e);
                    source.TrySetResult(false);
                }

                return source.Task;
            }

            public Task Close()
            {
                _connection?.Close();

                return Task.CompletedTask;
            }

            public void Data(byte[] data, MessageType messageType)
            {
                if (!IsOpened || data == null || data.Length <= 0) return;

                Send(data, messageType);
            }

            public void Data(string data, MessageType messageType)
            {
                if (!IsOpened || string.IsNullOrEmpty(data)) return;

                Send(data.GetBytes(), messageType);
            }

            public void Data(string data, MessageType messageType, Encoding encoding)
            {
                if (!IsOpened || string.IsNullOrEmpty(data)) return;

                Send(data.GetBytes(encoding), messageType);
            }

            public void Event(string name, byte[] data, MessageType messageType)
            {
                if (!IsOpened || string.IsNullOrEmpty(name) || data == null || data.Length <= 0) return;

                Send(NHelper.ArrayConcat(NMessage.Create(name, data.LongLength), data), messageType);
            }

            public void Event(string name, string data, MessageType messageType)
            {
                if (string.IsNullOrEmpty(data)) return;

                Event(name, data.GetBytes(), messageType);
            }

            public void Event(string name, string data, MessageType messageType, Encoding encoding)
            {
                if (string.IsNullOrEmpty(data)) return;

                Event(name, data.GetBytes(encoding), messageType);
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

            /* ---- INTERFACE --- */

            private void Send(byte[] data, MessageType messageType)
            {
                try
                {
                    _connection?.Send(data, messageType);
                }
                catch (Exception e)
                {
                    NLogger.Singleton.Submit(e);
                }
            }

            private void OnMessage(byte[] data, MessageType messageType)
            {
                var stream = NHelper.NewStream(data.LongLength);

                stream.Write(data, 0, data.Length);
                stream.Position = 0;

                if (NMessage.TryParse(stream, out var name, out var message, NHelper.NewStream))
                {
                    stream.Close();
                    On.OnEvent?.Invoke(null, (name, message, messageType));
                }
                else
                {
                    stream.Position = 0;
                    On.OnData?.Invoke(null, (stream, messageType));
                }
            }

            private static void CloseSocket(Socket socket)
            {
                try
                {
                    socket?.Close();
                }
                catch (Exception e)
                {
                    NLogger.Singleton.Submit(e);
                }
            }

            private static void StartReceiver(Socket socket, Connection connection)
            {
                var buffer = new byte[connection.MaxDatagram + 64];

                Receive();

                void Receive()
                {
                    try
                    {
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Callback, null);
                    }
                    catch (Exception)
                    {
                        // socket closed
                    }
                }

                void Callback(IAsyncResult result)
                {
                    try
                    {
                        var size = socket.EndReceive(result);

                        if (size > 0) connection.Receive(buffer, size);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (SocketException)
                    {
                        // icmp error of a previous datagram (server not found yet), keep receiving
                    }
                    catch (Exception e)
                    {
                        NLogger.Singleton.Submit(e);
                    }

                    if (!connection.IsClosed) Receive();
                }
            }
        }
    }
}
