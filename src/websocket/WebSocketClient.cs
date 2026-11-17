using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     WebSocket client, also the type of each client accepted by <see cref="WebSocketServer" />
    ///     and by <see cref="HttpServer" />.
    /// </summary>
    public class WebSocketClient
    {
        private const string EncryptedScheme = "wss", UnencryptedScheme = "ws";

        private readonly SemaphoreSlim _sendLocker = new SemaphoreSlim(1, 1);
        private readonly object _closeLocker = new object();
        private ClientWebSocket _clientSocket;
        private System.Net.WebSockets.WebSocket _serverSocket;
        private bool _tryConnecting, _tryClosing, _serverInitialized;
        private long _framingSize = Framing.DefaultSize;
        private int _isOpening;
        private Func<long, Stream> _onDynamicAllocation;
        private EventHandler _onOpen;
        private EventHandler<WebSocketCloseStatus> _onClose;
        private EventHandler<Exception> _onError;
        private EventHandler<ClientWebSocket> _onModify;
        private EventHandler<(Stream Stream, WebSocketDelivery Delivery)> _onData;
        private EventHandler<(string Name, Stream Stream, WebSocketDelivery Delivery)> _onEvent;

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a client.
        /// </summary>
        public WebSocketClient()
        {
            IsServer = false;
            Host = new Uri($"{EncryptedScheme}://{IPAddress.Any}");
        }

        // client accepted by a server
        internal WebSocketClient(System.Net.WebSockets.WebSocket socket, HttpServerRequest request)
        {
            _serverSocket = socket;
            IsServer = true;
            Request = request;
            Host = new Uri
            (
                $"{(request.IsEncrypted ? EncryptedScheme : UnencryptedScheme)}://{request.RemoteEndPoint.Address}:{request.RemoteEndPoint.Port}"
            );
        }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the connection is open.
        /// </summary>
        public bool IsOpened => IsServer
            ? _serverSocket != null && _serverSocket.State == WebSocketState.Open
            : _clientSocket != null && _clientSocket.State == WebSocketState.Open;

        /// <summary>
        ///     True when this client was accepted by a server.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        ///     Address of the other side.
        /// </summary>
        public Uri Host { get; private set; }

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

        // the server gives its limit to each client it accepts
        internal void SetFramingSize(long value)
        {
            _framingSize = Math.Max(1024, value);
        }

        /// <summary>
        ///     Headers sent in the handshake, change them before <see cref="Open" />.
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        ///     The handshake request, as the server received it.
        /// </summary>
        public HttpServerRequest Request { get; private set; }

        /// <summary>
        ///     Called with the new socket before it connects, change the socket options here.
        /// </summary>
        public void OnModify(Action<ClientWebSocket> callback)
        {
            _onModify += (sender, socket) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(socket));
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
        ///     Called when the connection is closed, with the close status.
        /// </summary>
        public void OnClose(Action<WebSocketCloseStatus> callback)
        {
            _onClose += (sender, status) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(status));
        }

        /// <summary>
        ///     Called when the connection fails to open (then <see cref="OnClose(Action)" /> is called), or when the
        ///     data received is above the limit or makes an allocation or a callback of the app throw (the message is
        ///     discarded and the connection closes: <see cref="OnClose(Action)" /> is called after).
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /// <summary>
        ///     Chooses where each received message is written. A WebSocket does not tell the size of a message before
        ///     its end, so this is the only allocation callback of the WebSocket (there is no OnAllocate):
        ///     <br />
        ///     - the first call receives <c>0</c> and returns the stream that starts the message (for example one of 8 KB);
        ///     <br />
        ///     - when the message grows over the size of the stream (at 8 KB, 16 KB, 32 KB...) it is called again with the
        ///     current size: return a bigger stream, the content is copied to it and the old one is disposed, or
        ///     <c>null</c> to keep the current one (a stream that grows by itself, like a <see cref="FileStream" />);
        ///     <br />
        ///     - for the body of an event, which is allocated after the message ended, it is called once with the
        ///     exact size.
        ///     <br />
        ///     An exception, a <c>null</c> on the first call or a failed copy discards the message and closes the
        ///     connection: <see cref="OnError" />, then <see cref="OnClose(Action)" />. Without it a
        ///     <see cref="MemoryStream" /> is used. The limit is always <see cref="FramingSize" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the connection is opened.</exception>
        public void OnDynamicAllocation(Func<long, Stream> callback)
        {
            if (IsOpened && !IsServer)
                throw new InvalidOperationException(
                    $"Must not update ({nameof(OnDynamicAllocation)}) while socket is Opened.");

            _onDynamicAllocation = callback;
        }

        // the server gives it to each client it accepts
        internal void SetDynamicAllocation(Func<long, Stream> callback)
        {
            _onDynamicAllocation = callback;
        }

        /// <summary>
        ///     Called for each received message.
        ///     The stream starts at position 0 and is disposed when the callback returns.
        /// </summary>
        public void OnData(Action<Stream, WebSocketDelivery> callback)
        {
            _onData += (sender, data) => callback?.Invoke(data.Stream, data.Delivery);
        }

        /// <summary>
        ///     Called for each received named event, same stream rules of <see cref="OnData" />.
        /// </summary>
        public void OnEvent(Action<string, Stream, WebSocketDelivery> callback)
        {
            _onEvent += (sender, data) => callback?.Invoke(data.Name, data.Stream, data.Delivery);
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data from <see cref="System.IO.Stream.Position" /> to the end, the stream is not closed
        ///     and its position is restored.
        /// </summary>
        /// <exception cref="ArgumentException">When the stream can't seek.</exception>
        public void SendData(Stream data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendData(Join(data), delivery);
        }

        /// <summary>
        ///     Sends the whole array.
        /// </summary>
        public void SendData(byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // empty data is ignored
            if (data.Length < 1) return;

            Send(delivery, data);
        }

        /// <summary>
        ///     Sends <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendData(byte[] data, int offset, int count, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendData(Segment(data, offset, count), delivery);
        }

        /// <summary>
        ///     Sends the segment.
        /// </summary>
        public void SendData(ArraySegment<byte> data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendData(new[] { data }, delivery);
        }

        /// <summary>
        ///     Sends all segments as one message.
        /// </summary>
        public void SendData(IList<ArraySegment<byte>> data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendData(Join(data), delivery);
        }

        /// <summary>
        ///     Sends an event without content.
        /// </summary>
        public void SendEvent(string name, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendEvent(name, Array.Empty<byte>(), delivery);
        }

        /// <summary>
        ///     Sends a named event with the content from the current position to the end of the stream.
        ///     <br />
        ///     The stream is not closed and its position is restored after sending.
        /// </summary>
        public void SendEvent(string name, Stream data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendEvent(name, Join(data), delivery);
        }

        /// <summary>
        ///     Sends a named event with the whole array as content.
        /// </summary>
        public void SendEvent(string name, byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException($"{nameof(name)}: {name}");

            if (data == null) throw new ArgumentNullException(nameof(data));

            Send(delivery, Message.Create(name, data.LongLength), data);
        }

        /// <summary>
        ///     Sends a named event with <paramref name="count" /> bytes of the array, starting at <paramref name="offset" />.
        /// </summary>
        public void SendEvent(string name, byte[] data, int offset, int count,
            WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendEvent(name, Segment(data, offset, count), delivery);
        }

        /// <summary>
        ///     Sends a named event with the segment as content.
        /// </summary>
        public void SendEvent(string name, ArraySegment<byte> data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendEvent(name, new[] { data }, delivery);
        }

        /// <summary>
        ///     Sends a named event with all segments as content.
        /// </summary>
        public void SendEvent(string name, IList<ArraySegment<byte>> data,
            WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            SendEvent(name, Join(data), delivery);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts the connection and returns without waiting.
        ///     The result comes by <see cref="OnOpen" />, or by <see cref="OnError" /> followed by
        ///     <see cref="OnClose(Action)" />.
        /// </summary>
        /// <param name="host">Server address, e.g. ws://127.0.0.1:8080/chat</param>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Uri host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            _ = OpenAsync(host);
        }

        // true when the connection is open, every failure goes to OnError then OnClose
        private async Task<bool> OpenAsync(Uri host)
        {
            if (IsServer || IsOpened || Interlocked.Exchange(ref _isOpening, 1) == 1) return IsOpened;

            try
            {
                await Connect(host);
            }
            finally
            {
                Interlocked.Exchange(ref _isOpening, 0);
            }

            return IsOpened;
        }

        // every failure goes to OnError, it never throws
        private Task Connect(Uri host)
        {
            lock (_closeLocker)
            {
                if (_tryConnecting || _tryClosing) return Task.CompletedTask;

                _tryConnecting = true;
            }

            return Task.Run(async () =>
            {
                ClientWebSocket ws = null;

                try
                {
                    ws = new ClientWebSocket();

                    foreach (var header in Headers) ws.Options.SetRequestHeader(header.Key, header.Value);

                    _onModify?.Invoke(null, ws);

                    await ws.ConnectAsync(host, CancellationToken.None);

                    Request = new HttpServerRequest(ws, host, Headers);

                    Host = host;

                    _clientSocket = ws;

                    // connecting is done, the user can close (or open again) inside OnOpen
                    lock (_closeLocker)
                    {
                        _tryConnecting = false;
                    }

                    _onOpen?.Invoke(null, null);

                    ReceiveTrigger();
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);

                    // the socket never opened, there is no close handshake
                    try
                    {
                        ws?.Dispose();
                    }
                    catch (Exception e2)
                    {
                        Logger.Singleton.Submit(e2);
                    }

                    _clientSocket = null;

                    _onError?.Invoke(null, e);
                    _onClose?.Invoke(null, WebSocketCloseStatus.Empty);
                }
                finally
                {
                    // REQUIRED: do not touch the closing flag, a close can already be running
                    lock (_closeLocker)
                    {
                        _tryConnecting = false;
                    }
                }
            });
        }

        /// <summary>
        ///     Closes the connection and returns without waiting, <see cref="OnClose(Action)" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            Close(WebSocketCloseStatus.NormalClosure);
        }

        /// <summary>
        ///     Closes the connection with a status and returns without waiting.
        /// </summary>
        public void Close(WebSocketCloseStatus status)
        {
            _ = CloseAsync(status);
        }

        private Task CloseAsync(WebSocketCloseStatus status)
        {
            System.Net.WebSockets.WebSocket socket;

            // the user and the receiver can close at the same time, only one of them goes on
            lock (_closeLocker)
            {
                if (_tryClosing || _tryConnecting) return Task.CompletedTask;

                socket = IsServer ? _serverSocket : _clientSocket;

                if (socket == null) return Task.CompletedTask;

                _tryClosing = true;
            }

            return Task.Run(async () =>
            {
                try
                {
                    var state = socket.State;

                    // close handshake only works in these states, otherwise it throws (WebSocketException 997)
                    if
                    (
                        state == WebSocketState.Open ||
                        state == WebSocketState.CloseReceived ||
                        state == WebSocketState.CloseSent
                    )
                    {
                        // only sends the close frame, waiting for the answer of the peer can hang when both sides close
                        // at the same time (the receiver of this socket already reads that answer)
                        using (var timeout = new CancellationTokenSource(5000))
                        {
                            await socket.CloseOutputAsync(status, string.Empty, timeout.Token);
                        }
                    }
                }
                catch (Exception e) when
                (
                    e is WebSocketException ||
                    e is ObjectDisposedException ||
                    e is OperationCanceledException
                )
                {
                    // peer aborted or disposed the socket between the state check and the close
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                }
                finally
                {
                    socket.Dispose();

                    if (IsServer)
                        _serverSocket = null;
                    else
                        _clientSocket = null;

                    lock (_closeLocker)
                    {
                        _tryClosing = false;
                    }

                    _onClose?.Invoke(null, status);
                }
            });
        }

        // the server calls it when the client was accepted
        internal void ServerInitializer()
        {
            if (_serverInitialized) return;

            _serverInitialized = true;

            ReceiveTrigger();

            _onOpen?.Invoke(null, null);
        }

        /* ---- SEND AND RECEIVE --- */

        private async void Send(WebSocketDelivery delivery, params byte[][] buffers)
        {
            if (buffers == null || buffers.Length <= 0) return;

            if (IsOpened is false)
            {
                _onError?.Invoke(null, new InvalidOperationException("The connection is not open."));
                return;
            }

            var type = delivery == WebSocketDelivery.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary;

            // event header and body must be the same websocket message
            var content = new ArraySegment<byte>(buffers.Length == 1 ? buffers[0] : Helper.ArrayConcat(buffers));

            try
            {
                // websocket allows only one outstanding send
                await _sendLocker.WaitAsync();

                try
                {
                    await (IsServer ? _serverSocket : _clientSocket).SendAsync(content, type, true,
                        CancellationToken.None);
                }
                finally
                {
                    _sendLocker.Release();
                }
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }
        }

        private void ReceiveTrigger()
        {
            // the loop awaits the socket, it does not hold a thread while the connection is idle
            ThreadPool.QueueUserWorkItem(ReceiveJob);
        }

        private async void ReceiveJob(object _)
        {
            var closeStatus = WebSocketCloseStatus.Empty;
            Stream message = null;

            try
            {
                const int size = 1024 * 8;
                var buffer = new ArraySegment<byte>(new byte[size], 0, size);
                long written = 0, limit = 0;

                while (IsOpened)
                {
                    var result = await (IsServer ? _serverSocket : _clientSocket).ReceiveAsync(buffer,
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close || buffer.Array == null)
                    {
                        closeStatus = result.CloseStatus ?? closeStatus;
                        break;
                    }

                    try
                    {
                        // write in the stream of the app while it arrives
                        if (message == null)
                        {
                            message = Allocate(0, null);
                            written = 0;
                            limit = _onDynamicAllocation == null ? long.MaxValue : size;
                        }

                        if (written + result.Count > _framingSize)
                            throw new InvalidDataException(
                                $"message: {written + result.Count} > {nameof(FramingSize)}: {_framingSize}");

                        while (written + result.Count > limit)
                        {
                            message = Allocate(written, message);
                            limit = Math.Max(limit * 2, written + result.Count);
                        }

                        message.Write(buffer.Array, 0, result.Count);
                        written += result.Count;

                        // wait the end of message
                        if (!result.EndOfMessage) continue;

                        var delivery = result.MessageType == WebSocketMessageType.Text
                            ? WebSocketDelivery.Text
                            : WebSocketDelivery.Binary;

                        var done = message;

                        message = null;

                        Release(done, delivery);
                    }
                    catch (Exception e)
                    {
                        // data or app failed: OnError then OnClose
                        Logger.Singleton.Submit(e);
                        _onError?.Invoke(null, e);
                        closeStatus = WebSocketCloseStatus.EndpointUnavailable;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                closeStatus = WebSocketCloseStatus.EndpointUnavailable;
            }
            finally
            {
                // message that did not end
                message?.Dispose();

                try
                {
                    await CloseAsync(closeStatus);
                }
                catch (Exception e)
                {
                    // async void: nothing can be thrown from here
                    Logger.Singleton.Submit(e);
                }
            }
        }

        // first call starts the stream, the next ones copy it to a bigger one
        private Stream Allocate(long size, Stream current)
        {
            var callback = _onDynamicAllocation;

            if (callback == null) return current ?? new MemoryStream();

            var next = callback(size);

            if (current == null)
                return next ?? throw new InvalidOperationException("OnDynamicAllocation returned null on the first call");

            // keep the current stream
            if (next == null || ReferenceEquals(next, current)) return current;

            try
            {
                current.Position = 0;
                current.CopyTo(next);

                if (next.Position != size) throw new InvalidDataException($"{nameof(next.Position)}: {next.Position}");
            }
            catch (Exception)
            {
                next.Dispose();
                throw;
            }

            current.Dispose();

            return next;
        }

        // message complete, an event or data
        private void Release(Stream stream, WebSocketDelivery delivery)
        {
            try
            {
                stream.Position = 0;

                if (Message.TryParse(stream, out var name, out var message, length => Allocate(length, null)))
                {
                    stream.Dispose();
                    Release(message, () => _onEvent?.Invoke(null, (name, message, delivery)));
                }
                else
                {
                    Release(stream, () => _onData?.Invoke(null, (stream, delivery)));
                }
            }
            catch (Exception)
            {
                stream.Dispose();
                throw;
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

        /* ---- HELPERS --- */

        private static ArraySegment<byte> Segment(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (offset < 0 || count < 0 || offset > data.Length - count)
                throw new ArgumentOutOfRangeException($"{nameof(offset)}: {offset}, {nameof(count)}: {count}");

            return new ArraySegment<byte>(data, offset, count);
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
    }
}
