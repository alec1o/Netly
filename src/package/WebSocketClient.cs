using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;

namespace Netly
{
    /// <summary>
    ///     WebSocket client, also the type of each client accepted by <see cref="WebSocketServer" />
    ///     and by <see cref="HttpServer" />.
    /// </summary>
    public class WebSocketClient
    {
        private readonly HTTP.WebSocket _inner;

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a client.
        /// </summary>
        public WebSocketClient()
        {
            _inner = new HTTP.WebSocket();
            IsServer = false;
        }

        internal WebSocketClient(HTTP.WebSocket inner)
        {
            _inner = inner;
            IsServer = true;
        }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the connection is open.
        /// </summary>
        public bool IsOpened => _inner.IsOpened;

        /// <summary>
        ///     True when this client was accepted by a server.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        ///     Address of the other side.
        /// </summary>
        public Uri Host => _inner.Host;

        /// <summary>
        ///     Headers sent in the handshake, change them before <see cref="Open" />.
        /// </summary>
        public Dictionary<string, string> Headers => _inner.Headers;

        /// <summary>
        ///     The handshake request, as the server received it.
        /// </summary>
        public Interfaces.IHTTP.ServerRequest Request => _inner.ServerRequest;

        /// <summary>
        ///     Called with the new socket before it connects, change the socket options here.
        /// </summary>
        public void OnModify(Action<ClientWebSocket> callback)
        {
            _inner.On.Modify(callback);
        }

        /// <summary>
        ///     Called when the connection is established.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _inner.On.Open(callback);
        }

        /// <summary>
        ///     Called when the connection is closed, by this side or by the peer.
        /// </summary>
        public void OnClose(Action callback)
        {
            _inner.On.Close(callback);
        }

        /// <summary>
        ///     Called when the connection is closed, with the close status.
        /// </summary>
        public void OnClose(Action<WebSocketCloseStatus> callback)
        {
            _inner.On.Close(callback);
        }

        /// <summary>
        ///     Called when the connection fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _inner.On.Error(callback);
        }

        /// <summary>
        ///     Called for each received message.
        ///     The stream starts at position 0 and is disposed when the callback returns.
        /// </summary>
        public void OnData(Action<Stream, WebSocketDelivery> callback)
        {
            _inner.On.Data((data, type) => Release(data, stream => callback?.Invoke(stream, (WebSocketDelivery)(int)type)));
        }

        /// <summary>
        ///     Called for each received named event, same stream rules of <see cref="OnData" />.
        /// </summary>
        public void OnEvent(Action<string, Stream, WebSocketDelivery> callback)
        {
            _inner.On.Event((name, data, type) =>
                Release(data, stream => callback?.Invoke(name, stream, (WebSocketDelivery)(int)type)));
        }

        private static void Release(byte[] data, Action<Stream> callback)
        {
            using (var stream = new MemoryStream(data, false))
            {
                callback(stream);
            }
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

            _inner.To.Data(data, (HTTP.MessageType)(int)delivery);
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

            _inner.To.Event(name, data, (HTTP.MessageType)(int)delivery);
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

            _ = _inner.To.Open(host);
        }

        /// <summary>
        ///     Closes the connection and returns without waiting, <see cref="OnClose(Action)" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            _ = _inner.To.Close();
        }

        /// <summary>
        ///     Closes the connection with a status and returns without waiting.
        /// </summary>
        public void Close(WebSocketCloseStatus status)
        {
            _ = _inner.To.Close(status);
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
