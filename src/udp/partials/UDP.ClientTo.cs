using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client
        {
            private class ClientTo : IUDP.ClientTo
            {
                private readonly Client _client;
                private readonly bool _isServer;
                private bool _isClosed, _isOpeningOrClosing, _initServerSide;
                private Socket _socket;

                private ClientTo()
                {
                    _socket = null;
                    _isServer = false;
                    _isClosed = true;
                    _isOpeningOrClosing = false;
                    _initServerSide = false;
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
                            NetlyEnvironment.Logger.Create(e);
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
                                NetlyEnvironment.Logger.Create(e);
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
                    if (!IsOpened || data == null) return;

                    Send(data);
                }

                public void Data(string data)
                {
                    if (!IsOpened || data == null) return;

                    Send(data.GetBytes());
                }

                public void Data(Host targetHost, byte[] data)
                {
                    if (!IsOpened || targetHost == null || data == null) return;

                    Send(targetHost, data);
                }

                public void Data(Host targetHost, string data)
                {
                    if (!IsOpened || targetHost == null || data == null) return;

                    Send(targetHost, data.GetBytes());
                }

                public void Data(string data, Encoding encoding)
                {
                    if (!IsOpened || data == null) return;

                    Send(data.GetBytes(encoding));
                }

                public void Data(Host targetHost, string data, Encoding encoding)
                {
                    if (!IsOpened || targetHost == null || data == null) return;

                    Send(targetHost, data.GetBytes(encoding));
                }

                public void Event(string name, byte[] data)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data));
                }

                public void Event(string name, string data)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
                }

                public void Event(Host targetHost, string name, byte[] data)
                {
                    if (!IsOpened || targetHost == null || name == null || data == null) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data));
                }

                public void Event(Host targetHost, string name, string data)
                {
                    if (!IsOpened || targetHost == null || name == null || data == null) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data.GetBytes()));
                }

                public void Event(string name, string data, Encoding encoding)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
                }

                public void Event(Host targetHost, string name, string data, Encoding encoding)
                {
                    if (!IsOpened || targetHost == null || name == null || data == null) return;

                    Send(targetHost, NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)));
                }

                public void InitServerSide()
                {
                    if (!_isServer || _initServerSide) return;
                    _initServerSide = true;

                    On.OnModify?.Invoke(null, _socket);

                    On.OnOpen?.Invoke(null, null);
                }


                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = NetlyEnvironment.EventManager.Verify(bytes);

                    if (content.buffer == null)
                        On.OnData?.Invoke(null, bytes);
                    else
                        On.OnEvent?.Invoke(null, (content.name, content.buffer));
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
                            bytes,
                            0,
                            bytes.Length,
                            SocketFlags.None,
                            host.EndPoint,
                            null,
                            null
                        );
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                }

                private void InitReceiver()
                {
                    var endpoint = Host.Default.EndPoint;

                    var buffer = new byte
                    [
                        // Maximum/Default receive buffer length.
                        (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                    ];

                    ReceiverUpdate();

                    void ReceiverUpdate()
                    {
                        _socket.BeginReceiveFrom
                        (
                            buffer,
                            0,
                            buffer.Length,
                            SocketFlags.None,
                            ref endpoint,
                            ReceiveCallback,
                            null
                        );
                    }

                    void ReceiveCallback(IAsyncResult result)
                    {
                        try
                        {
                            var size = _socket.EndReceiveFrom(result, ref endpoint);

                            if (size <= 0)
                            {
                                if (IsOpened)
                                    ReceiverUpdate();
                                else
                                    Close();

                                return;
                            }

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            PushResult(ref data);

                            ReceiverUpdate();
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                            Close();
                        }
                    }
                }

                public void OnServerBuffer(ref byte[] buffer)
                {
                    PushResult(ref buffer);
                }
            }
        }
    }
}