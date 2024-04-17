using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            internal class ClientTo : IUDP.ClientTo
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
                private ClientOn On => _client._on;

                public Task Open(Host host)
                {
                    if (_isOpeningOrClosing || IsOpened || _isServer) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            Host = new Host(_socket.RemoteEndPoint);

                            _isClosed = false;

                            InitReceiver();

                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            NETLY.Logger.PushError(e);
                            On.OnError?.Invoke(null, e);
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

                        On.OnClose?.Invoke(null, null);
                    });
                }

                public void Data(byte[] data)
                {
                    Send(data);
                }

                public void Data(Host targetHost, byte[] data)
                {
                    Send(targetHost, data);
                }

                public void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(NE.GetBytes(data, encoding));
                }

                public void Data(Host targetHost, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(targetHost, NE.GetBytes(data, encoding));
                }

                public void Event(string name, byte[] data)
                {
                    Send(EventManager.Create(name, data));
                }

                public void Event(Host targetHost, string name, byte[] data)
                {
                    Send(targetHost, EventManager.Create(name, data));
                }

                public void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                public void Event(Host targetHost, string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    Send(targetHost, EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                public void InitServerSide()
                {
                    if (_isServer is false) return;

                    On.OnModify?.Invoke(null, _socket);

                    On.OnOpen?.Invoke(null, null);
                }


                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = EventManager.Verify(bytes);

                    if (content.buffer == null)
                        On.OnData?.Invoke(null, bytes);
                    else
                        On.OnEvent?.Invoke(null, (content.name, content.buffer));
                }

                private void ReceiveJob()
                {
                    var length = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    var buffer = new byte[length];
                    var point = Host.EndPoint;

                    while (IsOpened)
                    {
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
                    }

                    Close();
                }

                private void Send(byte[] bytes)
                {
                    Send(Host, bytes);
                }

                private void Send(Host host, byte[] bytes)
                {
                    if (bytes == null || bytes.Length <= 0 || !IsOpened || host == null) return;

                    try
                    {
                        _socket?.BeginSendTo
                        (
                            buffer: bytes,
                            offset: 0,
                            size: bytes.Length,
                            socketFlags: SocketFlags.None,
                            remoteEP: host.EndPoint,
                            callback: null,
                            state: null
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