using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    ///     TCP server. Each accepted connection is a <see cref="TcpClient" />.
    /// </summary>
    public class TcpServer
    {
        private readonly object _locker = new object();
        private byte[] _certificate;
        private string _certificatePassword;
        private List<TcpClient> _clients;
        private long _framing;
        private bool _isOpening, _isClosing;
        private EventHandler<TcpClient> _onAccept;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<Exception> _onError;
        internal readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> OnSecureObject;

        /* ---- CONSTRUCTOR --- */

        private TcpServer()
        {
            _clients = new List<TcpClient>();
            Host = Host.Default;
            IsOpened = false;
            IsSecure = false;
            IsFraming = false;
            AllowUnsecured = false;
            FramingSize = Framing.DefaultSize;
            OnAllocateObject = Helper.NewStream;
            OnSecureObject = new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();
            Socket = null;
            Certificate = null;
            SecureProtocol = SslProtocols.None;
        }

        /// <summary>
        ///     Creates a server.
        /// </summary>
        /// <param name="isFraming">
        ///     When true each <see cref="TcpClient.OnData" /> is exactly one sent message,
        ///     otherwise it is whatever bytes arrived.
        /// </param>
        public TcpServer(bool isFraming = true) : this()
        {
            IsFraming = isFraming;
        }

        /* ---- INTERFACE --- */

        internal Func<long, Stream> OnAllocateObject { get; private set; }
        /// <summary>
        ///     Unique id of this server.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();
        /// <summary>
        ///     True while the server is listening.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        ///     True when the server uses TLS.
        /// </summary>
        public bool IsSecure { get; private set; }

        /// <summary>
        ///     True when each message is delimited, so one send is one <see cref="TcpClient.OnData" />.
        /// </summary>
        public bool IsFraming { get; }

        /// <summary>
        ///     True when clients without TLS are accepted on a secure server.
        /// </summary>
        public bool AllowUnsecured { get; private set; }

        /// <summary>
        ///     True when clients must send a certificate during the TLS handshake. Default is false.
        /// </summary>
        public bool ClientCertificateRequired { get; private set; }

        /// <summary>
        ///     Biggest message accepted from each client when <see cref="IsFraming" /> is true.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public long FramingSize
        {
            get => _framing;
            set
            {
                if (IsOpened)
                    throw new InvalidOperationException($"Must not update ({nameof(FramingSize)}) while socket is Opened.");

                _framing = Math.Max(1024, value);
            }
        }

        /// <summary>
        ///     Snapshot of the connected clients.
        /// </summary>
        public IReadOnlyList<TcpClient> Clients
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
        ///     The listening socket. It is null while the server is closed.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        ///     Address where the server is listening.
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        ///     Server certificate. It is null when TLS is not used.
        /// </summary>
        public X509Certificate Certificate { get; private set; }

        /// <summary>
        ///     TLS protocols allowed for the clients.
        /// </summary>
        public SslProtocols SecureProtocol { get; private set; }

        /// <summary>
        ///     Called with the new socket before it binds, change the socket options here.
        /// </summary>
        public void OnModify(Action<Socket> callback)
        {
            _onModify += (sender, socket) => callback?.Invoke(socket);
        }

        /// <summary>
        ///     Called when the server starts listening.
        /// </summary>
        public void OnOpen(Action callback)
        {
            _onOpen += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the server is closed.
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
        ///     Called for each client accepted, after the TLS handshake when <see cref="IsSecure" />.
        /// </summary>
        public void OnAccept(Action<TcpClient> callback)
        {
            _onAccept += (sender, client) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(client));
        }

        /// <summary>
        ///     Chooses where each data received from the clients is written, receives the size of the data.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public void OnAllocate(Func<long, Stream> callback)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not update ({nameof(OnAllocate)}) while socket is Opened.");

            OnAllocateObject = callback ?? Helper.NewStream;
        }

        /// <summary>
        ///     Validates the certificate of a client. Not used when no client certificate is required.
        /// </summary>
        public void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
        {
            if (callback == null) return;

            OnSecureObject.Add(callback);
        }

        /// <param name="certificate">Server certificate (PKCS#12 bytes).</param>
        /// <param name="password">Password of <paramref name="certificate" />.</param>
        /// <param name="protocols">Allowed TLS protocols. None uses the system default.</param>
        /// <param name="allowUnsecured">Accept clients that don't start a TLS handshake.</param>
        /// <param name="clientCertificateRequired">Refuse clients that don't send a certificate.</param>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public void ToSecure(byte[] certificate, string password, SslProtocols protocols, bool allowUnsecured,
            bool clientCertificateRequired = false)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not modify ({nameof(IsSecure)}) while socket is Opened.");

            _certificate = certificate;
            _certificatePassword = password;
            SecureProtocol = protocols;
            AllowUnsecured = allowUnsecured;
            ClientCertificateRequired = clientCertificateRequired;
            IsSecure = true;
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastData(byte[] data, Func<TcpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendData(data);
        }

        /// <summary>
        ///     Sends a named event to all clients, or only to the ones that match <paramref name="condition" />.
        /// </summary>
        public void BroadcastEvent(string name, byte[] data, Func<TcpClient, bool> condition = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            foreach (var client in condition == null ? Clients : Clients.Where(condition))
                client.SendEvent(name, data);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts listening and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Host host)
        {
            Open(host, (int)SocketOptionName.MaxConnections);
        }

        /// <param name="host">Address to listen on.</param>
        /// <param name="backlog">Size of the queue of connections waiting to be accepted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Host host, int backlog)
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
                    socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    _onModify?.Invoke(null, socket);

                    if (IsSecure) Certificate = new X509Certificate(_certificate, _certificatePassword);

                    socket.Bind(host.EndPoint);

                    socket.Listen(backlog > 0 ? backlog : (int)SocketOptionName.MaxConnections);

                    lock (_locker)
                    {
                        Host = new Host(socket.LocalEndPoint);
                        Socket = socket;
                        _clients = new List<TcpClient>();
                        IsOpened = true;

                        // Open can be called again from the OnOpen callback
                        _isOpening = false;
                    }

                    _onOpen?.Invoke(null, null);

                    AcceptTrigger(socket);
                }
                catch (Exception e)
                {
                    socket?.Close();

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
        ///     Stops listening, closes all clients and returns without waiting.
        ///     <see cref="OnClose" /> is called when it is done.
        /// </summary>
        public void Close()
        {
            if (!IsOpened) return;

            Task.Run(() =>
            {
                TcpClient[] clients;
                Socket socket;

                lock (_locker)
                {
                    if (_isClosing || IsOpened is false) return;

                    _isClosing = true;
                    IsOpened = false;
                    socket = Socket;
                    clients = _clients.ToArray();
                    _clients.Clear();
                }

                try
                {
                    socket?.Close();

                    foreach (var client in clients) client.Close();
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

        /* ---- ACCEPT --- */

        private void AcceptTrigger(Socket owner)
        {
            try
            {
                owner.BeginAccept(AcceptHandler, owner);
            }
            catch (Exception e)
            {
                // listener closed
                if (IsOpened) Logger.Singleton.Submit(e);
                Close();
            }
        }

        private void AcceptHandler(IAsyncResult result)
        {
            var owner = (Socket)result.AsyncState;

            try
            {
                var socket = owner.EndAccept(result);

                var client = new TcpClient(socket, this);

                client.OnAccepted(isAccepted =>
                {
                    if (isAccepted is false || Register(client) is false) client.Close();
                });

                Task.Run(client.ServerInitializer);
            }
            catch (ObjectDisposedException)
            {
                // listener closed
                return;
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            if (IsOpened && ReferenceEquals(Socket, owner))
                AcceptTrigger(owner);
        }

        // a client accepted while the server is closing must not leak
        private bool Register(TcpClient client)
        {
            lock (_locker)
            {
                if (IsOpened is false) return false;

                _clients.Add(client);
            }

            client.OnClose(() =>
            {
                lock (_locker)
                {
                    _clients.Remove(client);
                }
            });

            // client may have closed before the callback was registered
            if (client.IsOpened is false)
                lock (_locker)
                {
                    _clients.Remove(client);
                }

            _onAccept?.Invoke(null, client);

            return true;
        }
    }
}
