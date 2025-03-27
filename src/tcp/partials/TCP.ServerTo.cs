﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
                private readonly object _lockAccept, _lockClient;
                private readonly Server _server;
                private readonly List<Socket> _socketList;
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
                    Clients = new Dictionary<string, ITCP.Client>();
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

                public ServerTo(Server server) : this()
                {
                    _server = server;
                }

                public bool IsOpened => _socket != null;
                public Host Host { get; private set; }
                private ServerOn On => _server._on;
                public bool IsEncrypted { get; private set; }
                public X509Certificate Certificate { get; private set; }
                public SslProtocols EncryptionProtocol { get; private set; }
                public Dictionary<string, ITCP.Client> Clients { get; }

                public Task Open(Host host)
                {
                    return Open(host, _defaultBacklog);
                }

                public Task Open(Host host, int backlog)
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

                            Host = new Host(socket.LocalEndPoint);

                            _socket = socket;

                            _isClosed = false;

                            On.OnOpen?.Invoke(null, null);

                            InitAccept();
                        }
                        catch (Exception e)
                        {
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

                            foreach (var client in Clients.Values) client.To.Close();

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
                            On.OnClose?.Invoke(null, null);
                        }
                    });
                }

                public void DataBroadcast(string data)
                {
                    foreach (var client in Clients) client.Value.To.Data(data);
                }

                public void DataBroadcast(string data, Encoding encoding)
                {
                    foreach (var client in Clients) client.Value.To.Data(data, encoding);
                }

                public void DataBroadcast(byte[] data)
                {
                    foreach (var client in Clients) client.Value.To.Data(data);
                }

                public void EventBroadcast(string name, string data)
                {
                    foreach (var client in Clients) client.Value.To.Event(name, data);
                }

                public void EventBroadcast(string name, string data, Encoding encoding)
                {
                    foreach (var client in Clients) client.Value.To.Data(data, encoding);
                }

                public void EventBroadcast(string name, byte[] data)
                {
                    foreach (var client in Clients) client.Value.To.Event(name, data);
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
                    void UpdateAccept()
                    {
                        if (IsOpened) _socket.BeginAccept(AcceptCallback, null);
                    }

                    void AcceptCallback(IAsyncResult result)
                    {
                        try
                        {
                            var socket = _socket.EndAccept(result);

                            lock (_lockAccept)
                            {
                                _socketList.Add(socket);
                            }
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                        }
                        finally
                        {
                            UpdateAccept();
                        }
                    }

                    void AcceptValidation()
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

                            new Client(socket, _server, (client, success) =>
                            {
                                if (success)
                                {
                                    lock (_lockClient)
                                    {
                                        client.On.Close(() => RemoveClient(client.Id));

                                        Clients.Add(client.Id, client);

                                        On.OnAccept?.Invoke(null, client);

                                        client.InitServerSide();
                                    }
                                }
                                else
                                {
                                    socket.Close();
                                    socket.Dispose();
                                }
                            }).InitServerValidator();
                        }
                    }


                    // Accept
                    UpdateAccept();
                    // true: thread is destroyed normally when program end (non-force required)
                    // false: this thread will be persistent and will block the destruction of main process (force quit required)
                    const bool isBackground = true;
                    new Thread(AcceptValidation) { IsBackground = isBackground }.Start();
                }

                private void RemoveClient(string id)
                {
                    lock (_lockClient)
                    {
                        Clients.Remove(id);
                    }
                }
            }
        }
    }
}