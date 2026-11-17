using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     RUDP server (reliable UDP). A connection starts with a handshake sent by <see cref="RudpClient" />.
    /// </summary>
    public class RudpServer
    {
        private readonly object _locker = new object();
        private Dictionary<IPEndPoint, Session> _sessions = new Dictionary<IPEndPoint, Session>();
        private bool _isOpening, _isClosing;
        private int _handshakeTimeout = 5000, _noResponseTimeout = 5000;
        private EventHandler<RudpClient> _onAccept;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<Exception> _onError;

        private class Session
        {
            public RudpConnection Connection;
            public RudpClient Client;
        }

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a server.
        /// </summary>
        public RudpServer()
        {
            Host = Host.Default;
            IsOpened = false;
            OnAllocateObject = Helper.NewStream;
            Socket = null;
        }

        /* ---- INTERFACE --- */

        internal Func<long, Stream> OnAllocateObject { get; private set; }

        /// <summary>
        ///     Unique id of this server.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while the socket is open.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        ///     Snapshot of the connected clients.
        /// </summary>
        public IReadOnlyList<RudpClient> Clients
        {
            get
            {
                lock (_locker)
                {
                    return _sessions.Values.Where(x => x.Connection.IsOpened).Select(x => x.Client).ToArray();
                }
            }
        }

        /// <summary>
        ///     The socket. It is null while the server is closed.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        ///     Address where the server is bound.
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        ///     Milliseconds each client has to finish the handshake, minimum 1000. Default is 5000.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the value is less than 1000.</exception>
        public int HandshakeTimeout
        {
            get => _handshakeTimeout;
            set => _handshakeTimeout = Timeout(value, nameof(HandshakeTimeout));
        }

        /// <summary>
        ///     Milliseconds without any packet from a client before it is closed, minimum 1000. Default is 5000.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the value is less than 1000.</exception>
        public int NoResponseTimeout
        {
            get => _noResponseTimeout;
            set => _noResponseTimeout = Timeout(value, nameof(NoResponseTimeout));
        }

        private int Timeout(int value, string name)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not modify ({name}) while socket is Opened.");

            if (value < 1000)
                throw new ArgumentOutOfRangeException($"{name}: {value}");

            return value;
        }

        /// <summary>
        ///     Called with the new socket before it binds, change the socket options here.
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
        ///     Called when the server fails to open.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /// <summary>
        ///     Called for each client that finishes the handshake.
        /// </summary>
        public void OnAccept(Action<RudpClient> callback)
        {
            _onAccept += (sender, client) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(client));
        }

        /// <summary>
        ///     Chooses where each message received from the clients is written, receives the size of the message.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public void OnAllocate(Func<long, Stream> callback)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not update ({nameof(OnAllocate)}) while socket is Opened.");

            OnAllocateObject = callback ?? Helper.NewStream;
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastData(byte[] data, RudpDelivery delivery = RudpDelivery.Reliable,
            Func<RudpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendData(data, delivery);
        }

        /// <summary>
        ///     Sends a named event to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastEvent(string name, byte[] data, RudpDelivery delivery = RudpDelivery.Reliable,
            Func<RudpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendEvent(name, data, delivery);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Binds the socket and returns without waiting.
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
                    if (_isOpening || _isClosing || IsOpened) return;

                    _isOpening = true;
                }

                Socket socket = null;

                try
                {
                    socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _onModify?.Invoke(null, socket);

                    Tune(socket);

                    socket.Bind(host.EndPoint);

                    lock (_locker)
                    {
                        Host = new Host(socket.LocalEndPoint);
                        Socket = socket;
                        _sessions = new Dictionary<IPEndPoint, Session>();
                        IsOpened = true;

                        // Open can be called again from the OnOpen callback
                        _isOpening = false;
                    }

                    _onOpen?.Invoke(null, null);

                    ReceiveTrigger(socket, new byte[ushort.MaxValue]);
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
        ///     Closes all clients and the socket and returns without waiting.
        ///     <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            if (!IsOpened) return;

            Task.Run(() =>
            {
                Session[] sessions;
                Socket socket;

                lock (_locker)
                {
                    if (_isClosing || IsOpened is false) return;

                    _isClosing = true;
                    IsOpened = false;
                    socket = Socket;
                    sessions = _sessions.Values.ToArray();
                    _sessions.Clear();
                }

                try
                {
                    // clients receive the close notice before the socket is closed
                    foreach (var session in sessions) session.Connection.Close();

                    socket?.Close();
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                }
                finally
                {
                    lock (_locker)
                    {
                        Socket = null;
                        _isClosing = false;
                    }

                    _onClose?.Invoke(null, null);
                }
            });
        }

        /* ---- RECEIVE --- */

        private static void Tune(Socket socket)
        {
            try
            {
                socket.ReceiveBufferSize = 4 * 1024 * 1024;
                socket.SendBufferSize = 4 * 1024 * 1024;
            }
            catch (Exception)
            {
                // the system keeps the default size
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    socket.IOControl(-1744830452, new byte[] { 0 }, null);
            }
            catch (Exception)
            {
                // windows: don't fail the receive when a client is gone (icmp port unreachable)
            }
        }

        private static EndPoint AnyEndPoint(Socket socket)
        {
            return new IPEndPoint(socket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
        }

        private void ReceiveTrigger(Socket owner, byte[] buffer)
        {
            for (;;)
            {
                IAsyncResult result;

                try
                {
                    var remote = AnyEndPoint(owner);

                    result = owner.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote,
                        ReceiveHandler, (owner, buffer));
                }
                catch (Exception e)
                {
                    if (IsOpened) Logger.Singleton.Submit(e);
                    Close();
                    return;
                }

                // a receive that completed inside BeginReceiveFrom is handled here, a recursion would overflow the stack
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
                var remote = AnyEndPoint(owner);

                var size = owner.EndReceiveFrom(result, ref remote);

                if (size > 0) Dispatch((IPEndPoint)remote, owner, buffer, size);
            }
            catch (ObjectDisposedException)
            {
                // socket closed
                return false;
            }
            catch (SocketException)
            {
                // client gone (connection reset), the socket still works
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            return IsOpened && ReferenceEquals(Socket, owner);
        }

        private void Dispatch(IPEndPoint endpoint, Socket owner, byte[] data, int size)
        {
            Session session;

            lock (_locker)
            {
                _sessions.TryGetValue(endpoint, out session);
            }

            if (session != null)
            {
                session.Connection.Receive(data, size);
                return;
            }

            // only a valid syn creates a connection
            if (!RudpWire.IsSyn(data, size)) return;

            var remote = new IPEndPoint(endpoint.Address, endpoint.Port);
            var connection = new RudpConnection(owner, remote, true)
            {
                HandshakeTimeout = _handshakeTimeout,
                NoResponseTimeout = _noResponseTimeout
            };
            var client = new RudpClient(this, owner, connection, new Host(remote));

            session = new Session { Connection = connection, Client = client };

            connection.OnOpen += () =>
            {
                _onAccept?.Invoke(null, client);
                client.ServerOpened();
            };
            connection.OnOpenFail += _ => Remove(remote, connection);
            connection.OnClose += () => Remove(remote, connection);

            lock (_locker)
            {
                if (IsOpened is false || ReferenceEquals(Socket, owner) is false) return;

                _sessions[remote] = session;
            }

            connection.Receive(data, size);
        }

        private void Remove(IPEndPoint endpoint, RudpConnection connection)
        {
            lock (_locker)
            {
                if (_sessions.TryGetValue(endpoint, out var session) && session.Connection == connection)
                    _sessions.Remove(endpoint);
            }
        }
    }
}
