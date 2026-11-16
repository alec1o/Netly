using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     UDP client, also the type of each peer known by <see cref="UdpServer" />.
    /// </summary>
    public class UdpClient
    {
        /// <summary>
        ///     Biggest datagram that can be sent (65535 - 8 bytes UDP header - 20 bytes IP header).
        /// </summary>
        public const int MaxDatagramSize = 65507;

        private readonly object _locker = new object();
        private readonly UdpServer _server;
        private bool _isOpening, _isClosing;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<(string Name, Stream Stream)> _onEvent;
        private EventHandler<Exception> _onError;
        private EventHandler<Stream> _onData;
        private Func<long, Stream> _onAllocate;
        private Func<long, Stream> Allocate => IsServer ? _server.OnAllocateObject : _onAllocate;

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
        ///     Called when the socket fails to open, when a send fails or when a callback throws while receiving.
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
        /// <exception cref="ArgumentException">When the stream can't seek or the datagram is too big.</exception>
        public void SendData(Stream data)
        {
            Send(null, data);
        }

        /// <summary>
        ///     Sends the whole array.
        /// </summary>
        /// <exception cref="ArgumentException">When the datagram is bigger than <see cref="MaxDatagramSize" />.</exception>
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

                    _onModify?.Invoke(null, socket);

                    socket.Connect(host.Address, host.Port);

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

                    ReceiveTrigger(socket, new byte[MaxDatagramSize]);
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

        // header at the start, the caller fills the body
        private static byte[] Datagram(byte[] header, long bodySize)
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
                    if (IsOpened is false) return;

                    socket = Socket;
                }

                if (IsServer)
                    socket.SendTo(datagram, 0, datagram.Length, SocketFlags.None, Host.EndPoint);
                else
                    socket.Send(datagram, 0, datagram.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                _onError?.Invoke(null, e);
            }
        }

        /* ---- RECEIVE --- */

        private void ReceiveTrigger(Socket owner, byte[] buffer)
        {
            try
            {
                owner.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveHandler, (owner, buffer));
            }
            catch (Exception e)
            {
                if (IsOpened) Logger.Singleton.Submit(e);
                Close(owner);
            }
        }

        private void ReceiveHandler(IAsyncResult result)
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
                return;
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
                return;
            }

            if (IsOpened && ReferenceEquals(Socket, owner))
                ReceiveTrigger(owner, buffer);
        }

        // the server delivers the datagrams of its peers here
        internal void Release(byte[] buffer, int size)
        {
            try
            {
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
    }
}
