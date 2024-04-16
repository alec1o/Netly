﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            internal class ClientTo : IClientTo
            {
                private readonly Client _client;
                private readonly bool _isServer;
                private bool _isClosed, _isOpeningOrClosing;
                private Socket _socket;

                private ClientTo()
                {
                    _socket = null;
                    _isServer = false;
                    _isClosed = true;
                    _isOpeningOrClosing = false;
                    Host = Host.Default;
                }

                public ClientTo(Client client) : this()
                {
                    _client = client;
                }

                public ClientTo(Client client, ref Host host, ref Socket socket) : this()
                {
                    Host = host;
                    _client = client;
                    _socket = socket;
                    _isServer = true;
                    _isClosed = false;
                }

                public Host Host { get; private set; }
                public bool IsOpened => !_isClosed && _socket != null;
                private ClientOn ClientOn => _client._on;

                public Task Open(Host host)
                {
                    if (_isOpeningOrClosing || IsOpened || _isServer) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            ClientOn.OnModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            Host = new Host(_socket.RemoteEndPoint);

                            _isClosed = false;

                            InitReceiver();

                            ClientOn.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            NETLY.Logger.PushError(e);
                            ClientOn.OnError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpeningOrClosing = false;
                        }
                    });
                }

                public Task Close()
                {
                    if (_isOpeningOrClosing || !IsOpened) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        if (!_isServer)
                            try
                            {
                                _socket?.Shutdown(SocketShutdown.Both);
                                _socket?.Close();
                                _socket?.Dispose();
                            }
                            catch (Exception e)
                            {
                                NETLY.Logger.PushError(e);
                            }
                            finally
                            {
                                _socket = null;
                            }

                        _isOpeningOrClosing = false;
                        _isClosed = true;

                        try
                        {
                            ClientOn.OnClose?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            NETLY.Logger.PushError(e);
                        }
                    });
                }

                public void Data(byte[] data)
                {
                    Send(data);
                }

                public void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(NE.GetBytes(data, encoding));
                }

                public void Event(string name, byte[] data)
                {
                    Send(EventManager.Create(name, data));
                }

                public void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                public void InitServerSide()
                {
                    if (_isServer is false) return;

                    ClientOn.OnModify?.Invoke(null, _socket);

                    ClientOn.OnOpen?.Invoke(null, null);
                }


                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = EventManager.Verify(bytes);

                    if (content.buffer == null)
                        ClientOn.OnData?.Invoke(null, bytes);
                    else
                        ClientOn.OnEvent?.Invoke(null, (content.name, content.buffer));
                }

                private void ReceiveJob()
                {
                    var length = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    var buffer = new byte[length];
                    var point = Host.EndPoint;

                    while (IsOpened)
                        try
                        {
                            var size = _socket.ReceiveFrom
                            (
                                buffer,
                                0,
                                buffer.Length,
                                SocketFlags.None,
                                ref point
                            );

                            if (size <= 0)
                            {
                                if (IsOpened) continue;
                                break;
                            }

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            PushResult(ref data);
                        }
                        catch (Exception e)
                        {
                            NETLY.Logger.PushError(e);
                            if (!IsOpened) break;
                        }

                    Close();
                }

                private void Send(byte[] bytes)
                {
                    if (bytes == null || bytes.Length <= 0 || !IsOpened) return;

                    try
                    {
                        _socket?.BeginSendTo
                        (
                            bytes,
                            0,
                            bytes.Length,
                            SocketFlags.None,
                            Host.EndPoint,
                            null,
                            null
                        );
                    }
                    catch (Exception e)
                    {
                        NETLY.Logger.PushError(e);
                    }
                }

                private void InitReceiver()
                {
                    new Thread(ReceiveJob)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    }.Start();
                }

                public void OnServerBuffer(ref byte[] buffer)
                {
                    PushResult(ref buffer);
                }
            }
        }
    }
}