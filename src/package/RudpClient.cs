using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     RUDP client (reliable UDP), also the type of each client accepted by <see cref="RudpServer" />.
    /// </summary>
    public class RudpClient
    {
        private readonly object _locker = new object();
        private readonly RudpServer _server;
        private RUDP.Connection _connection;
        private bool _isOpening;
        private int _handshakeTimeout = 5000, _noResponseTimeout = 5000;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<(string Name, Stream Stream, RudpDelivery Delivery)> _onEvent;
        private EventHandler<Exception> _onError;
        private EventHandler<(Stream Stream, RudpDelivery Delivery)> _onData;
        private Func<long, Stream> _onAllocate;
        private Func<long, Stream> Allocate => IsServer ? _server.OnAllocateObject : _onAllocate;

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a client.
        /// </summary>
        public RudpClient()
        {
            _server = null;
            _onAllocate = Helper.NewStream;
            Host = Host.Default;
            IsServer = false;
            Socket = null;
        }

        // client accepted by the server, the connection is created by the server
        internal RudpClient(RudpServer server, Socket socket, RUDP.Connection connection, Host host) : this()
        {
            _server = server;
            _connection = connection;
            _handshakeTimeout = connection.HandshakeTimeout;
            _noResponseTimeout = connection.NoResponseTimeout;
            Socket = socket;
            Host = host;
            IsServer = true;

            connection.OnMessage += OnMessage;
            connection.OnClose += () => _onClose?.Invoke(null, null);
        }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of the connection, empty until it is open.
        /// </summary>
        public string Id => _connection == null ? string.Empty : _connection.Id;

        /// <summary>
        ///     True while the connection is established.
        /// </summary>
        public bool IsOpened => _connection != null && _connection.IsOpened;

        /// <summary>
        ///     True when this client was accepted by a <see cref="RudpServer" />.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        ///     The socket. For a client of a server it is the socket of the server.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        ///     Address of the other side.
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        ///     Milliseconds to wait for the handshake, minimum 1000. Default is 5000.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the connection is opened.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the value is less than 1000.</exception>
        public int HandshakeTimeout
        {
            get => _handshakeTimeout;
            set => _handshakeTimeout = Timeout(value, nameof(HandshakeTimeout));
        }

        /// <summary>
        ///     Milliseconds without any packet from the other side before the connection is closed, minimum 1000.
        ///     Default is 5000.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the connection is opened.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the value is less than 1000.</exception>
        public int NoResponseTimeout
        {
            get => _noResponseTimeout;
            set => _noResponseTimeout = Timeout(value, nameof(NoResponseTimeout));
        }

        private int Timeout(int value, string name)
        {
            if (IsOpened || IsServer)
                throw new InvalidOperationException($"Must not modify ({name}) while socket is Opened.");

            if (value < 1000)
                throw new ArgumentOutOfRangeException($"{name}: {value}");

            return value;
        }

        /// <summary>
        ///     Called with the new socket before it connects, change the socket options here.
        /// </summary>
        public void OnModify(Action<Socket> callback)
        {
            _onModify += (sender, socket) => callback?.Invoke(socket);
        }

        /// <summary>
        ///     Called when the connection is established.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _onOpen += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the connection is closed, by this side or by the peer.
        /// </summary>
        public void OnClose(Action callback)
        {
            _onClose += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the connection fails to open, when a send fails or when a callback throws while receiving.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /// <summary>
        ///     Called for each received message with the way it was delivered.
        ///     The stream starts at position 0 and is disposed when the callback returns.
        /// </summary>
        public void OnData(Action<Stream, RudpDelivery> callback)
        {
            _onData += (sender, data) => callback?.Invoke(data.Stream, data.Delivery);
        }

        /// <summary>
        ///     Called for each received named event, same stream rules of <see cref="OnData" />.
        /// </summary>
        public void OnEvent(Action<string, Stream, RudpDelivery> callback)
        {
            _onEvent += (sender, data) => callback?.Invoke(data.Name, data.Stream, data.Delivery);
        }

        /// <summary>
        ///     Chooses where each received message is written, receives the size of the message.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the connection is opened.</exception>
        public void OnAllocate(Func<long, Stream> callback)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not update ({nameof(OnAllocate)}) while socket is Opened.");

            _onAllocate = callback ?? Helper.NewStream;
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data from <see cref="System.IO.Stream.Position" /> to the end, the stream is not closed
        ///     and its position is restored.
        /// </summary>
        /// <exception cref="ArgumentException">When the stream can't seek.</exception>
        public void SendData(Stream data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            Send(null, Join(data), delivery);
        }

        /// <summary>
        ///     Sends the whole array.
        /// </summary>
        public void SendData(byte[] data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            SendData(data, 0, data.Length, delivery);
        }

        /// <summary>
        ///     Sends <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendData(byte[] data, int offset, int count, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            SendData(Segment(data, offset, count), delivery);
        }

        /// <summary>
        ///     Sends the segment.
        /// </summary>
        public void SendData(ArraySegment<byte> data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            SendData(new[] { data }, delivery);
        }

        /// <summary>
        ///     Sends all segments as one message.
        /// </summary>
        public void SendData(IList<ArraySegment<byte>> data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            Send(null, Join(data), delivery);
        }

        /// <summary>
        ///     Sends an event without content.
        /// </summary>
        public void SendEvent(string name, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            Send(Header(name, 0), Array.Empty<byte>(), delivery);
        }

        /// <summary>
        ///     Sends a named event with the content from the current position to the end of the stream.
        ///     <br />
        ///     The stream is not closed and its position is restored after sending.
        /// </summary>
        public void SendEvent(string name, Stream data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            var body = Join(data);

            Send(Header(name, body.Length), body, delivery);
        }

        /// <summary>
        ///     Sends a named event with the whole array as content.
        /// </summary>
        public void SendEvent(string name, byte[] data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            SendEvent(name, data, 0, data.Length, delivery);
        }

        /// <summary>
        ///     Sends a named event with <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendEvent(string name, byte[] data, int offset, int count,
            RudpDelivery delivery = RudpDelivery.Reliable)
        {
            SendEvent(name, Segment(data, offset, count), delivery);
        }

        /// <summary>
        ///     Sends a named event with the segment as content.
        /// </summary>
        public void SendEvent(string name, ArraySegment<byte> data, RudpDelivery delivery = RudpDelivery.Reliable)
        {
            SendEvent(name, new[] { data }, delivery);
        }

        /// <summary>
        ///     Sends a named event with all segments as content.
        /// </summary>
        public void SendEvent(string name, IList<ArraySegment<byte>> data,
            RudpDelivery delivery = RudpDelivery.Reliable)
        {
            var body = Join(data);

            Send(Header(name, body.Length), body, delivery);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts the connection and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Host host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            Task.Run(() =>
            {
                lock (_locker)
                {
                    if (_isOpening || IsServer || IsOpened) return;

                    _isOpening = true;
                }

                Socket socket = null;

                try
                {
                    socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _onModify?.Invoke(null, socket);

                    socket.Connect(host.EndPoint);

                    var connection = new RUDP.Connection(socket, host.IPEndPoint, false)
                    {
                        HandshakeTimeout = _handshakeTimeout,
                        NoResponseTimeout = _noResponseTimeout
                    };

                    connection.OnOpen += () =>
                    {
                        Host = new Host(host.IPEndPoint);
                        _isOpening = false;
                        _onOpen?.Invoke(null, null);
                    };

                    connection.OnOpenFail += message =>
                    {
                        _isOpening = false;
                        CloseSocket(socket);

                        var e = new Exception(message);
                        Logger.Singleton.Submit(e);
                        _onError?.Invoke(null, e);
                    };

                    connection.OnClose += () =>
                    {
                        CloseSocket(socket);
                        _onClose?.Invoke(null, null);
                    };

                    connection.OnMessage += OnMessage;

                    lock (_locker)
                    {
                        Socket = socket;
                        _connection = connection;
                    }

                    StartReceiver(socket, connection);
                    connection.StartClient();
                }
                catch (Exception e)
                {
                    _isOpening = false;
                    CloseSocket(socket);

                    Logger.Singleton.Submit(e);
                    _onError?.Invoke(null, e);
                }
            });
        }

        /// <summary>
        ///     Closes the connection and returns without waiting, <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            _connection?.Close();
        }

        /* ---- SEND HELPERS --- */

        private static ArraySegment<byte> Segment(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (offset < 0 || count < 0 || offset > data.Length - count)
                throw new ArgumentOutOfRangeException($"{nameof(offset)}: {offset}, {nameof(count)}: {count}");

            return new ArraySegment<byte>(data, offset, count);
        }

        private static byte[] Header(string name, long size)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(name)}: {name}");

            return Message.Create(name, size);
        }

        private static byte[] Join(IList<ArraySegment<byte>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var result = new byte[data.Sum(x => (long)x.Count)];
            var offset = 0;

            foreach (var segment in data)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, result, offset, segment.Count);
                offset += segment.Count;
            }

            return result;
        }

        private static byte[] Join(Stream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.CanSeek is false)
                throw new ArgumentException($"{nameof(data)}: {nameof(data.CanSeek)} = {data.CanSeek}");

            var start = data.Position;
            var result = new byte[data.Length - start];

            try
            {
                for (var offset = 0; offset < result.Length;)
                {
                    var read = data.Read(result, offset, result.Length - offset);

                    if (read <= 0) throw new EndOfStreamException($"{nameof(data)}: {data.Position}");

                    offset += read;
                }
            }
            finally
            {
                // allow send the same stream again
                data.Position = start;
            }

            return result;
        }

        private void Send(byte[] header, byte[] body, RudpDelivery delivery)
        {
            // an event can be empty, data can't
            if (body.Length < 1 && header == null) return;

            try
            {
                var connection = _connection;

                if (connection == null || connection.IsOpened is false) return;

                connection.Send(header == null ? body : Helper.ArrayConcat(header, body), delivery);
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                _onError?.Invoke(null, e);
            }
        }

        /* ---- RECEIVE --- */

        private void OnMessage(byte[] data, RudpDelivery delivery)
        {
            try
            {
                var stream = Allocate(data.LongLength);

                stream.Write(data, 0, data.Length);

                if (Message.TryParse(stream, out var name, out var message, Allocate))
                {
                    stream.Dispose();
                    Release(message, () => _onEvent?.Invoke(null, (name, message, delivery)));
                }
                else
                {
                    Release(stream, () => _onData?.Invoke(null, (stream, delivery)));
                }
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                _onError?.Invoke(null, e);
            }
        }

        // stream is valid only during the callback
        private static void Release(Stream stream, Action callback)
        {
            try
            {
                if (stream.CanSeek) stream.Position = 0;

                callback();
            }
            finally
            {
                stream.Dispose();
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
                Logger.Singleton.Submit(e);
            }
        }

        private static void StartReceiver(Socket socket, RUDP.Connection connection)
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
                    Logger.Singleton.Submit(e);
                }

                if (!connection.IsClosed) Receive();
            }
        }

        // the server calls it when the connection of an accepted client opens
        internal void ServerOpened()
        {
            _onModify?.Invoke(null, Socket);
            _onOpen?.Invoke(null, null);
        }
    }
}
