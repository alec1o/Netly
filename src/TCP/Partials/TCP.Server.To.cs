using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server
        {
            internal class _To : ITo
            {
                private readonly Server _server;
                public bool IsOpened => _socket != null;
                public Host Host { get; private set; }
                private _On On => _server._on;
                public bool IsEncrypted { get; private set; }
                public X509Certificate Certificate { get; private set; }
                public SslProtocols EncryptionProtocol { get; private set; }
                public Dictionary<string, IClient> Clients { get; private set; }


                private Socket _socket;
                private readonly object _lockAccept, _lockClient;
                private readonly List<Socket> _socketList;
                private readonly int _defaultBacklog;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed;

                private _To()
                {
                    Clients = new Dictionary<string, IClient>();
                    _socketList = new List<Socket>();
                    _socket = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isClosed = true;
                    Host = Host.Default;
                    IsEncrypted = false;
                    _lockAccept = new object();
                    _lockClient = new object();
                    _defaultBacklog = (int)SocketOptionName.MaxConnections;
                }

                public _To(Server server) : this()
                {
                    _server = server;
                }

                public void Open(Host host) => Open(host, _defaultBacklog);

                public void Open(Host host, int backlog)
                {
                    if (_isOpening || _isClosing || IsOpened) return;

                    _isOpening = true;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            Socket socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            On.m_onModify?.Invoke(null, socket);

                            socket.Bind(host.EndPoint);

                            socket.Listen(ClampBacklog(backlog));

                            Host = new Host(socket.LocalEndPoint);

                            _socket = socket;

                            _isClosed = false;

                            On.m_onOpen?.Invoke(null, null);

                            InitAccept();
                        }
                        catch (Exception e)
                        {
                            On.m_onError?.Invoke(null, e);
                        }

                        _isOpening = false;
                    });
                }

                public void Close()
                {
                    if (!IsOpened || _isClosed || _isClosing) return;

                    _isClosing = true;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            _socket.Close();

                            foreach (var client in Clients.Values)
                            {
                                client.To.Close();
                            }

                            _socket.Dispose();
                        }
                        catch
                        {
                            // Ignored
                        }
                        finally
                        {
                            _socket = null;
                            _isClosed = true;
                            _isClosing = false;
                            Clients.Clear();
                            On.m_onClose?.Invoke(null, null);
                        }
                    });
                }

                public void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword,
                    SslProtocols protocols)
                {
                    if (IsOpened || _isClosing || _isOpening)
                    {
                        throw new InvalidOperationException
                        (
                            $"You must not update {nameof(Encryption)} while {nameof(IsOpened)} is {IsOpened}"
                        );
                    }


                    if (enableEncryption)
                    {
                        Certificate = new X509Certificate(pfxCertificate, pfxPassword);
                        EncryptionProtocol = protocols;
                    }

                    IsEncrypted = enableEncryption;
                }

                // ---

                private int ClampBacklog(int backlog)
                {
                    return (backlog <= 0) || (backlog >= _defaultBacklog) ? _defaultBacklog : backlog;
                }

                private void InitAccept()
                {
                    // true: thread is destroyed normally when program end (non-force required)
                    // false: this thread will be persistent and will block the destruction of main process (force quit required)
                    const bool isBackground = true;
                    // *-*-*-*-*-*-*-*-*-*-*-*-*-*-
                    new Thread(AcceptJob) { IsBackground = isBackground }.Start();
                    new Thread(InitClientJob) { IsBackground = isBackground }.Start();
                }

                private void RemoveClient(string id)
                {
                    lock (_lockClient)
                    {
                        Clients.Remove(id);
                    }
                }

                private void AcceptJob()
                {
                    while (IsOpened)
                    {
                        try
                        {
                            Socket socket = _socket.Accept();

                            lock (_lockAccept)
                            {
                                _socketList.Add(socket);
                            }
                        }
                        catch
                        {
                            // Ignored
                        }
                    }
                }

                private void InitClientJob()
                {
                    while (IsOpened)
                    {
                        // it is just check.
                        // it mustn't use lock for not use lock resources and decrees accept performance
                        // *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
                        // ReSharper disable once InconsistentlySynchronizedField
                        // *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
                        if (_socketList.Count <= 0) continue;

                        Socket socket;

                        lock (_lockAccept)
                        {
                            // FIFO: (First In First Out) strategy
                            socket = _socketList[0];
                            _socketList.RemoveAt(0);
                        }

                        Client client = new Client(socket, _server, out bool success);

                        if (success)
                        {
                            lock (_lockClient)
                            {
                                client.On.Close(() => RemoveClient(client.Id));

                                Clients.Add(client.Id, client);

                                On.m_onAccept?.Invoke(null, client);

                                client.InitServerSide();
                            }
                        }
                        else
                        {
                            socket.Close();
                            socket.Dispose();
                        }
                    }
                }
            }
        }
    }
}