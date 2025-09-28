using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
                    Host = NHost.Default;
                }

                public ClientTo(Client client) : this()
                {
                    _client = client;
                }

                public ClientTo(Client client, ref NHost host, ref Socket socket) : this()
                {
                    Host = host;
                    _client = client;
                    _socket = socket;
                    _isServer = true;
                    _isClosed = false;
                }

                public NHost Host { get; private set; }
                public bool IsOpened => !_isClosed && _socket != null;
                private ClientOn On => _client._on;

                public Task Open(NHost host)
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

                            Host = new NHost(_socket.RemoteEndPoint);

                            _isClosed = false;

                            InitReceiver();

                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            NLogger.Singleton.Submit(e);
                            _isClosed = true;
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
                                NLogger.Singleton.Submit(e);
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
                    if (!IsOpened || string.IsNullOrEmpty(data)) return;

                    Send(data.GetBytes());
                }

                public void Data(string data, Encoding encoding)
                {
                    if (!IsOpened || string.IsNullOrEmpty(data)) return;

                    Send(data.GetBytes(encoding));
                }

                public void Event(string name, byte[] data)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || data == null || data.Length <= 0) return;

                    var header = NMessage.Create(name, data.LongLength);
                    Send(header, data);
                }

                public void Event(string name, string data)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data)) return;

                    var bytes = data.GetBytes();
                    var header = NMessage.Create(name, bytes.LongLength);
                    Send(header, bytes);
                }

                public void Event(string name, string data, Encoding encoding)
                {
                    if (!IsOpened || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data)) return;

                    var bytes = data.GetBytes(encoding);
                    var header = NMessage.Create(name, bytes.LongLength);
                    Send(header, bytes);
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
                    var stream = NHelper.NewStream(bytes.LongLength);
                    stream.Position = 0;
                    stream.Write(bytes, 0, bytes.Length);

                    if (NMessage.TryParse(stream, out var name, out var message, NHelper.NewStream))
                    {
                        var buffer = new byte[message.Length];
                        message.Position = 0;
                        message.Write(buffer, 0, buffer.Length);
                        On.OnEvent?.Invoke(null, (name, buffer));
                    }
                    else
                        On.OnData?.Invoke(null, bytes);
                }

                private void Send(params byte[][] bytes)
                {
                    if (bytes == null || bytes.Length <= 0) return;

                    Send(Host, bytes);
                }

                private void Send(NHost host, params byte[][] buffers)
                {
                    if (buffers == null || buffers.Length <= 0 || !IsOpened || host == null) return;

                    foreach (var buffer in buffers)
                    {
                        try
                        {
                            var data = new ArraySegment<byte>(buffer);
                            if (_isServer)
                            {
                                // this way of send just work on windows and linux, except macOS (maybe iOS)
                                _socket?.SendToAsync(data, SocketFlags.None, host.EndPoint);
                            }
                            else
                            {
                                // this way of send just work on windows and linux, include macOS and iOS
                                _socket?.SendAsync(data, SocketFlags.None);
                            }
                        }
                        catch (Exception e)
                        {
                            NLogger.Singleton.Submit(e);
                        }
                    }
                }

                private void InitReceiver()
                {
                    var buffer = new byte
                    [
                        // Maximum/Default receive buffer length.
                        (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                    ];

                    new Thread(Receive) { IsBackground = true }.Start();

                    return;

                    void Receive()
                    {
                        while (IsOpened)
                        {
                            try
                            {
                                var size = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                                if (size <= 0)
                                {
                                    if (IsOpened) continue;
                                    else break;
                                }

                                var bytes = new byte[size];
                                Array.Copy(buffer, 0, bytes, 0, bytes.Length);

                                PushResult(ref bytes);
                            }
                            catch (Exception e)
                            {
                                NLogger.Singleton.Submit(e);
                            }
                        }

                        Close();
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