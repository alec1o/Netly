using System;
using System.Net;
using Netly.Interfaces;

namespace Netly
{
    /// <summary>
    ///     HTTP server with routes, middleware and WebSocket routes.
    /// </summary>
    public class HttpServer
    {
        private readonly HTTP.Server _inner = new HTTP.Server();

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a server.
        /// </summary>
        public HttpServer()
        {
            Map = new HttpMap(_inner);
        }

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
        ///     Routes of the server.
        /// </summary>
        public HttpMap Map { get; }

        /// <summary>
        ///     Middleware that runs before the routes.
        /// </summary>
        public IHTTP.Middleware Middleware => _inner.Middleware;

        /// <summary>
        ///     Called with the new listener before it starts, change the listener options here.
        /// </summary>
        public void OnModify(Action<HttpListener> callback)
        {
            _inner.On.Modify(callback);
        }

        /// <summary>
        ///     Called when the server starts listening.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _inner.On.Open(callback);
        }

        /// <summary>
        ///     Called when the server is closed.
        /// </summary>
        public void OnClose(Action callback)
        {
            _inner.On.Close(callback);
        }

        /// <summary>
        ///     Called when the server fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _inner.On.Error(callback);
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data to all WebSocket clients connected to the routes of this server.
        /// </summary>
        public void BroadcastWebSocketData(byte[] data, WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _inner.To.WebsocketDataBroadcast(data, (HTTP.MessageType)(int)delivery);
        }

        /// <summary>
        ///     Sends a named event to all WebSocket clients connected to the routes of this server.
        /// </summary>
        public void BroadcastWebSocketEvent(string name, byte[] data,
            WebSocketDelivery delivery = WebSocketDelivery.Binary)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _inner.To.WebsocketEventBroadcast(name, data, (HTTP.MessageType)(int)delivery);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts listening and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Uri host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            _ = _inner.To.Open(host);
        }

        /// <summary>
        ///     Stops listening and returns without waiting, <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            _ = _inner.To.Close();
        }
    }

    /// <summary>
    ///     Routes of a <see cref="HttpServer" />.
    /// </summary>
    public class HttpMap
    {
        private readonly HTTP.Server _server;

        internal HttpMap(HTTP.Server server)
        {
            _server = server;
        }

        /// <summary>
        ///     Route for WebSocket connections. The client is already open when the callback runs.
        /// </summary>
        public void WebSocket(string path, Action<IHTTP.ServerRequest, WebSocketClient> callback)
        {
            _server.Map.WebSocket(path, (request, socket) =>
                callback?.Invoke(request, new WebSocketClient((HTTP.WebSocket)socket)));
        }

        /// <summary>
        ///     Route for any method.
        /// </summary>
        public void All(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.All(path, callback);
        }

        /// <summary>
        ///     Route for GET.
        /// </summary>
        public void Get(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Get(path, callback);
        }

        /// <summary>
        ///     Route for PUT.
        /// </summary>
        public void Put(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Put(path, callback);
        }

        /// <summary>
        ///     Route for HEAD.
        /// </summary>
        public void Head(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Head(path, callback);
        }

        /// <summary>
        ///     Route for POST.
        /// </summary>
        public void Post(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Post(path, callback);
        }

        /// <summary>
        ///     Route for PATCH.
        /// </summary>
        public void Patch(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Patch(path, callback);
        }

        /// <summary>
        ///     Route for DELETE.
        /// </summary>
        public void Delete(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Delete(path, callback);
        }

        /// <summary>
        ///     Route for TRACE.
        /// </summary>
        public void Trace(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Trace(path, callback);
        }

        /// <summary>
        ///     Route for OPTIONS.
        /// </summary>
        public void Options(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
        {
            _server.Map.Options(path, callback);
        }
    }
}
