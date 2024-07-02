using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                public Host Host { get; private set; }
                public bool IsOpened => IsConnected();
                private ClientOn On => _client._on;
                private readonly Client _client;
                private Socket _socket;
                private bool _isOpeningOrClosing, _isClosed;
                private readonly bool _isServer;
                private int _openTimeout;

                private struct Config
                {
                    public const byte PingBuffer = 0;
                    public const byte OpenBuffer = 128;
                    public const byte CloseBuffer = 255;
                    public const byte OpenRepeat = 32;
                    public const byte CloseRepeat = 32;
                }


                private ClientTo()
                {
                    _isOpeningOrClosing = false;
                    _isClosed = true;
                    _socket = null;
                    _client = null;
                    _isServer = false;
                    _openTimeout = 3000;
                    Host = Host.Default;
                }

                public ClientTo(Client client)
                {
                    _client = client;
                }

                public Task Open(Host host)
                {
                    if (!IsOpened || _isOpeningOrClosing || _isServer) return Task.CompletedTask;
                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            // create connection data
                            var data = new byte[] { Config.OpenBuffer };

                            // send connected data
                            for (int i = 0; i < Config.OpenRepeat; i++)
                            {
                                SendRaw(null, ref data);
                            }

                            // waiting for connection data response
                            List<byte> ackList = new List<byte>();

                            try
                            {
                                Task.Run(() =>
                                {
                                    while (ackList.Count < Config.OpenRepeat)
                                    {
                                        var buffer = new byte[1024];
                                        var endpoint = Host.EndPoint;

                                        var size = _socket.ReceiveFrom
                                        (
                                            buffer,
                                            0,
                                            buffer.Length,
                                            SocketFlags.None,
                                            ref endpoint
                                        );

                                        if (size == 1)
                                        {
                                            ackList.Add(buffer[0]);
                                        }
                                    }
                                }).Wait(_openTimeout);
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }

                            // check if connection already opened
                            int feeds = 0;

                            foreach (var ack in ackList)
                            {
                                if (ack == Config.OpenBuffer)
                                {
                                    feeds++;
                                }
                            }

                            if (feeds <= 0)
                            {
                                throw new Exception
                                (
                                    $"A connection attempt failed because the connected party did not properly respond after a period of time ({_openTimeout}ms), or established connection failed because connected host has failed to respond"
                                );
                            }

                            // connected successful
                            Host = new Host(_socket.RemoteEndPoint);

                            _isClosed = false;
                            
                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            _socket = null;
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
                    if (IsOpened || _isOpeningOrClosing) return Task.CompletedTask;
                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        if (IsOpened)
                        {
                            try
                            {
                                var data = new[] { Config.CloseBuffer };

                                for (var i = 0; i < Config.CloseRepeat; i++)
                                {
                                    SendRaw(_isServer ? Host : null, ref data);
                                }
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }
                        }

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

                public void Data(byte[] data, MessageType messageType)
                {
                    Send(null, data, messageType);
                }

                public void Data(string data, MessageType messageType)
                {
                    Send(null, data.GetBytes(), messageType);
                }

                public void Data(string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, data.GetBytes(encoding), messageType);
                }

                public void Event(string name, byte[] data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data), messageType);
                }

                public void Event(string name, string data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes()), messageType);
                }

                public void Event(string name, string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)), messageType);
                }

                private void Send(Host host, byte[] data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                private void SendRaw(Host host, ref byte[] data)
                {
                    throw new NotImplementedException();
                }

                private bool IsConnected()
                {
                    if (_socket == null || _isClosed) return false;
                    return true;
                }

                public void SetOpenTimeout(int value)
                {
                    if (IsOpened)
                        throw new Exception
                        (
                            $"Isn't possible use `{nameof(SetOpenTimeout)}` while socket is already connected."
                        );

                    if (value <= 0)
                        throw new Exception
                        (
                            $"Isn't possible use {nameof(SetOpenTimeout)} with a `0` or `negative` value"
                        );

                    _openTimeout = value;
                }

                public int GetOpenTimeout()
                {
                    return _openTimeout;
                }
            }
        }
    }
}