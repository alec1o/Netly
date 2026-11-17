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
        private Uri _openHost;
        private long _framingSize = Framing.DefaultSize;
        private int _reopenId, _activeId, _isOpening;
        private EventHandler<int> _onReopen;
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
            ReopenEnabled = false;
            ReopenAttempts = 5;
            ReopenDelay = TimeSpan.FromSeconds(1);

            // closes made by the user change _reopenId
            _onClose += (sender, status) =>
            {
                if (ReopenEnabled && Volatile.Read(ref _reopenId) == _activeId) Reopen(_openHost, _activeId);
            };
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
        ///     Called when the connection fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
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
        ///     It does not happen after <see cref="Close()" /> and it must be called before <see cref="Open" />.
        /// </summary>
        /// <param name="enabled">True to open the connection again.</param>
        /// <param name="attempts">Maximum number of attempts for each failure. -1 means no limit.</param>
        /// <param name="delay">Time to wait before each attempt. Null means one second.</param>
        /// <exception cref="InvalidOperationException">While the connection is opened or for a client of a server.</exception>
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
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <param name="host">Server address, e.g. ws://127.0.0.1:8080/chat</param>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Uri host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            // a new Open replaces any pending reopen
            var id = Interlocked.Increment(ref _reopenId);

            _ = OpenAsync(host, id, false);
        }

        // true when the connection is open, every failure goes to OnError
        private async Task<bool> OpenAsync(Uri host, int id, bool isAttempt)
        {
            if (IsServer || IsOpened || Interlocked.Exchange(ref _isOpening, 1) == 1) return IsOpened;

            try
            {
                _openHost = host;
                _activeId = id;

                await Connect(host);
            }
            finally
            {
                Interlocked.Exchange(ref _isOpening, 0);
            }

            if (IsOpened) return true;

            // an attempt that fails is handled by the loop of the reopen
            if (isAttempt is false && ReopenEnabled && Volatile.Read(ref _reopenId) == id) Reopen(host, id);

            return false;
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

        private void Reopen(Uri host, int id)
        {
            if (host == null) return;

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
            // user close never opens again
            Interlocked.Increment(ref _reopenId);

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
            if (IsOpened is false || buffers == null || buffers.Length <= 0) return;

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

            try
            {
                const int size = 1024 * 8;
                var buffer = new ArraySegment<byte>(new byte[size], 0, size);
                var fragments = new MemoryStream();

                while (IsOpened)
                {
                    var result = await (IsServer ? _serverSocket : _clientSocket).ReceiveAsync(buffer,
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close || buffer.Array == null)
                    {
                        closeStatus = result.CloseStatus ?? closeStatus;
                        break;
                    }

                    fragments.Write(buffer.Array, 0, result.Count);

                    if (fragments.Length > _framingSize)
                        throw new InvalidOperationException($"{nameof(fragments)}: {fragments.Length}");

                    // wait the end of message
                    if (!result.EndOfMessage) continue;

                    var data = fragments.ToArray();

                    fragments.SetLength(0);

                    var delivery = result.MessageType == WebSocketMessageType.Text
                        ? WebSocketDelivery.Text
                        : WebSocketDelivery.Binary;

                    Release(data, delivery);
                }
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                closeStatus = WebSocketCloseStatus.EndpointUnavailable;
            }
            finally
            {
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

        private void Release(byte[] data, WebSocketDelivery delivery)
        {
            try
            {
                var stream = Helper.NewStream(data.LongLength);
                stream.Write(data, 0, data.Length);

                if (Message.TryParse(stream, out var name, out var message, Helper.NewStream))
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
