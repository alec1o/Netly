using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
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
        private RudpConnection _connection;
        private bool _isOpening;
        private int _reopenId;
        private EventHandler<int> _onReopen;
        private int _handshakeTimeout = 5000, _noResponseTimeout = 5000;
        private long _framingSize = Framing.DefaultSize;
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
            ReopenEnabled = false;
            ReopenAttempts = 5;
            ReopenDelay = TimeSpan.FromSeconds(1);
        }

        // client accepted by the server, the connection is created by the server
        internal RudpClient(RudpServer server, Socket socket, RudpConnection connection, Host host) : this()
        {
            _server = server;
            _connection = connection;
            _handshakeTimeout = connection.HandshakeTimeout;
            _noResponseTimeout = connection.NoResponseTimeout;
            _framingSize = connection.MaxMessageSize;
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
        ///     Biggest datagram of the connection: the smaller one between the limits of the sockets of both sides,
        ///     agreed in the handshake (each limit comes from the send and receive buffers of the socket).
        ///     Before the connection is open it is the limit of this side.
        /// </summary>
        public int MaxDatagram => _connection?.MaxDatagram ?? 0;

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

        /// <summary>
        ///     Biggest message accepted, a bigger one closes the connection. Default is 20 MB, minimum 1 KB.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the connection is opened or for a client of a server.</exception>
        public long FramingSize
        {
            get => _framingSize;
            set
            {
                if (IsOpened || IsServer)
                    throw new InvalidOperationException($"Must not update ({nameof(FramingSize)}) while socket is Opened.");

                _framingSize = Math.Max(1024, value);
            }
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

        /// <summary>
        ///     True when the client opens the connection again after it was lost or failed to open.
        ///     Use <see cref="UseAutoReopen" /> to change it.
        /// </summary>
        public bool ReopenEnabled { get; private set; }

        /// <summary>
        ///     How many times the client tries to open again. -1 means no limit.
        /// </summary>
        public int ReopenAttempts { get; private set; }

        /// <summary>
        ///     Time to wait before each attempt to open again.
        /// </summary>
        public TimeSpan ReopenDelay { get; private set; }

        /// <summary>
        ///     Called before each attempt to open again, with the number of the attempt (starts at 1).
        ///     <see cref="OnOpen" /> is called when an attempt works, <see cref="OnError" /> when it fails.
        /// </summary>
        public void OnReopen(Action<int> callback)
        {
            _onReopen += (sender, attempt) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(attempt));
        }

        /// <summary>
        ///     Enables or disables opening the connection again when it is lost or when the first open fails.
        ///     It does not happen after <see cref="Close" /> and it must be called before <see cref="Open" />.
        /// </summary>
        /// <param name="enabled">True to open the connection again.</param>
        /// <param name="attempts">Maximum number of attempts for each failure. -1 means no limit.</param>
        /// <param name="delay">Time to wait before each attempt. Null means one second.</param>
        /// <exception cref="InvalidOperationException">While the connection is opened.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="attempts" /> or <paramref name="delay" /> is invalid.</exception>
        public void UseAutoReopen(bool enabled, int attempts = 5, TimeSpan? delay = null)
        {
            if (IsOpened || IsServer)
                throw new InvalidOperationException($"Must not modify ({nameof(ReopenEnabled)}) while socket is Opened.");

            if (attempts == 0 || attempts < -1)
                throw new ArgumentOutOfRangeException($"{nameof(attempts)}: {attempts}");

            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException($"{nameof(delay)}: {delay}");

            ReopenEnabled = enabled;
            ReopenAttempts = attempts;
            ReopenDelay = delay ?? TimeSpan.FromSeconds(1);
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

            // a new Open replaces any pending reopen
            var id = Interlocked.Increment(ref _reopenId);

            _ = OpenAsync(host, id, false);
        }

        // true when the connection is open, every failure goes to OnError, it never throws
        private Task<bool> OpenAsync(Host host, int id, bool isAttempt)
        {
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task.Run(() =>
            {
                lock (_locker)
                {
                    if (_isOpening || IsServer || IsOpened)
                    {
                        source.TrySetResult(IsOpened);
                        return;
                    }

                    _isOpening = true;
                }

                Socket socket = null;

                try
                {
                    socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _onModify?.Invoke(null, socket);

                    socket.Connect(host.EndPoint);

                    var connection = new RudpConnection(socket, host.IPEndPoint, false)
                    {
                        HandshakeTimeout = _handshakeTimeout,
                        NoResponseTimeout = _noResponseTimeout,
                        MaxMessageSize = _framingSize
                    };

                    connection.OnOpen += () =>
                    {
                        Host = new Host(host.IPEndPoint);
                        _isOpening = false;
                        _onOpen?.Invoke(null, null);
                        source.TrySetResult(true);
                    };

                    connection.OnOpenFail += message =>
                    {
                        _isOpening = false;
                        CloseSocket(socket);

                        var e = new Exception(message);
                        Logger.Singleton.Submit(e);
                        _onError?.Invoke(null, e);
                        source.TrySetResult(false);

                        // an attempt that fails is handled by the loop of the reopen
                        if (isAttempt is false && ReopenEnabled && Volatile.Read(ref _reopenId) == id) Reopen(host, id);
                    };

                    connection.OnClose += () =>
                    {
                        CloseSocket(socket);
                        _onClose?.Invoke(null, null);

                        // closes made by the user change _reopenId
                        if (ReopenEnabled && Volatile.Read(ref _reopenId) == id) Reopen(host, id);
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
                    source.TrySetResult(false);

                    if (isAttempt is false && ReopenEnabled && Volatile.Read(ref _reopenId) == id) Reopen(host, id);
                }
            });

            return source.Task;
        }

        private void Reopen(Host host, int id)
        {
            Task.Run(async () =>
            {
                for (var attempt = 1; ReopenAttempts < 0 || attempt <= ReopenAttempts; attempt++)
                {
                    await Task.Delay(ReopenDelay);

                    if (Volatile.Read(ref _reopenId) != id || IsOpened) return;

                    _onReopen?.Invoke(null, attempt);

                    if (await OpenAsync(host, id, true)) return;
                }
            });
        }

        /// <summary>
        ///     Closes the connection and returns without waiting, <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            // user close never opens again
            Interlocked.Increment(ref _reopenId);

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

        private static void StartReceiver(Socket socket, RudpConnection connection)
        {
            Receive(socket, connection, new byte[connection.MaxDatagram + 64]);
        }

        private static void Receive(Socket socket, RudpConnection connection, byte[] buffer)
        {
            for (;;)
            {
                IAsyncResult result;

                try
                {
                    result = socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveHandler,
                        (socket, connection, buffer));
                }
                catch (Exception)
                {
                    // socket closed
                    return;
                }

                // a receive that completed inside BeginReceive is handled here, a recursion would overflow the stack
                if (result.CompletedSynchronously is false || ReceiveComplete(result) is false) return;
            }
        }

        private static void ReceiveHandler(IAsyncResult result)
        {
            if (result.CompletedSynchronously) return;

            if (ReceiveComplete(result) is false) return;

            var (socket, connection, buffer) = ((Socket, RudpConnection, byte[]))result.AsyncState;
            Receive(socket, connection, buffer);
        }

        // true when the next receive must be started
        private static bool ReceiveComplete(IAsyncResult result)
        {
            var (socket, connection, buffer) = ((Socket, RudpConnection, byte[]))result.AsyncState;

            try
            {
                var size = socket.EndReceive(result);

                if (size > 0) connection.Receive(buffer, size);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (SocketException)
            {
                // icmp error of a previous datagram (server not found yet), keep receiving
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            return !connection.IsClosed;
        }

        // the server calls it when the connection of an accepted client opens
        internal void ServerOpened()
        {
            _onModify?.Invoke(null, Socket);
            _onOpen?.Invoke(null, null);
        }
    }
}
