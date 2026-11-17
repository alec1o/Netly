using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     UDP client, also the type of each peer known by <see cref="UdpServer" />.
    /// </summary>
    public class UdpClient
    {
        /// <summary>
        ///     Biggest datagram the protocol allows (65535 - 8 bytes UDP header - 20 bytes IP header).
        ///     The real limit of a socket is <see cref="MaxDatagram" />, the system can allow less.
        /// </summary>
        public const int MaxDatagramSize = 65507;

        private readonly object _locker = new object();
        private readonly UdpServer _server;
        private readonly Dictionary<ushort, Assembly> _assemblies = new Dictionary<ushort, Assembly>();
        private int _maxDatagram = MaxDatagramSize;
        private int _messageId;
        private bool _isOpening, _isClosing;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<(string Name, Stream Stream)> _onEvent;
        private EventHandler<Exception> _onError;
        private EventHandler<Stream> _onData;
        private Func<long, Stream> _onAllocate;
        private Func<long, Stream> Allocate => IsServer ? _server.OnAllocateObject : _onAllocate;

        private const long FragmentLifetime = 2000;
        private const int MaxAssemblies = 16;

        // fragments of a message that is not complete, the others are dropped when it takes too long
        private class Assembly
        {
            public byte[][] Parts;
            public int Received, Length;
            public long Deadline;
        }

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a client.
        /// </summary>
        public UdpClient()
        {
            _server = null;
            _onAllocate = Helper.NewStream;
            Host = Host.Default;
            IsOpened = false;
            IsServer = false;
            Socket = null;
        }

        internal UdpClient(UdpServer server, Socket socket, Host host) : this()
        {
            _server = server;
            Socket = socket;
            Host = host;
            IsServer = true;
            IsOpened = true;
            _maxDatagram = LimitOf(socket, host.Address);
        }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the socket is open.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        ///     True when this client is a peer known by an <see cref="UdpServer" />.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        ///     Biggest datagram this socket sends: <see cref="MaxDatagramSize" />, or less when the system limits it:
        ///     the send buffer (macOS only sends up to its send buffer, 9216 bytes by default) or, for a loopback
        ///     address, the biggest datagram that was seen to arrive (some systems drop the bigger ones, without any error).
        ///     A message bigger than this is sent in fragments (up to <see cref="MaxDatagramSize" /> bytes) and joined by the
        ///     receiver, a message that fits it is sent as one datagram, so it works with any other UDP program.
        /// </summary>
        public int MaxDatagram => _maxDatagram;

        /// <summary>
        ///     The socket. For a client of a server it is the socket of the server.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        ///     Address of the other side.
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        ///     Called with the new socket before it connects, change the socket options here.
        /// </summary>
        public void OnModify(Action<Socket> callback)
        {
            _onModify += (sender, socket) => callback?.Invoke(socket);
        }

        /// <summary>
        ///     Called when the socket is open.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _onOpen += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the socket is closed.
        /// </summary>
        public void OnClose(Action callback)
        {
            _onClose += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the socket fails to open (then <see cref="OnClose" /> is called), when a send fails, or when an
        ///     allocation or a callback of the app throws while receiving (the datagram is discarded and the socket closes).
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /// <summary>
        ///     Called for each received datagram.
        ///     The stream starts at position 0 and is disposed when the callback returns.
        /// </summary>
        public void OnData(Action<Stream> callback)
        {
            _onData += (sender, stream) => callback?.Invoke(stream);
        }

        /// <summary>
        ///     Called for each received named event, same stream rules of <see cref="OnData" />.
        /// </summary>
        public void OnEvent(Action<string, Stream> callback)
        {
            _onEvent += (sender, data) => callback?.Invoke(data.Name, data.Stream);
        }

        /// <summary>
        ///     Chooses where each received datagram is written, receives the size of the datagram.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
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
        /// <exception cref="ArgumentException">When the stream can't seek or the message is bigger than <see cref="MaxDatagramSize" />.</exception>
        public void SendData(Stream data)
        {
            Send(null, data);
        }

        /// <summary>
        ///     Sends the whole array.
        /// </summary>
        /// <exception cref="ArgumentException">When the message is bigger than <see cref="MaxDatagramSize" />.</exception>
        public void SendData(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            SendData(data, 0, data.Length);
        }

        /// <summary>
        ///     Sends <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendData(byte[] data, int offset, int count)
        {
            SendData(Segment(data, offset, count));
        }

        /// <summary>
        ///     Sends the segment.
        /// </summary>
        public void SendData(ArraySegment<byte> data)
        {
            SendData(new[] { data });
        }

        /// <summary>
        ///     Sends all segments as one datagram.
        /// </summary>
        public void SendData(IList<ArraySegment<byte>> data)
        {
            Send(null, data);
        }

        /// <summary>
        ///     Sends an event without content.
        /// </summary>
        public void SendEvent(string name)
        {
            Send(Header(name, 0), new ArraySegment<byte>[0]);
        }

        /// <summary>
        ///     Sends a named event with the content from the current position to the end of the stream.
        ///     <br />
        ///     The stream is not closed and its position is restored after sending.
        /// </summary>
        public void SendEvent(string name, Stream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Send(Header(name, data.Length - data.Position), data);
        }

        /// <summary>
        ///     Sends a named event with the whole array as content.
        /// </summary>
        public void SendEvent(string name, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            SendEvent(name, data, 0, data.Length);
        }

        /// <summary>
        ///     Sends a named event with <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendEvent(string name, byte[] data, int offset, int count)
        {
            SendEvent(name, Segment(data, offset, count));
        }

        /// <summary>
        ///     Sends a named event with the segment as content.
        /// </summary>
        public void SendEvent(string name, ArraySegment<byte> data)
        {
            SendEvent(name, new[] { data });
        }

        /// <summary>
        ///     Sends a named event with all segments as content.
        /// </summary>
        public void SendEvent(string name, IList<ArraySegment<byte>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Send(Header(name, data.Sum(x => (long)x.Count)), data);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Opens the socket and returns without waiting.
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
                    if (_isOpening || _isClosing || IsOpened || IsServer) return;

                    _isOpening = true;
                }

                Socket socket = null;

                try
                {
                    socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    Tune(socket);

                    _onModify?.Invoke(null, socket);

                    socket.Connect(host.Address, host.Port);

                    _maxDatagram = LimitOf(socket, host.Address);

                    lock (_locker)
                    {
                        Socket = socket;
                        Host = new Host(socket.RemoteEndPoint);
                        IsOpened = true;

                        // Open can be called again from the OnOpen callback
                        _isOpening = false;
                    }

                    // allow client send data on OnOpen callback.
                    _onOpen?.Invoke(null, null);

                    ReceiveTrigger(socket, new byte[SocketLimits.Receive(socket, MaxDatagramSize)]);
                }
                catch (Exception e)
                {
                    try
                    {
                        socket?.Close();
                    }
                    catch
                    {
                        // socket already disposed
                    }

                    lock (_locker)
                    {
                        Socket = null;
                        IsOpened = false;
                    }

                    Logger.Singleton.Submit(e);
                    _onError?.Invoke(null, e);
                    _onClose?.Invoke(null, null);
                }
                finally
                {
                    _isOpening = false;
                }
            });
        }

        /// <summary>
        ///     Closes the socket and returns without waiting, <see cref="OnClose" /> is called when it is done.
        ///     For a client of a server it only forgets the peer.
        /// </summary>
        public void Close()
        {
            Socket owner;

            lock (_locker)
            {
                owner = Socket;
            }

            if (owner == null) return;

            Task.Run(() => Close(owner));
        }

        // owner avoids an old socket closing a new one.
        internal void Close(Socket owner)
        {
            lock (_locker)
            {
                if (_isClosing || IsOpened is false || ReferenceEquals(Socket, owner) is false) return;

                _isClosing = true;
            }

            try
            {
                if (IsServer is false) owner.Close();
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }
            finally
            {
                lock (_locker)
                {
                    IsOpened = false;
                    _isClosing = false;
                    if (IsServer is false) Socket = null;
                }

                _server?.Remove(this);
                _onClose?.Invoke(null, null);
            }
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

        private void Send(byte[] header, IList<ArraySegment<byte>> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var count = body.Sum(x => (long)x.Count);

            // an event can be empty, data can't
            if (count < 1 && header == null) return;

            var datagram = Datagram(header, count);
            var offset = header?.Length ?? 0;

            foreach (var segment in body)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, datagram, offset, segment.Count);
                offset += segment.Count;
            }

            Transmit(datagram);
        }

        private void Send(byte[] header, Stream body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            if (body.CanSeek is false)
                throw new ArgumentException($"{nameof(body)}: {nameof(body.CanSeek)} = {body.CanSeek}");

            var start = body.Position;
            var count = body.Length - start;

            // an event can be empty, data can't
            if (count < 1 && header == null) return;

            var datagram = Datagram(header, count);

            try
            {
                var offset = header?.Length ?? 0;

                for (var left = (int)count; left > 0;)
                {
                    var read = body.Read(datagram, offset, left);

                    if (read <= 0) throw new EndOfStreamException($"{nameof(body)}: {body.Position}");

                    offset += read;
                    left -= read;
                }
            }
            finally
            {
                // allow send the same stream again
                body.Position = start;
            }

            Transmit(datagram);
        }

        // a send buffer smaller than a datagram makes some systems (macOS) refuse it, so the buffer must be at least that big
        private static void Tune(Socket socket)
        {
            try
            {
                socket.SendBufferSize = 256 * 1024;
                socket.ReceiveBufferSize = 256 * 1024;
            }
            catch (Exception)
            {
                // the system keeps the default size
            }
        }

        // the loopback is measured (the first time takes a moment), the network is not: the system tells nothing about it
        private static int LimitOf(Socket socket, IPAddress remote)
        {
            var limit = SocketLimits.Send(socket, MaxDatagramSize);

            return remote != null && IPAddress.IsLoopback(remote) ? Math.Min(limit, Capabilities.LoopbackDatagram) : limit;
        }

        // header at the start, the caller fills the body
        private byte[] Datagram(byte[] header, long bodySize)
        {
            var size = (header?.Length ?? 0) + bodySize;

            if (size > MaxDatagramSize)
                throw new ArgumentException($"size: {size} > {nameof(MaxDatagramSize)}: {MaxDatagramSize}");

            var datagram = new byte[size];

            if (header != null) Buffer.BlockCopy(header, 0, datagram, 0, header.Length);

            return datagram;
        }

        private void Transmit(byte[] datagram)
        {
            try
            {
                Socket socket;

                lock (_locker)
                {
                    if (IsOpened is false) throw new InvalidOperationException("The connection is not open.");

                    socket = Socket;
                }

                // fits in one datagram: sent as it is
                if (datagram.Length <= _maxDatagram)
                {
                    Send(socket, datagram, datagram.Length);
                    return;
                }

                var size = _maxDatagram - UdpFragment.HeaderSize;
                var count = (datagram.Length + size - 1) / size;
                var id = unchecked((ushort)Interlocked.Increment(ref _messageId));

                for (var index = 0; index < count; index++)
                {
                    var offset = index * size;
                    var length = Math.Min(size, datagram.Length - offset);
                    var fragment = new byte[UdpFragment.HeaderSize + length];

                    UdpFragment.Write(fragment, id, index, count);
                    Buffer.BlockCopy(datagram, offset, fragment, UdpFragment.HeaderSize, length);
                    Send(socket, fragment, fragment.Length);
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset ||
                                            e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                // nobody is listening on the other side (ICMP of a previous datagram): linux, windows and macOS
                // report it on a different send, UDP has no connection to lose
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                _onError?.Invoke(null, e);
            }
        }

        private void Send(Socket socket, byte[] datagram, int length)
        {
            if (IsServer)
                socket.SendTo(datagram, 0, length, SocketFlags.None, Host.EndPoint);
            else
                socket.Send(datagram, 0, length, SocketFlags.None);
        }

        /* ---- RECEIVE --- */

        private void ReceiveTrigger(Socket owner, byte[] buffer)
        {
            for (;;)
            {
                IAsyncResult result;

                try
                {
                    result = owner.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveHandler,
                        (owner, buffer));
                }
                catch (Exception e)
                {
                    if (IsOpened) Logger.Singleton.Submit(e);
                    Close(owner);
                    return;
                }

                // a receive that completed inside BeginReceive is handled here, a recursion would overflow the stack
                if (result.CompletedSynchronously is false || ReceiveComplete(result) is false) return;
            }
        }

        private void ReceiveHandler(IAsyncResult result)
        {
            if (result.CompletedSynchronously) return;

            if (ReceiveComplete(result) is false) return;

            var (owner, buffer) = ((Socket, byte[]))result.AsyncState;
            ReceiveTrigger(owner, buffer);
        }

        // true when the next receive must be started
        private bool ReceiveComplete(IAsyncResult result)
        {
            var (owner, buffer) = ((Socket, byte[]))result.AsyncState;

            try
            {
                var size = owner.EndReceive(result);

                if (size > 0) Release(buffer, size);
            }
            catch (ObjectDisposedException)
            {
                Close(owner);
                return false;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset ||
                                            e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                // nobody is listening on the other side (ICMP), UDP has no connection to lose
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                Close(owner);
                return false;
            }

            return IsOpened && ReferenceEquals(Socket, owner);
        }

        // the server delivers the datagrams of its peers here
        internal void Release(byte[] buffer, int size)
        {
            try
            {
                // a fragment waits for the others, the message is delivered when the last one arrives
                if (UdpFragment.TryRead(buffer, size, out var id, out var index, out var count))
                {
                    buffer = Join(buffer, size, id, index, count);

                    if (buffer == null) return;

                    size = buffer.Length;
                }

                var stream = Allocate(size);

                stream.Write(buffer, 0, size);

                if (Message.TryParse(stream, out var name, out var message, Allocate))
                {
                    stream.Dispose();
                    Release(message, () => _onEvent?.Invoke(null, (name, message)));
                }
                else
                {
                    Release(stream, () => _onData?.Invoke(null, stream));
                }
            }
            catch (Exception e)
            {
                // datagram discarded, OnError then OnClose
                Logger.Singleton.Submit(e);
                _onError?.Invoke(null, e);

                Socket owner;

                lock (_locker)
                {
                    owner = Socket;
                }

                if (owner != null) Close(owner);
            }
        }

        // null until all fragments of the message arrived
        private byte[] Join(byte[] buffer, int size, ushort id, int index, int count)
        {
            lock (_assemblies)
            {
                var now = RudpWire.Now();
                List<ushort> expired = null;

                foreach (var pair in _assemblies)
                {
                    if (pair.Value.Deadline >= now) continue;

                    if (expired == null) expired = new List<ushort>();
                    expired.Add(pair.Key);
                }

                if (expired != null)
                    foreach (var key in expired)
                        _assemblies.Remove(key);

                if (_assemblies.TryGetValue(id, out var assembly) is false)
                {
                    if (_assemblies.Count >= MaxAssemblies) DropOldest();

                    assembly = new Assembly { Parts = new byte[count][], Deadline = now + FragmentLifetime };
                    _assemblies[id] = assembly;
                }

                if (assembly.Parts.Length != count || assembly.Parts[index] != null) return null;

                var part = new byte[size - UdpFragment.HeaderSize];

                Buffer.BlockCopy(buffer, UdpFragment.HeaderSize, part, 0, part.Length);
                assembly.Parts[index] = part;
                assembly.Received++;
                assembly.Length += part.Length;

                // a message is never bigger than the protocol allows
                if (assembly.Length > MaxDatagramSize)
                {
                    _assemblies.Remove(id);
                    return null;
                }

                if (assembly.Received < count) return null;

                var message = new byte[assembly.Length];
                var offset = 0;

                foreach (var item in assembly.Parts)
                {
                    Buffer.BlockCopy(item, 0, message, offset, item.Length);
                    offset += item.Length;
                }

                _assemblies.Remove(id);

                return message;
            }
        }

        private void DropOldest()
        {
            ushort oldest = 0;
            var deadline = long.MaxValue;

            foreach (var pair in _assemblies)
                if (pair.Value.Deadline < deadline)
                {
                    deadline = pair.Value.Deadline;
                    oldest = pair.Key;
                }

            _assemblies.Remove(oldest);
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
    }
}
