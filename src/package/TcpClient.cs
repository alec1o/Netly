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
    ///     TCP client, also the type of each client accepted by <see cref="TcpServer" />.
    /// </summary>
    public class TcpClient
    {
        private const int MergeLimit = 16 * 1024;

        private readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> _onSecure =
            new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();

        private readonly object _locker = new object();
        private readonly object _sendLocker = new object();
        private readonly Framing _framing = new Framing();
        private byte[] _certificate;
        private string _certificatePassword;
        private bool _isOpening, _isClosing;
        private NetworkStream _networkStream;
        private Action<bool> _onAccepted;
        private EventHandler _onOpen, _onClose;
        private EventHandler<Socket> _onModify;
        private EventHandler<(string Name, Stream Stream)> _onEvent;
        private EventHandler<Exception> _onError;
        private EventHandler<Stream> _onData;
        private Func<long, Stream> _onAllocate;
        private SslStream _secureStream;
        private Func<long, Stream> Allocate => IsServer ? _server.OnAllocateObject : _onAllocate;

        /* ---- CONSTRUCTOR --- */

        private TcpClient()
        {
            _server = null;
            _onAllocate = Helper.NewStream;
            Host = Host.Default;
            IsOpened = false;
            SecureProtocol = SslProtocols.None;
            SecureDomain = string.Empty;
            IsSecure = false;
            IsFraming = false;
            IsServer = false;
            FramingSize = Framing.DefaultSize;
            Socket = null;
            Certificate = null;
        }

        /// <summary>
        ///     Creates a client.
        /// </summary>
        /// <param name="isFraming">
        ///     When true each <see cref="OnData" /> is exactly one sent message,
        ///     otherwise it is whatever bytes arrived.
        /// </param>
        public TcpClient(bool isFraming) : this()
        {
            IsFraming = isFraming;
        }

        internal TcpClient(Socket socket, TcpServer server) : this()
        {
            IsFraming = server.IsFraming;
            IsSecure = server.IsSecure;
            IsServer = true;
            IsOpened = true;
            Socket = socket;
            Host = new Host(socket.RemoteEndPoint);
            _framing.MaxSize = server.FramingSize;
            _server = server;
        }

        private TcpServer _server { get; }

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();
        /// <summary>
        ///     True while the connection is open.
        /// </summary>
        public bool IsOpened { get; private set; }
        /// <summary>
        ///     True when the connection uses TLS.
        /// </summary>
        public bool IsSecure { get; private set; }
        /// <summary>
        ///     True when each message is delimited, so one send is one <see cref="OnData" />.
        /// </summary>
        public bool IsFraming { get; }
        /// <summary>
        ///     True when this client was accepted by a <see cref="TcpServer" />.
        /// </summary>
        public bool IsServer { get; }
        /// <summary>
        ///     TLS protocols allowed for this connection.
        /// </summary>
        public SslProtocols SecureProtocol { get; private set; }
        /// <summary>
        ///     Server name used to validate the certificate. Empty means the host address.
        /// </summary>
        public string SecureDomain { get; private set; }

        /// <summary>
        ///     Biggest message accepted when <see cref="IsFraming" /> is true.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public long FramingSize
        {
            get => _framing.MaxSize;
            set
            {
                if (IsOpened || IsServer)
                    throw new InvalidOperationException($"Must not update ({nameof(FramingSize)}) while socket is Opened.");

                _framing.MaxSize = Math.Max(1024, value);
            }
        }

        /// <summary>
        ///     The underlying socket. It is null while the client is closed.
        /// </summary>
        public Socket Socket { get; private set; }
        /// <summary>
        ///     The stream used to read and write. It is the TLS stream when <see cref="IsSecure" />.
        /// </summary>
        public Stream Stream => IsSecure ? _secureStream : (Stream)_networkStream;
        /// <summary>
        ///     Address of the remote side.
        /// </summary>
        public Host Host { get; private set; }
        /// <summary>
        ///     Certificate sent to the server. It is null when none was set.
        /// </summary>
        public X509Certificate Certificate { get; private set; }

        /// <summary>
        ///     Called with the new socket before it connects, change the socket options here.
        /// </summary>
        public void OnModify(Action<Socket> callback)
        {
            _onModify += (sender, socket) => callback?.Invoke(socket);
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
        ///     Called when the connection fails to open, when a send fails or when a callback throws while receiving.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /// <summary>
        ///     Called for each received data.
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
        ///     Chooses where each received data is written, receives the size of the data.
        ///     Return a <see cref="FileStream" /> to keep big data out of memory.
        /// </summary>
        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public void OnAllocate(Func<long, Stream> callback)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not update ({nameof(OnAllocate)}) while socket is Opened.");

            _onAllocate = callback ?? Helper.NewStream;
        }

        /// <summary>
        ///     Validates the server certificate, without callbacks only valid certificates are accepted.
        /// </summary>
        public void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
        {
            if (callback == null) return;

            _onSecure.Add(callback);
        }

        /// <exception cref="InvalidOperationException">While the socket is opened.</exception>
        public void ToSecure(bool allowSecure, byte[] certificate = null, string password = null,
            SslProtocols protocols = SslProtocols.None, string secureDomain = null)
        {
            if (IsOpened || IsServer)
                throw new InvalidOperationException($"Must not modify ({nameof(IsSecure)}) while socket is Opened.");

            IsSecure = allowSecure;
            SecureProtocol = protocols;
            SecureDomain = secureDomain ?? string.Empty;
            _certificatePassword = password;
            _certificate = certificate;
        }

        /* ---- SEND --- */

        /// <summary>
        ///     Sends the data from <see cref="System.IO.Stream.Position" /> to the end, the stream is not closed
        ///     and its position is restored.
        /// </summary>
        /// <exception cref="ArgumentException">When the stream can't seek.</exception>
        public void SendData(Stream data)
        {
            Send(null, data);
        }

        /// <summary>
        ///     Sends the whole array.
        /// </summary>
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
        ///     Sends all segments as one data, without joining them first.
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
            Send(Header(name, data), data);
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
        ///     Sends a named event with all segments as content, without copying them to a new array first.
        /// </summary>
        public void SendEvent(string name, IList<ArraySegment<byte>> data)
        {
            Send(Header(name, data), data);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Starts the connection and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="host" /> is null.</exception>
        public void Open(Host host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            _ = OpenAsync(host);
        }

        // every failure goes to OnError, it never throws
        private async Task OpenAsync(Host host)
        {
            lock (_locker)
            {
                if (_isOpening || _isClosing || IsOpened || IsServer) return;

                _isOpening = true;
            }

            Socket socket = null;

            try
            {
                socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                _onModify?.Invoke(null, socket);

                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host.Address, host.Port, null);

                var networkStream = new NetworkStream(socket, true);

                if (IsSecure)
                {
                    Certificate = _certificate == null ? null : new X509Certificate(_certificate, _certificatePassword);

                    // REQUIRED: Save stream state, the connection must be secure before any read or write
                    _secureStream = await InitializeSecureClient(networkStream, host);
                }

                lock (_locker)
                {
                    _networkStream = networkStream;
                    Socket = socket;
                    Host = new Host(socket.RemoteEndPoint);
                    IsOpened = true;
                }

                _framing.Open();

                // allow client send data on OnOpen callback.
                _onOpen?.Invoke(null, null);

                InitReceiver(socket);
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
                    _secureStream = null;
                    _networkStream = null;
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
        }

        /// <summary>
        ///     Closes the connection and returns without waiting, <see cref="OnClose" /> is called when it is done.
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

        internal void OnAccepted(Action<bool> callback)
        {
            _onAccepted = callback;
        }

        internal void ServerInitializer()
        {
            var accepted = false;

            try
            {
                var networkStream = new NetworkStream(Socket, true);

                if (IsSecure)
                {
                    // allow plain connections when the first byte is not a TLS handshake record
                    var isTls = !_server.AllowUnsecured || IsTlsHandshake(Socket);

                    if (isTls)
                        _secureStream = InitializeSecureServer(networkStream).GetAwaiter().GetResult();
                    else
                        IsSecure = false;
                }

                _networkStream = networkStream;

                _framing.Open();

                accepted = true;
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            _onAccepted?.Invoke(accepted);

            if (accepted) InitReceiver(Socket);
        }

        /* ---- SEND HELPERS --- */

        private static ArraySegment<byte> Segment(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (offset < 0 || count < 0 || offset > data.Length - count)
                throw new ArgumentOutOfRangeException($"{nameof(offset)}: {offset}, {nameof(count)}: {count}");

            return new ArraySegment<byte>(data, offset, count);
        }

        private static byte[] Header(string name, IList<ArraySegment<byte>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            return Header(name, data.Sum(x => (long)x.Count));
        }

        private static byte[] Header(string name, Stream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.CanSeek is false)
                throw new ArgumentException($"{nameof(data)}: {nameof(data.CanSeek)} = {data.CanSeek}");

            return Header(name, data.Length - data.Position);
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

            var head = Head(header, count);

            try
            {
                var stream = Stream;

                if (IsOpened is false || stream == null) return;

                // REQUIRED: one writer at time, framing prefix and body must not interleave
                lock (_sendLocker)
                {
                    // small data goes in a single write
                    if (head.Length + count <= MergeLimit)
                    {
                        var merged = new byte[head.Length + count];

                        Buffer.BlockCopy(head, 0, merged, 0, head.Length);

                        var offset = head.Length;

                        foreach (var segment in body)
                        {
                            Buffer.BlockCopy(segment.Array, segment.Offset, merged, offset, segment.Count);
                            offset += segment.Count;
                        }

                        stream.Write(merged, 0, merged.Length);
                        return;
                    }

                    stream.Write(head, 0, head.Length);

                    foreach (var segment in body) stream.Write(segment.Array, segment.Offset, segment.Count);
                }
            }
            catch (Exception e)
            {
                SendFail(e);
            }
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

            var head = Head(header, count);

            try
            {
                var stream = Stream;

                if (IsOpened is false || stream == null) return;

                var buffer = new byte[Math.Min(count, 81920)];

                // REQUIRED: one writer at time, framing prefix and body must not interleave
                lock (_sendLocker)
                {
                    stream.Write(head, 0, head.Length);

                    for (var left = count; left > 0;)
                    {
                        var read = body.Read(buffer, 0, (int)Math.Min(buffer.Length, left));

                        if (read <= 0) throw new EndOfStreamException($"{nameof(body)}: {body.Position}");

                        stream.Write(buffer, 0, read);
                        left -= read;
                    }
                }
            }
            catch (Exception e)
            {
                SendFail(e);
            }
            finally
            {
                // allow send the same stream again
                body.Position = start;
            }
        }

        // framing prefix and event header
        private byte[] Head(byte[] header, long bodySize)
        {
            var prefix = IsFraming ? Framing.Create((header?.Length ?? 0) + bodySize) : Array.Empty<byte>();

            if (header == null) return prefix;

            var head = new byte[prefix.Length + header.Length];

            Buffer.BlockCopy(prefix, 0, head, 0, prefix.Length);
            Buffer.BlockCopy(header, 0, head, prefix.Length, header.Length);

            return head;
        }

        private void SendFail(Exception e)
        {
            Logger.Singleton.Submit(e);
            _onError?.Invoke(null, e);
            Close();
        }

        /* ---- RECEIVE --- */

        private void InitReceiver(Socket owner)
        {
            var bufferSize = (int)owner.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);

            ReceiveTrigger(owner, new byte[bufferSize > 0 ? bufferSize : 4096]);
        }

        private void ReceiveTrigger(Socket owner, byte[] buffer)
        {
            try
            {
                Stream.BeginRead(buffer, 0, buffer.Length, ReceiveHandler, (owner, buffer));
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
                Close(owner);
            }
        }

        private void ReceiveHandler(IAsyncResult result)
        {
            var (owner, buffer) = ((Socket, byte[]))result.AsyncState;

            try
            {
                var size = Stream.EndRead(result);

                if (size <= 0)
                {
                    Close(owner);
                    return;
                }

                if (IsFraming)
                {
                    _framing.Write(new ArraySegment<byte>(buffer, 0, size), Allocate);
                    while (_framing.Read(out var stream)) ReceiveRelease(stream);
                }
                else
                {
                    var stream = Allocate(size);
                    stream.Write(buffer, 0, size);
                    ReceiveRelease(stream);
                }

                ReceiveTrigger(owner, buffer);
            }
            catch (Exception e)
            {
                // connection closed by this side or by the peer
                if (IsOpened) Logger.Singleton.Submit(e);
                Close(owner);
            }
        }

        private void ReceiveRelease(Stream stream)
        {
            try
            {
                if (stream.Length < 1) throw new InvalidDataException($"{nameof(stream)}: {stream.Length}");

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

        /* ---- CLOSE --- */

        // owner avoids an old connection closing a new one.
        private void Close(Socket owner)
        {
            lock (_locker)
            {
                if (_isClosing || IsOpened is false || ReferenceEquals(Socket, owner) is false) return;

                _isClosing = true;
            }

            try
            {
                _framing.Close();
                _secureStream?.Dispose();
                _networkStream?.Dispose();
                owner.Close();
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
                    _networkStream = null;
                    _secureStream = null;
                    _isClosing = false;
                    if (IsServer is false) Socket = null;
                }

                _onClose?.Invoke(null, null);
            }
        }

        /* ---- TLS --- */

        private static bool IsTlsHandshake(Socket socket)
        {
            var first = new byte[1];

            return socket.Receive(first, 0, 1, SocketFlags.Peek) == 1 && first[0] == 0x16;
        }

        private async Task<SslStream> InitializeSecureClient(NetworkStream networkStream, Host host)
        {
            var secure = new SslStream(networkStream, false, ValidateCertificate);

            var domain = string.IsNullOrWhiteSpace(SecureDomain) ? host.Address.ToString() : SecureDomain;

            var collection = Certificate == null
                ? new X509CertificateCollection()
                : new X509CertificateCollection(new[] { Certificate });

            await Timeout(secure.AuthenticateAsClientAsync(domain, collection, SecureProtocol, false), secure);

            return secure;
        }

        private async Task<SslStream> InitializeSecureServer(NetworkStream networkStream)
        {
            var secure = new SslStream(networkStream, false);

            await Timeout(secure.AuthenticateAsServerAsync
            (
                serverCertificate: _server.Certificate,
                clientCertificateRequired: false,
                enabledSslProtocols: _server.SecureProtocol,
                checkCertificateRevocation: false
            ), secure);

            return secure;
        }

        private static async Task Timeout(Task handshake, SslStream secure)
        {
            var finished = await Task.WhenAny(handshake, Task.Delay(TimeSpan.FromSeconds(7)));

            if (finished != handshake)
            {
                secure.Dispose();
                throw new TimeoutException($"TLS handshake timeout ({nameof(TcpClient)})");
            }

            await handshake;
        }

        // without callbacks, only valid certificates are accepted
        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
        {
            if (_onSecure.Count <= 0) return errors == SslPolicyErrors.None;

            return _onSecure.All(callback => callback.Invoke(certificate, chain, errors));
        }
    }
}
