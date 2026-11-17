using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Netly
{
    /// <summary>
    ///     WebSocket server. The path of the address given to <see cref="Open" /> is the route of the connections.
    /// </summary>
    public class WebSocketServer
    {
        private readonly HttpServer _inner = new HttpServer();
        private readonly object _locker = new object();
        private readonly List<WebSocketClient> _clients = new List<WebSocketClient>();
        private readonly HashSet<string> _paths = new HashSet<string>();
        private EventHandler<WebSocketClient> _onAccept;
        private long _framingSize = Framing.DefaultSize;

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this server.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the server is listening.
        /// </summary>
        public bool IsOpened => _inner.IsOpened;

        /// <summary>
        ///     Address where the server is listening.
        /// </summary>
        public Uri Host => _inner.Host;

        /// <summary>
        ///     Biggest message accepted from the clients, a bigger one closes the connection of that client.
        ///     Default is 20 MB, minimum 1 KB.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the server is opened.</exception>
        public long FramingSize
        {
            get => _framingSize;
            set
            {
                if (IsOpened)
                    throw new InvalidOperationException($"Must not update ({nameof(FramingSize)}) while socket is Opened.");

                _framingSize = Math.Max(1024, value);
            }
        }

        /// <summary>
        ///     Snapshot of the connected clients.
        /// </summary>
        public IReadOnlyList<WebSocketClient> Clients
        {
            get
            {
                lock (_locker)
                {
                    return _clients.ToArray();
                }
            }
        }

        /// <summary>
        ///     Called with the new listener before it starts, change the listener options here.
        /// </summary>
        public void OnModify(Action<HttpListener> callback)
        {
            _inner.OnModify(callback);
        }

        /// <summary>
        ///     Called when the server starts listening.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _inner.OnOpen(callback);
        }

        /// <summary>
        ///     Called when the server is closed.
        /// </summary>
        public void OnClose(Action callback)
        {
            _inner.OnClose(callback);
        }

        /// <summary>
        ///     Called when the server fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _inner.OnError(callback);
        }

        /// <summary>
        ///     Called for each client that connects, the client is already open.
        /// </summary>
        public void OnAccept(Action<WebSocketClient> callback)
        {
            _onAccept += (sender, client) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(client));
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastData(byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary,
            Func<WebSocketClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendData(data, delivery);
        }

        /// <summary>
        ///     Sends a named event to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastEvent(string name, byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary,
            Func<WebSocketClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendEvent(name, data, delivery);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts listening and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <param name="host">Address, e.g. ws://0.0.0.0:8080/chat or http://127.0.0.1:8080/chat</param>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Uri host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            var path = host.AbsolutePath.Length > 1 ? host.AbsolutePath.TrimEnd('/') : "/";

            lock (_locker)
            {
                if (_paths.Add(path)) _inner.Map.WebSocket(path, (request, client) => Register(client));
            }

            _inner.Open(host);
        }

        /// <summary>
        ///     Stops listening, closes all clients and returns without waiting.
        ///     <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            _inner.Close();
        }

        private void Register(WebSocketClient client)
        {
            client.SetFramingSize(_framingSize);

            lock (_locker)
            {
                _clients.Add(client);
            }

            client.OnClose(() =>
            {
                lock (_locker)
                {
                    _clients.Remove(client);
                }
            });

            _onAccept?.Invoke(null, client);
        }
    }
}
