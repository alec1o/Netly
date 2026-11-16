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
    ///     UDP server. A client is created for each address that sends a datagram.
    /// </summary>
    public class UdpServer
    {
        private readonly object _locker = new object();
        private Dictionary<IPEndPoint, UdpClient> _clients;
        private bool _isOpening, _isClosing;
        private EventHandler<UdpClient> _onAccept;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<Exception> _onError;

        /* ---- CONSTRUCTOR --- */

        /// <summary>
        ///     Creates a server.
        /// </summary>
        public UdpServer()
        {
            _clients = new Dictionary<IPEndPoint, UdpClient>();
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
        ///     Snapshot of the known clients.
        /// </summary>
        public IReadOnlyList<UdpClient> Clients
        {
            get
            {
                lock (_locker)
                {
                    return _clients.Values.ToArray();
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
        ///     Called for each new address that sends a datagram, before the datagram is delivered.
        /// </summary>
        public void OnAccept(Action<UdpClient> callback)
        {
            _onAccept += (sender, client) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(client));
        }

        /// <summary>
        ///     Chooses where each datagram received from the clients is written, receives the size of the datagram.
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
        public void BroadcastData(byte[] data, Func<UdpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendData(data);
        }

        /// <summary>
        ///     Sends a named event to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastEvent(string name, byte[] data, Func<UdpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendEvent(name, data);
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

                    DisableConnectionReset(socket);

                    socket.Bind(host.EndPoint);

                    lock (_locker)
                    {
                        Host = new Host(socket.LocalEndPoint);
                        Socket = socket;
                        _clients = new Dictionary<IPEndPoint, UdpClient>();
                        IsOpened = true;

                        // Open can be called again from the OnOpen callback
                        _isOpening = false;
                    }

                    _onOpen?.Invoke(null, null);

                    ReceiveTrigger(socket, new byte[UdpClient.MaxDatagramSize]);
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
        ///     Closes the socket and all clients and returns without waiting.
        ///     <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            if (!IsOpened) return;

            Task.Run(() =>
            {
                UdpClient[] clients;
                Socket socket;

                lock (_locker)
                {
                    if (_isClosing || IsOpened is false) return;

                    _isClosing = true;
                    IsOpened = false;
                    socket = Socket;
                    clients = _clients.Values.ToArray();
                    _clients.Clear();
                }

                try
                {
                    foreach (var client in clients) client.Close(socket);

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

        internal void Remove(UdpClient client)
        {
            lock (_locker)
            {
                if (_clients.TryGetValue(client.Host.IPEndPoint, out var current) && ReferenceEquals(current, client))
                    _clients.Remove(client.Host.IPEndPoint);
            }
        }

        /* ---- RECEIVE --- */

        // windows reports a closed remote port as an error on the next receive of the socket, it is not an error
        private static void DisableConnectionReset(Socket socket)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    socket.IOControl(-1744830452, new byte[] { 0 }, null);
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }
        }

        private void ReceiveTrigger(Socket owner, byte[] buffer)
        {
            try
            {
                EndPoint remote = new IPEndPoint(
                    owner.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

                owner.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote, ReceiveHandler,
                    (owner, buffer));
            }
            catch (Exception e)
            {
                if (IsOpened) Logger.Singleton.Submit(e);
                Close();
            }
        }

        private void ReceiveHandler(IAsyncResult result)
        {
            var (owner, buffer) = ((Socket, byte[]))result.AsyncState;

            try
            {
                EndPoint remote = new IPEndPoint(
                    owner.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

                var size = owner.EndReceiveFrom(result, ref remote);

                // empty datagram carries nothing and does not create a client
                if (size > 0) Release((IPEndPoint)remote, owner, buffer, size);
            }
            catch (ObjectDisposedException)
            {
                // socket closed
                return;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset ||
                                            e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                // nobody is listening on a client address (ICMP), UDP has no connection to lose
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            if (IsOpened && ReferenceEquals(Socket, owner))
                ReceiveTrigger(owner, buffer);
        }

        private void Release(IPEndPoint remote, Socket owner, byte[] buffer, int size)
        {
            UdpClient client;
            var isNew = false;

            lock (_locker)
            {
                if (IsOpened is false || ReferenceEquals(Socket, owner) is false) return;

                if (_clients.TryGetValue(remote, out client) is false)
                {
                    client = new UdpClient(this, owner, new Host(remote));
                    _clients.Add(remote, client);
                    isNew = true;
                }
            }

            if (isNew) _onAccept?.Invoke(null, client);

            client.Release(buffer, size);
        }
    }
}
