using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Netly.Packages.Interfaces;
using Netly.Packages.Utils;

namespace Netly.Packages
{
    public class NTcpServer : INTcpServer
    {
        private readonly object _locker = new object();
        internal readonly List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>> OnSecureObject;
        private byte[] _certificate;
        private string _certificatePassword;
        private List<NTcpClient> _clients;
        private long _framing;
        private EventHandler<NTcpClient> _onAccept;
        private EventHandler _onConnect, _onDisconnect;
        private EventHandler<Socket> _onCreate;
        private EventHandler<Exception> _onFail;

        private NTcpServer()
        {
            _clients = new List<NTcpClient>();
            Host = Host.Default;
            IsConnected = false;
            IsSecure = false;
            IsFraming = false;
            AllowUnsecured = false;
            FramingSize = NTcpFraming.DefaultSize;
            OnStreamObject = NTcpFraming.DefaultOnStream;
            OnSecureObject = new List<Func<X509Certificate, X509Chain, SslPolicyErrors, bool>>();
            Socket = null;
            Certificate = new X509Certificate();
            SecureProtocol = SslProtocols.Default;
        }

        public NTcpServer(bool isFraming) : this()
        {
            IsFraming = isFraming;
        }

        internal Func<long, Stream> OnStreamObject { get; private set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsConnected { get; private set; }
        public bool IsSecure { get; private set; }
        public bool IsFraming { get;}
        public bool AllowUnsecured { get; private set; }

        public long FramingSize
        {
            get => _framing;
            set
            {
                if (IsConnected)
                    throw new InvalidOperationException($"Must not update {nameof(FramingSize)} while now");
                _framing = Math.Max(1024, value);
            }
        }

        public IList<NTcpClient> Clients => _clients;
        public Socket Socket { get; private set; }
        public Host Host { get; private set; }
        public X509Certificate Certificate { get; private set; }

        public SslProtocols SecureProtocol { get; private set; }


        public void OnFail(Action<Exception> callback)
        {
            _onFail += (sender, exception) => callback?.Invoke(exception);
        }

        public void OnAccept(Action<NTcpClient> callback)
        {
            _onAccept += (sender, client) => callback?.Invoke(client);
        }

        public void OnCreate(Action<Socket> callback)
        {
            _onCreate += (sender, socket) => callback?.Invoke(socket);
        }

        public void OnStream(Func<long, Stream> callback)
        {
            OnStreamObject = callback;
        }

        public void OnConnect(Action callback)
        {
            _onConnect += (sender, _) => callback?.Invoke();
        }

        public void OnDisconnect(Action callback)
        {
            _onDisconnect += (sender, _) => callback?.Invoke();
        }

        public void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
        {
            OnSecureObject.Add(callback);
        }

        public void ToBroadcastMessage(byte[] message, Func<NTcpClient, bool> condition = null)
        {
            if (message == null || message.Length < 1) return;

            foreach (var client in condition == null ? _clients : _clients.Where(condition))
                client.ToMessage(message);
        }

        public void ToBroadcastEvent(string name, byte[] message, Func<NTcpClient, bool> condition = null)
        {
            if (message == null || message.Length < 1 || string.IsNullOrEmpty(name)) return;

            foreach (var client in condition == null ? _clients : _clients.Where(condition))
                client.ToEvent(name, message);
        }

        public void ToConnect(Host host)
        {
            ToConnectAsync(host);
        }

        public Task ToConnectAsync(Host host)
        {
            return ToConnectAsync(host, (int)SocketOptionName.MaxConnections);
        }

        public void ToConnect(Host host, int backlog)
        {
            ToConnectAsync(host, backlog);
        }

        public Task ToConnectAsync(Host host, int backlog)
        {
            if (IsConnected) return Task.CompletedTask;

            return Task.Run(() =>
            {
                lock (_locker)
                {
                    try
                    {
                        var socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        _onCreate?.Invoke(null, socket);

                        if (IsSecure) Certificate = new X509Certificate(_certificate, _certificatePassword);

                        socket.Bind(host.EndPoint);

                        socket.Listen(Math.Min(0, backlog));

                        Host = new Host(socket.LocalEndPoint);

                        Socket = socket;

                        _clients?.Clear();
                        _clients = new List<NTcpClient>();

                        IsConnected = true;
                        
                        _onConnect?.Invoke(null, null);

                        AcceptTrigger();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                        _onFail?.Invoke(null, e);
                    }
                }
            });
        }

        public void ToDisconnect()
        {
            ToDisconnectAsync();
        }

        public void ToSecure(byte[] certificate, string password, SslProtocols protocols, bool allowUnsecured)
        {
            if (IsConnected)
                throw new InvalidOperationException($"Can't call {nameof(ToConnect)}, {nameof(IsConnected)}");

            _certificate = certificate;
            _certificatePassword = password;
            SecureProtocol = protocols;
            AllowUnsecured = allowUnsecured;
            IsSecure = true;
        }

        public Task ToDisconnectAsync()
        {
            if (!IsConnected) return Task.CompletedTask;

            return Task.Run(() =>
            {
                try
                {
                    Socket?.Close();

                    lock (_locker)
                    {
                        _clients.ForEach(x => x.ToDisconnect());
                        _clients.Clear();
                    }
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
                finally
                {
                    IsConnected = false;
                    
                    Socket = null;
                    _clients.Clear();
                    _clients = new List<NTcpClient>();
                    _onDisconnect?.Invoke(null, null);
                }
            });
        }

        private void AcceptTrigger()
        {
            try
            {
                Socket.BeginAccept(AcceptHandler, null);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
                ToDisconnect();
            }
        }

        private void AcceptHandler(IAsyncResult result)
        {
            try
            {
                var socket = Socket.EndAccept(result);

                var client = new NTcpClient(socket, this);

                client.OnAccepted(isAccepted =>
                {
                    if (isAccepted)
                    {
                        client.OnDisconnect(() =>
                        {
                            lock (_locker)
                            {
                                Clients.Remove(client);
                            }
                        });

                        lock (_locker)
                        {
                            Clients.Add(client);
                        }

                        _onAccept?.Invoke(null, client);
                    }
                    else
                    {
                        client.ToDisconnect();
                        client = null;
                    }
                });

                Task.Run(client.ServerInitializer);
            }
            catch (Exception e)
            {
                NetlyEnvironment.Logger.Create(e);
            }

            if (IsConnected)
                AcceptTrigger();
            else
                ToDisconnect();
        }
    }
}