using System;
using System.Net;
using System.Net.Security;
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
            internal class _To : ITo
            {
                public Host Host { get; private set; }
                public bool IsOpened => IsConnected();

                private bool UseConnection => _client._useConnection;

                private bool CanSend => _isClosed is false && _isClosing is false && _isOpening is false;

                private string Id => _client._id;

                private _On On => _client._on;

                private Socket _socket;

                private bool
                    _isOpening,
                    _isClosing,
                    _isClosed;

                private readonly Client _client;
                private readonly bool _isServer;

                /* ---- CONSTRUCTOR --- */

                private _To()
                {
                    _socket = null;
                    _isOpening = false;
                    _isClosing = false;
                    _isServer = false;
                    _isClosed = true;
                    Host = Host.Default;
                }

                public _To(Client client) : this()
                {
                    _client = client;
                    _isClosed = true;
                }

                public _To(Client client, Host host, out bool success) : this()
                {
                    _client = client;
                    CreateUdpSocket(ref host, out _socket);
                    _isServer = true;
                    _isClosed = false;
                    success = true;
                }

                /* ---- INTERFACE --- */

                private static void CreateUdpSocket(ref Host host, out Socket socket)
                {
                    socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                }

                public void Open(Host host)
                {
                    if (_isOpening || _isClosing || IsOpened || _isServer) return;

                    _isOpening = true;

                    Task.Run(() =>
                    {
                        try
                        {
                            CreateUdpSocket(ref host, out _socket);

                            On.m_onModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            Host = new Host(_socket.RemoteEndPoint);

                            _isClosed = false;

                            On.m_onOpen?.Invoke(null, null);

                            InitReceiver();
                        }
                        catch (Exception e)
                        {
                            On.m_onError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpening = false;
                        }
                    });
                }

                public void Close()
                {
                    if (_isOpening || _isClosing) return;

                    _isClosing = true;

                    Task.Run(() =>
                    {
                        try
                        {
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

                            if (_isClosed is false)
                            {
                                _isClosed = true;
                                On.m_onClose?.Invoke(null, null);
                            }

                            _isClosing = false;
                        }
                    });
                }

                public void Data(byte[] data)
                {
                    if (CanSend == false || data == null) return;

                    SendDispatch(data);
                }


                public void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (CanSend == false || data == null) return;

                    SendDispatch(NE.GetBytes(data, encoding));
                }

                public void Event(string name, byte[] data)
                {
                    if (CanSend == false || data == null || name == null) return;

                    SendDispatch(EventManager.Create(name, data));
                }

                public void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (CanSend == false || data == null || name == null) return;

                    SendDispatch(EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                /* ---- INTERNAL --- */

                private bool IsConnected()
                {
                    try
                    {
                        if (_socket == null || !_socket.Connected) return false;
                        const int timeout = 5000;
                        return !(_socket.Poll(timeout, SelectMode.SelectRead) && _socket.Available == 0);
                    }
                    catch
                    {
                        return false;
                    }
                }

                public void InitServerSide()
                {
                    if (_isServer is false) return;

                    On.m_onModify?.Invoke(null, _socket);

                    On.m_onOpen?.Invoke(null, null);

                    InitReceiver();
                }


                private void PushResult(ref byte[] bytes)
                {
                    (string name, byte[] buffer) content = EventManager.Verify(bytes);

                    if (content.buffer == null)
                    {
                        On.m_onData?.Invoke(null, bytes);
                    }
                    else
                    {
                        On.m_onEvent?.Invoke(null, (content.name, content.buffer));
                    }
                }

                private void ReceiveJob()
                {
                    // Info: Is max UDP packet size.
                    // Docs: https://en.wikipedia.org/wiki/User_Datagram_Protocol
                    // Size: 65,536 (64kb).
                    byte[] buffer = new byte[1024 * 64];
                    EndPoint endpoint = Host.EndPoint;

                    while (IsOpened)
                    {
                        try
                        {
                            int size = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                            if (size <= 0)
                            {
                                if (IsOpened) continue;
                                break;
                            }

                            byte[] data = new byte[size];

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

                private void SendDispatch(byte[] bytes)
                {
                    if (!IsOpened) return;

                    try
                    {
                        _socket?.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, callback: null, state: null);
                    }
                    catch
                    {
                        // Ignored
                    }
                }

                private void InitReceiver()
                {
                    new Thread(ReceiveJob) { IsBackground = true }.Start();
                }
            }
        }
    }
}