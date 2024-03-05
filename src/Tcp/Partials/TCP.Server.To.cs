using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
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
                public X509Certificate2 Certificate { get; private set; }
                public SslProtocols EncryptionProtocol { get; private set; }
                public List<IClient> Clients { get; private set; }

                private Socket _socket;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed;

                private _To()
                {
                    Clients = new List<IClient>();
                    _socket = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isClosed = true;
                    Host = Host.Default;
                    IsEncrypted = false;
                }

                public _To(Server server) : this()
                {
                    _server = server;
                }

                public Task Open(Host host)
                {
                    return Open(host, -1);
                }

                public Task Open(Host host, int backlog)
                {
                    if (_isOpening || _isClosing || IsOpened) return Task.CompletedTask;

                    _isOpening = true;

                    return Task.Run(async () =>
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

                            await InitAccept();
                        }
                        catch (Exception e)
                        {
                            On.m_onError?.Invoke(null, e);
                        }

                        _isOpening = false;
                        return Task.CompletedTask;
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

                            foreach (var client in Clients)
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

                public Task Encryption(bool enable, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols)
                {
                    if (IsOpened || _isClosing || _isOpening)
                    {
                        throw new InvalidOperationException
                        (
                            $"You must not update {nameof(Encryption)} while {nameof(IsOpened)} is {IsOpened}"
                        );
                    }

                    if (enable)
                    {
                        Certificate = new X509Certificate2(pfxCertificate, pfxPassword);
                        EncryptionProtocol = protocols;
                    }
                    else
                    {
                        IsEncrypted = false;
                    }

                    return Task.CompletedTask;
                }

                // ---

                private int ClampBacklog(int backlog)
                {
                    const int maxBacklog = (int)SocketOptionName.MaxConnections;
                    return (backlog <= 0 || backlog >= maxBacklog)
                        ? maxBacklog
                        : backlog;
                }

                private Task InitAccept()
                {
                    // true: thread is destroyed normally when program end (non-force required)
                    // false: this thread will be persistent and will block the destruction of main process (force quit required)
                    const bool isBackground = true;

                    Thread acceptThread = new Thread(AcceptJob) { IsBackground = isBackground };

                    acceptThread.Start();

                    return Task.CompletedTask;
                }

                private async void AcceptJob()
                {
                    while (IsOpened)
                    {
                        try
                        {
                            Socket socket = await _socket.AcceptAsync();

                            Client client = new Client(socket, _server, out bool success);

                            if (success)
                            {
                                Clients.Add(client);
                                client.InitServerSide();
                            }
                            else
                            {
                                socket.Close();
                                socket.Dispose();
                            }
                        }
                        catch
                        {
                            // Ignored
                        }
                    }
                }
            }
        }
    }
}