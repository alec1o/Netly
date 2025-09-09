using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server
        {
            internal class ServerTo : ITCP.ServerTo
            {
                private readonly int _defaultBacklog;
                private readonly object _lockClient = new object();
                private readonly Server _server;
                private byte[] _pfxCertificate;
                private string _pfxPassword;
                private SslProtocols _pfxCertificateProtocol;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed,
                    _enableEncryption;


                private Socket _socket;

                private ServerTo()
                {
                    _socket = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isClosed = true;
                    Host = NHost.Default;
                    IsEncrypted = false;
                    _defaultBacklog = (int)SocketOptionName.MaxConnections;
                }

                public ServerTo(Server server) : this()
                {
                    _server = server;
                }

                public bool IsOpened => _socket != null;
                public NHost Host { get; private set; }
                private ServerOn On => _server._on;
                public bool IsEncrypted { get; private set; }
                public X509Certificate Certificate { get; private set; }
                public SslProtocols EncryptionProtocol { get; private set; }
                public readonly List<ITCP.Client> Clients = new List<ITCP.Client>();

                public Task Open(NHost host)
                {
                    return Open(host, _defaultBacklog);
                }

                public Task Open(NHost host, int backlog)
                {
                    if (_isOpening || _isClosing || IsOpened) return Task.CompletedTask;

                    _isOpening = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            var socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            On.OnModify?.Invoke(null, socket);

                            if (_enableEncryption)
                            {
                                Certificate = new X509Certificate(_pfxCertificate, _pfxPassword);
                                EncryptionProtocol = _pfxCertificateProtocol;
                                IsEncrypted = _enableEncryption;
                            }

                            socket.Bind(host.EndPoint);

                            socket.Listen(ClampBacklog(backlog));


                            Host = new NHost(socket.LocalEndPoint);

                            _socket = socket;

                            _isClosed = false;

                            On.OnOpen?.Invoke(null, null);

                            InitAccept();
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                            On.OnError?.Invoke(null, e);
                        }

                        _isOpening = false;
                    });
                }

                public Task Close()
                {
                    if (!IsOpened || _isClosed || _isClosing) return Task.CompletedTask;

                    _isClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket.Close();

                            lock (_lockClient)
                            {
                                Clients.ForEach(x => _ = x.To.Close());
                            }
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                        }
                        finally
                        {
                            _socket = null;
                            _isClosed = true;
                            _isClosing = false;
                            Clients.Clear();
                            On.OnClose?.Invoke(null, null);
                        }
                    });
                }

                public void DataBroadcast(string data)
                {
                    foreach (var client in Clients.ToArray()) client.To.Data(data);
                }

                public void DataBroadcast(string data, Encoding encoding)
                {
                    foreach (var client in Clients.ToArray()) client.To.Data(data, encoding);
                }

                public void DataBroadcast(byte[] data)
                {
                    foreach (var client in Clients.ToArray()) client.To.Data(data);
                }

                public void EventBroadcast(string name, string data)
                {
                    foreach (var client in Clients.ToArray()) client.To.Event(name, data);
                }

                public void EventBroadcast(string name, string data, Encoding encoding)
                {
                    foreach (var client in Clients.ToArray()) client.To.Data(data, encoding);
                }

                public void EventBroadcast(string name, byte[] data)
                {
                    foreach (var client in Clients.ToArray()) client.To.Event(name, data);
                }

                public void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword,
                    SslProtocols protocols)
                {
                    if (IsOpened || _isClosing || _isOpening)
                        throw new InvalidOperationException
                        (
                            $"You must not update {nameof(Encryption)} while {nameof(IsOpened)} is {IsOpened}"
                        );

                    _enableEncryption = enableEncryption;
                    _pfxCertificate = pfxCertificate;
                    _pfxPassword = pfxPassword;
                    _pfxCertificateProtocol = protocols;
                }

                // ---

                private int ClampBacklog(int backlog)
                {
                    return backlog <= 0 || backlog >= _defaultBacklog ? _defaultBacklog : backlog;
                }

                private void InitAccept()
                {
                    AcceptTrigger();
                }

                private void AcceptTrigger()
                {
                    try
                    {
                        _socket.BeginAccept(AcceptHandler, null);
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                        Close();
                    }
                }

                private void AcceptHandler(IAsyncResult result)
                {
                    try
                    {
                        var socket = _socket.EndAccept(result);

                        var client = new Client(socket, _server, it =>
                        {
                            it.On.Close(() =>
                            {
                                lock (_lockClient)
                                {
                                    Clients.Remove(it);
                                }
                            });

                            lock (_lockClient)
                            {
                                Clients.Add(it);
                            }

                            On.OnAccept?.Invoke(null, it);

                            it.InitServerSide();
                        });

                        Task.Run(client.InitServerValidator);
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }

                    if (IsOpened)
                        AcceptTrigger();
                    else
                        Close();
                }

                public Socket GetSocket()
                {
                    return _socket ?? new Socket(Host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }
            }
        }
    }
}